using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Показывает иконку способности выбранного скина и обновляет её состояние в матче.
/// </summary>
public sealed class BallSkillHudController : MonoBehaviour
{
    enum SkillHudMode
    {
        Passive,
        DragonMultiplier,
        ChargeCounter,
        WarriorThreshold
    }

    struct SkillIconBinding
    {
        public BallSkinId SkinId;
        public GameObject Root;
        public SkillHudMode Mode;
        public Image BlockOverlay;
        public TMP_Text CounterText;
        public TMP_Text MultiplierText;
        public Image WarriorIndicator;
    }

    [Header("Связи")]
    [SerializeField] Transform skillsRoot;
    [SerializeField] BallAbilityConfig abilityConfig;
    [SerializeField] BallSpawner ballSpawner;

    readonly List<SkillIconBinding> _icons = new();
    BallSkinId _activeSkin = BallSkinId.Defolt;
    SlingshotShooter _playerBall;
    BallSkinController _skinController;
    BallThrowSession _throwSession;
    BallAbilityProcessor _abilityProcessor;
    BallAbilityMatchState _matchState;
    HoopHealth _enemyHoop;

    void Awake()
    {
        if (skillsRoot == null)
            skillsRoot = transform;

        CacheIcons();

        if (ballSpawner == null)
            ballSpawner = BallSpawner.Instance;

        _matchState = BallAbilityMatchState.EnsureExists();
        _matchState.StateChanged += RefreshDynamicWidgets;

        ApplySkin(BallSkinSelectionStorage.SelectedSkin, bindBall: false);
    }

    void OnEnable()
    {
        if (ballSpawner != null)
            ballSpawner.ThrowableBallReady += HandleThrowableBallReady;

        if (ballSpawner != null && ballSpawner.CurrentThrowableBall != null)
            BindPlayerBall(ballSpawner.CurrentThrowableBall);

        RefreshDynamicWidgets();
    }

    void OnDisable()
    {
        if (ballSpawner != null)
            ballSpawner.ThrowableBallReady -= HandleThrowableBallReady;

        UnbindPlayerBall();
    }

    void OnDestroy()
    {
        if (_matchState != null)
            _matchState.StateChanged -= RefreshDynamicWidgets;
    }

    void LateUpdate()
    {
        UpdateDragonMultiplier();
        UpdateWarriorIndicator();
    }

    void HandleThrowableBallReady(SlingshotShooter ball) => BindPlayerBall(ball);

    void BindPlayerBall(SlingshotShooter ball)
    {
        UnbindPlayerBall();

        if (ball == null || ball.gameObject.name.Contains("Bot"))
            return;

        _playerBall = ball;

        if (!ball.TryGetComponent(out _skinController))
            _skinController = null;

        if (!ball.TryGetComponent(out _throwSession))
            _throwSession = null;

        if (!ball.TryGetComponent(out _abilityProcessor))
            _abilityProcessor = null;
        else if (_abilityProcessor.Config != null)
            abilityConfig = _abilityProcessor.Config;

        if (_skinController != null)
        {
            _skinController.SkinChanged += HandleSkinChanged;
            ApplySkin(_skinController.ActiveSkin, bindBall: true);
        }
    }

    void UnbindPlayerBall()
    {
        if (_skinController != null)
            _skinController.SkinChanged -= HandleSkinChanged;

        _playerBall = null;
        _skinController = null;
        _throwSession = null;
        _abilityProcessor = null;
        _enemyHoop = null;
    }

    void HandleSkinChanged(BallSkinId skinId) => ApplySkin(skinId, bindBall: true);

    public void ApplySkin(BallSkinId skinId, bool bindBall)
    {
        _activeSkin = skinId;

        for (int i = 0; i < _icons.Count; i++)
        {
            SkillIconBinding icon = _icons[i];
            if (icon.Root == null)
                continue;

            icon.Root.SetActive(icon.SkinId == skinId);
        }

        if (bindBall)
            ResolveEnemyHoop();

        RefreshDynamicWidgets();
    }

    void CacheIcons()
    {
        _icons.Clear();

        for (int i = 0; i < skillsRoot.childCount; i++)
        {
            Transform child = skillsRoot.GetChild(i);
            if (child == null)
                continue;

            if (!BallSkinController.TryParseSkinName(child.name, out BallSkinId skinId))
                continue;

            SkillHudMode mode = ResolveMode(skinId);
            _icons.Add(new SkillIconBinding
            {
                SkinId = skinId,
                Root = child.gameObject,
                Mode = mode,
                BlockOverlay = FindBlockOverlay(child, mode),
                CounterText = FindCounterText(child, mode),
                MultiplierText = FindMultiplierText(child, mode),
                WarriorIndicator = FindWarriorIndicator(child, mode)
            });
        }
    }

    static SkillHudMode ResolveMode(BallSkinId skinId)
    {
        switch (skinId)
        {
            case BallSkinId.Dragon:
                return SkillHudMode.DragonMultiplier;
            case BallSkinId.Fire:
            case BallSkinId.Goblin:
            case BallSkinId.Golem:
                return SkillHudMode.ChargeCounter;
            case BallSkinId.Warior:
                return SkillHudMode.WarriorThreshold;
            default:
                return SkillHudMode.Passive;
        }
    }

    static Image FindBlockOverlay(Transform root, SkillHudMode mode)
    {
        if (mode != SkillHudMode.ChargeCounter)
            return null;

        Image rootImage = root.GetComponent<Image>();
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == rootImage)
                continue;

            return image;
        }

        return null;
    }

    static TMP_Text FindCounterText(Transform root, SkillHudMode mode)
    {
        if (mode != SkillHudMode.ChargeCounter)
            return null;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    static TMP_Text FindMultiplierText(Transform root, SkillHudMode mode)
    {
        if (mode != SkillHudMode.DragonMultiplier)
            return null;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    static Image FindWarriorIndicator(Transform root, SkillHudMode mode)
    {
        if (mode != SkillHudMode.WarriorThreshold)
            return null;

        Image rootImage = root.GetComponent<Image>();
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == rootImage)
                continue;

            return image;
        }

        return null;
    }

    void RefreshDynamicWidgets()
    {
        if (_matchState == null)
            _matchState = BallAbilityMatchState.EnsureExists();

        PvPTeam playerTeam = PvPTeam.Player;

        for (int i = 0; i < _icons.Count; i++)
        {
            SkillIconBinding icon = _icons[i];
            if (icon.Root == null || icon.SkinId != _activeSkin)
                continue;

            switch (icon.Mode)
            {
                case SkillHudMode.ChargeCounter:
                    ApplyChargeCounter(icon, playerTeam);
                    break;
                case SkillHudMode.DragonMultiplier:
                    ApplyDragonIdle(icon);
                    break;
                case SkillHudMode.WarriorThreshold:
                    ApplyWarriorIdle(icon);
                    break;
            }
        }
    }

    void ApplyChargeCounter(SkillIconBinding icon, PvPTeam team)
    {
        if (abilityConfig == null)
            return;

        bool ready = false;
        int remaining = 0;

        switch (icon.SkinId)
        {
            case BallSkinId.Fire:
                ready = _matchState.IsFireCharged(team);
                if (!ready)
                    remaining = 1;

                break;

            case BallSkinId.Goblin:
            {
                int cycle = abilityConfig.goblinEveryNthAccurateThrow;
                int mod = _matchState.GetAccurateThrows(team) % cycle;
                ready = mod == cycle - 1;
                if (!ready)
                    remaining = cycle - 1 - mod;

                break;
            }

            case BallSkinId.Golem:
                ready = _matchState.IsGolemCharged(team);
                if (!ready)
                {
                    int need = abilityConfig.golemConsecutiveHitsForBuff;
                    remaining = Mathf.Max(1, need - _matchState.GetConsecutiveScores(team));
                }

                break;
        }

        SetOverlay(icon.BlockOverlay, !ready);
        SetCounter(icon.CounterText, ready, remaining);
    }

    void ApplyDragonIdle(SkillIconBinding icon)
    {
        if (icon.MultiplierText == null)
            return;

        icon.MultiplierText.gameObject.SetActive(false);
    }

    void ApplyWarriorIdle(SkillIconBinding icon)
    {
        if (icon.WarriorIndicator != null)
            icon.WarriorIndicator.gameObject.SetActive(true);
    }

    void UpdateDragonMultiplier()
    {
        if (_activeSkin != BallSkinId.Dragon)
            return;

        SkillIconBinding? icon = FindActiveIcon(BallSkinId.Dragon);
        if (icon == null || icon.Value.MultiplierText == null)
            return;

        TMP_Text label = icon.Value.MultiplierText;
        bool inFlight = _throwSession != null && _throwSession.ThrowStarted && !_throwSession.ThrowFinished;

        if (!inFlight)
        {
            label.gameObject.SetActive(false);
            return;
        }

        label.gameObject.SetActive(true);

        if (_abilityProcessor != null)
        {
            label.text = FormatDragonMultiplier(_abilityProcessor.GetDragonDamageMultiplierForFlight());
            return;
        }

        float distance = _throwSession.FlightDistanceMeters;
        label.text = abilityConfig != null
            ? abilityConfig.FormatDragonMultiplierForUi(distance)
            : FormatDragonMultiplier(1f);
    }

    void UpdateWarriorIndicator()
    {
        if (_activeSkin != BallSkinId.Warior)
            return;

        SkillIconBinding? icon = FindActiveIcon(BallSkinId.Warior);
        if (icon == null || icon.Value.WarriorIndicator == null)
            return;

        if (_enemyHoop == null)
            ResolveEnemyHoop();

        bool unlocked = _enemyHoop != null
            && abilityConfig != null
            && _enemyHoop.Health01 <= abilityConfig.wariorEnemyShieldThreshold;

        // Image = блокировка: включён, пока способность неактивна.
        icon.Value.WarriorIndicator.gameObject.SetActive(!unlocked);
    }

    void ResolveEnemyHoop()
    {
        _enemyHoop = null;

        if (_playerBall == null)
            return;

        if (_playerBall.TryGetComponent(out BallAbilityProcessor processor))
            _enemyHoop = processor.ResolveEnemyHoop(PvPTeam.Player);

        if (_enemyHoop == null)
        {
            HoopCombatRegistry registry = HoopCombatRegistry.Instance;
            _enemyHoop = registry != null ? registry.GetHoop(PvPTeam.Bot) : null;
        }
    }

    SkillIconBinding? FindActiveIcon(BallSkinId skinId)
    {
        for (int i = 0; i < _icons.Count; i++)
        {
            if (_icons[i].SkinId == skinId)
                return _icons[i];
        }

        return null;
    }

    static void SetOverlay(Image overlay, bool visible)
    {
        if (overlay != null)
            overlay.gameObject.SetActive(visible);
    }

    static string FormatDragonMultiplier(float multiplier) => multiplier.ToString("0.#");

    static void SetCounter(TMP_Text counter, bool ready, int remaining)
    {
        if (counter == null)
            return;

        if (ready)
        {
            counter.gameObject.SetActive(false);
            return;
        }

        counter.gameObject.SetActive(true);
        counter.text = Mathf.Max(1, remaining).ToString();
    }
}
