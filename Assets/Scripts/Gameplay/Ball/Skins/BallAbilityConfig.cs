using UnityEngine;

/// <summary>Настройки чисел для способностей скинов мяча.</summary>
[CreateAssetMenu(fileName = "BallAbilityConfig", menuName = "Gameplay/Ball Ability Config")]
public sealed class BallAbilityConfig : ScriptableObject
{
    [Header("Общее")]
    [Min(1)] public int baseScoreDamage = 1;
    [Min(1f)] public float critMultiplier = 2f;

    [Header("Dragon — урон от времени полёта")]
    [Min(0f)] public float dragonDamagePerFlightSecond = 0.15f;
    [Min(1f)] public float dragonMaxDamageBonus = 3f;

    [Header("Fire — поджог кольца")]
    [Min(1)] public int fireConsecutiveHitsToIgnite = 2;
    [Min(1)] public int fireBurnDamagePerTick = 1;
    [Min(0.1f)] public float fireBurnTickInterval = 1f;
    [Min(0.1f)] public float fireBurnDuration = 4f;

    [Header("Gnom — отскок от щита")]
    [Min(0)] public int gnomGoldReward = 3;

    [Header("Goblin — каждый N-й точный бросок")]
    [Min(1)] public int goblinEveryNthAccurateThrow = 3;
    [Min(0)] public int goblinStealGoldAmount = 5;

    [Header("Golem — серия попаданий")]
    [Min(1)] public int golemConsecutiveHitsForBuff = 2;
    [Min(1)] public int golemMaxShieldBonus = 2;

    [Header("Gorgylia — антилечение")]
    [Min(0.1f)] public float gorgyliaAntiHealDuration = 5f;

    [Header("Orc — разница HP щитов")]
    [Min(0f)] public float orcDamagePerShieldHpDifference = 0.02f;
    [Min(1f)] public float orcMaxDamageMultiplier = 2.5f;

    [Header("Paladin — лечение своего щита")]
    [Min(1)] public int paladinSelfHealAmount = 2;

    [Header("Skelet — урон при промахе")]
    [Min(1)] public int skeletMissDamage = 1;

    [Header("Wampir — вампиризм")]
    [Range(0f, 1f)] public float wampirLifestealPercent = 0.5f;

    [Header("Warior — крит при низком HP кольца врага (0.3 = 30%)")]
    [Range(0f, 1f)] public float wariorEnemyShieldThreshold = 0.3f;

    [Header("Zombie — яд")]
    [Range(0f, 1f)] public float zombiePoisonChance = 0.3f;
    [Min(1)] public int zombiePoisonDamagePerTick = 1;
    [Min(0.1f)] public float zombiePoisonTickInterval = 1.2f;
    [Min(0.1f)] public float zombiePoisonDuration = 4f;
}
