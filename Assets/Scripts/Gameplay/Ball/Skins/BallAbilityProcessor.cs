using UnityEngine;

/// <summary>Расчёт урона и побочных эффектов способностей скина при голе и промахе.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BallSkinController))]
[RequireComponent(typeof(BallThrowSession))]
public sealed class BallAbilityProcessor : MonoBehaviour
{
    [SerializeField] BallAbilityConfig config;

    BallSkinController _skins;
    BallThrowSession _session;

    void Awake()
    {
        _skins = GetComponent<BallSkinController>();
        _session = GetComponent<BallThrowSession>();
    }

    public void NotifyThrowStarted()
    {
        _session.BeginThrow();
    }

    public void NotifyThrowEndedWithoutScore()
    {
        if (_session == null)
            return;

        if (!_session.TryFinishWithoutScore())
            return;

        BallAbilityMatchState match = BallAbilityMatchState.EnsureExists();
        match.RegisterMiss(ResolveThrowerTeam());
    }

    PvPTeam ResolveThrowerTeam()
    {
        if (TryGetComponent(out BallThrowOwnership ownership))
            return ownership.LastThrower;

        return PvPTeam.Player;
    }

    public void NotifyShieldCollision() => _session.MarkShieldBounce();

    public void NotifyRimOrShieldCollisionWithoutScore() => _session.MarkRimOrShieldHitWithoutScore();

    public BallScoreOutcome ComputeScoreOutcome(HoopHealth targetHoop, PvPTeam throwerTeam)
    {
        var outcome = new BallScoreOutcome();
        if (targetHoop == null)
        {
            outcome.HoopDamage = 1;
            return outcome;
        }

        BallSkinId skin = _skins != null ? _skins.ActiveSkin : BallSkinId.Defolt;

        if (skin == BallSkinId.Paladin && targetHoop.IsDefendedBy(throwerTeam))
        {
            outcome.IsFriendlyHealOnly = true;
            outcome.PaladinShieldHeal = config != null ? config.paladinSelfHealAmount : 2;
            return outcome;
        }

        if (config == null)
        {
            outcome.HoopDamage = 1;
            return outcome;
        }

        int damage = config.baseScoreDamage;
        outcome.IgnoreShieldDefense = skin == BallSkinId.Ricar;

        switch (skin)
        {
            case BallSkinId.Dragon:
            {
                float bonus = _session.FlightTimeSeconds * config.dragonDamagePerFlightSecond;
                bonus = Mathf.Min(bonus, config.dragonMaxDamageBonus);
                damage += Mathf.RoundToInt(bonus);
                break;
            }
            case BallSkinId.Orc:
                damage = ApplyOrcBonus(damage, throwerTeam);
                break;
        }

        bool forceCrit = false;
        if (skin == BallSkinId.Warior && IsEnemyHoopAtOrBelowWariorThreshold(targetHoop))
            forceCrit = true;

        if (skin == BallSkinId.Gnom && _session.BouncedOffShield)
            forceCrit = true;

        BallAbilityMatchState match = BallAbilityMatchState.EnsureExists();
        int consecutiveScores = match.GetConsecutiveScores(throwerTeam);
        int nextAccurateThrow = match.GetAccurateThrows(throwerTeam) + 1;

        if (skin == BallSkinId.Goblin && nextAccurateThrow % config.goblinEveryNthAccurateThrow == 0)
            forceCrit = true;

        if (forceCrit)
        {
            damage = Mathf.RoundToInt(damage * config.critMultiplier);
            outcome.IsCritical = true;
        }

        outcome.HoopDamage = Mathf.Max(1, damage);
        outcome.ShieldDamage = outcome.IgnoreShieldDefense ? outcome.HoopDamage : 0;

        switch (skin)
        {
            case BallSkinId.Fire:
                if (consecutiveScores + 1 >= config.fireConsecutiveHitsToIgnite)
                    outcome.ApplyBurn = true;
                break;
            case BallSkinId.Golem:
                if (consecutiveScores + 1 >= config.golemConsecutiveHitsForBuff)
                    outcome.GolemShieldBuff = true;
                break;
            case BallSkinId.Gorgylia:
                outcome.ApplyAntiHeal = true;
                break;
            case BallSkinId.Goblin:
                if (outcome.IsCritical)
                    outcome.GoblinGoldSteal = true;
                break;
            case BallSkinId.Gnom:
                if (_session.BouncedOffShield)
                    outcome.GnomGoldReward = true;
                break;
            case BallSkinId.Zombie:
                if (Random.value < config.zombiePoisonChance)
                    outcome.ApplyPoison = true;
                break;
        }

        return outcome;
    }

    public void ApplyScoreOutcome(BallScoreOutcome outcome, HoopHealth targetHoop, PvPTeam throwerTeam)
    {
        if (targetHoop == null)
            return;

        HoopCombatRegistry registry = HoopCombatRegistry.Instance;

        if (outcome.IsFriendlyHealOnly)
        {
            int healAmount = outcome.PaladinShieldHeal;
            if (healAmount > 0)
            {
                targetHoop.Heal(healAmount);

                ShieldHealth ownShield = registry != null ? registry.GetShield(throwerTeam) : null;
                if (ownShield == null)
                    ownShield = targetHoop.GetComponentInChildren<ShieldHealth>(true);

                HoopStatusEffects throwerEffects = registry?.GetStatusEffects(throwerTeam);
                if (ownShield != null && !ownShield.IsHealingBlocked(throwerEffects))
                    ownShield.Heal(healAmount);
            }

            return;
        }

        _session.MarkScored();
        BallAbilityMatchState.EnsureExists().RegisterScore(throwerTeam);

        ShieldHealth enemyShield = ResolveShield(targetHoop.DefendedTeam, registry, targetHoop);
        if (enemyShield != null)
            enemyShield.TakeDamage(outcome.HoopDamage);

        int hoopDamageDealt = targetHoop.ApplyScoreDamage(outcome.HoopDamage, outcome.IsCritical);

        BallSkinId activeSkin = _skins != null ? _skins.ActiveSkin : BallSkinId.Defolt;
        if (activeSkin == BallSkinId.Wampir && config != null && hoopDamageDealt > 0)
            ApplyWampirLifesteal(throwerTeam, hoopDamageDealt, registry);

        if (outcome.ApplyBurn)
            ResolveStatusEffects(targetHoop)
                .ApplyBurn(config.fireBurnDamagePerTick, config.fireBurnTickInterval, config.fireBurnDuration);

        if (outcome.ApplyPoison)
            ResolveStatusEffects(targetHoop)
                .ApplyPoison(config.zombiePoisonDamagePerTick, config.zombiePoisonTickInterval, config.zombiePoisonDuration);

        if (outcome.ApplyAntiHeal)
            ResolveStatusEffects(targetHoop).ApplyAntiHeal(config.gorgyliaAntiHealDuration);

        BattleGoldWallet wallet = BattleGoldWallet.Instance;
        if (wallet != null)
        {
            if (outcome.GnomGoldReward)
                wallet.AddGold(throwerTeam, config.gnomGoldReward);

            if (outcome.GoblinGoldSteal)
                wallet.StealGold(throwerTeam, config.goblinStealGoldAmount);
        }

        if (outcome.GolemShieldBuff && registry != null)
        {
            ShieldHealth ownShield = registry.GetShield(throwerTeam);
            ownShield?.IncreaseMaxHealth(config.golemMaxShieldBonus);
        }
    }

    public void TryApplySkeletMissDamage(HoopHealth nearestEnemyHoop, PvPTeam throwerTeam)
    {
        if (config == null || _skins == null || _skins.ActiveSkin != BallSkinId.Skelet)
            return;

        if (!_session.HitRimOrShieldWithoutScore || _session.ScoredThisThrow)
            return;

        if (nearestEnemyHoop == null)
            return;

        nearestEnemyHoop.ApplyScoreDamage(config.skeletMissDamage, isCritical: false);
    }

    /// <summary>Крит, если у кольца противника (до этого удара) осталось 30% HP или меньше.</summary>
    bool IsEnemyHoopAtOrBelowWariorThreshold(HoopHealth enemyHoop)
    {
        if (enemyHoop == null || config == null)
            return false;

        return enemyHoop.Health01 <= config.wariorEnemyShieldThreshold;
    }

    int ApplyOrcBonus(int baseDamage, PvPTeam throwerTeam)
    {
        HoopCombatRegistry registry = HoopCombatRegistry.Instance;
        if (registry == null)
            return baseDamage;

        ShieldHealth throwerShield = registry.GetShield(throwerTeam);
        PvPTeam enemyTeam = throwerTeam == PvPTeam.Player ? PvPTeam.Bot : PvPTeam.Player;
        ShieldHealth enemyShield = registry.GetShield(enemyTeam);

        if (throwerShield == null || enemyShield == null)
            return baseDamage;

        int diff = Mathf.Abs(throwerShield.CurrentHealth - enemyShield.CurrentHealth);
        float multiplier = 1f + diff * config.orcDamagePerShieldHpDifference;
        multiplier = Mathf.Min(multiplier, config.orcMaxDamageMultiplier);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    void ApplyWampirLifesteal(PvPTeam throwerTeam, int enemyHoopDamageDealt, HoopCombatRegistry registry)
    {
        int healAmount = Mathf.Max(1, Mathf.RoundToInt(enemyHoopDamageDealt * config.wampirLifestealPercent));
        HoopHealth throwerHoop = ResolveHoop(throwerTeam, registry);
        ShieldHealth throwerShield = ResolveShield(throwerTeam, registry, throwerHoop);
        if (throwerShield == null)
            return;

        HoopStatusEffects throwerEffects = registry?.GetStatusEffects(throwerTeam);
        if (throwerShield.IsHealingBlocked(throwerEffects))
            return;

        throwerShield.Heal(healAmount);

        // Видимая полоска над кольцом — HP защищаемого кольца игрока (щит в префабе без UI).
        throwerHoop?.Heal(healAmount);
    }

    static ShieldHealth ResolveShield(PvPTeam team, HoopCombatRegistry registry, HoopHealth hoopHint)
    {
        ShieldHealth shield = registry != null ? registry.GetShield(team) : null;
        if (shield != null)
            return shield;

        if (hoopHint != null)
            return hoopHint.GetComponentInChildren<ShieldHealth>(true);

        return registry?.GetHoop(team)?.GetComponentInChildren<ShieldHealth>(true);
    }

    static HoopHealth ResolveHoop(PvPTeam team, HoopCombatRegistry registry)
    {
        HoopHealth hoop = registry != null ? registry.GetHoop(team) : null;
        if (hoop != null)
            return hoop;

        HoopHealth[] hoops = Object.FindObjectsByType<HoopHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < hoops.Length; i++)
        {
            HoopHealth candidate = hoops[i];
            if (candidate == null || !candidate.UsesPvPTeamFilter)
                continue;

            if (candidate.DefendedTeam == team)
                return candidate;
        }

        return null;
    }

    static HoopStatusEffects ResolveStatusEffects(HoopHealth hoop)
    {
        if (hoop == null)
            return null;

        if (!hoop.TryGetComponent(out HoopStatusEffects effects))
            effects = hoop.gameObject.AddComponent<HoopStatusEffects>();

        return effects;
    }

    public HoopHealth ResolveEnemyHoop(PvPTeam throwerTeam)
    {
        HoopCombatRegistry registry = HoopCombatRegistry.Instance;
        if (registry == null)
            return null;

        PvPTeam enemy = throwerTeam == PvPTeam.Player ? PvPTeam.Bot : PvPTeam.Player;
        return registry.GetHoop(enemy);
    }
}
