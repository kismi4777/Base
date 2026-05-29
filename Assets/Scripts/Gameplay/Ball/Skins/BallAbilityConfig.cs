using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Настройки чисел для способностей скинов мяча.</summary>
[CreateAssetMenu(fileName = "BallAbilityConfig", menuName = "Gameplay/Ball Ability Config")]
public sealed class BallAbilityConfig : ScriptableObject
{
    [Serializable]
    public struct SkinScoreDamageEntry
    {
        public BallSkinId skinId;
        [Min(1)] public int baseScoreDamage;
    }

    [Header("Общее")]
    [FormerlySerializedAs("baseScoreDamage")]
    [Tooltip("Урон при голе, если для скина нет отдельной записи ниже.")]
    [Min(1)] public int defaultScoreDamage = 10;
    [Min(1f)] public float critMultiplier = 2f;

    [Header("Урон при голе — отдельно для каждого скина")]
    [SerializeField] SkinScoreDamageEntry[] skinScoreDamages;

    public int GetBaseScoreDamage(BallSkinId skinId)
    {
        EnsureSkinDamagesPopulated();

        if (skinScoreDamages != null)
        {
            for (int i = 0; i < skinScoreDamages.Length; i++)
            {
                if (skinScoreDamages[i].skinId == skinId)
                    return Mathf.Max(1, skinScoreDamages[i].baseScoreDamage);
            }
        }

        return Mathf.Max(1, defaultScoreDamage);
    }

    public void EnsureSkinDamagesPopulated()
    {
        BallSkinId[] allSkins = (BallSkinId[])Enum.GetValues(typeof(BallSkinId));
        if (skinScoreDamages == null || skinScoreDamages.Length == 0)
        {
            skinScoreDamages = CreateDefaultSkinDamages(allSkins, defaultScoreDamage);
            return;
        }

        bool changed = false;
        var merged = new List<SkinScoreDamageEntry>(skinScoreDamages);
        for (int i = 0; i < allSkins.Length; i++)
        {
            BallSkinId skin = allSkins[i];
            if (ContainsSkin(merged, skin))
                continue;

            merged.Add(new SkinScoreDamageEntry
            {
                skinId = skin,
                baseScoreDamage = defaultScoreDamage
            });
            changed = true;
        }

        if (changed)
            skinScoreDamages = merged.ToArray();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (defaultScoreDamage < 1)
            defaultScoreDamage = 1;

        EnsureSkinDamagesPopulated();
    }
#endif

    static bool ContainsSkin(List<SkinScoreDamageEntry> list, BallSkinId skinId)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].skinId == skinId)
                return true;
        }

        return false;
    }

    static SkinScoreDamageEntry[] CreateDefaultSkinDamages(BallSkinId[] allSkins, int damage)
    {
        damage = Mathf.Max(1, damage);
        var entries = new SkinScoreDamageEntry[allSkins.Length];
        for (int i = 0; i < allSkins.Length; i++)
        {
            entries[i] = new SkinScoreDamageEntry
            {
                skinId = allSkins[i],
                baseScoreDamage = damage
            };
        }

        return entries;
    }

    [Header("Dragon — множитель урона от дистанции полёта")]
    [Tooltip("Множитель = метры × это значение (минимум 1). То же число показывается на иконке.")]
    [Min(0f)] public float dragonDamagePerMeter = 1f;
    [Tooltip("Потолок множителя урона (например 15 = не больше ×15 к базовому урону).")]
    [Min(1f)] public float dragonMaxDamageBonus = 15f;

    /// <summary>Множитель урона по пройденным метрам (то же значение, что на иконке).</summary>
    public float ComputeDragonDamageMultiplier(float flightDistanceMeters)
    {
        float multiplier = Mathf.Max(1f, flightDistanceMeters * dragonDamagePerMeter);
        return Mathf.Min(multiplier, dragonMaxDamageBonus);
    }

    /// <summary>Итоговый урон гола для Дракона с учётом множителя.</summary>
    public int ComputeDragonScoreDamage(float flightDistanceMeters)
    {
        int baseDamage = GetBaseScoreDamage(BallSkinId.Dragon);
        float multiplier = ComputeDragonDamageMultiplier(flightDistanceMeters);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    /// <summary>Текст множителя для UI (не метры полёта).</summary>
    public string FormatDragonMultiplierForUi(float flightDistanceMeters)
    {
        float multiplier = ComputeDragonDamageMultiplier(flightDistanceMeters);
        return multiplier.ToString("0.#");
    }

    [Header("Fire — поджог кольца")]
    [Tooltip("Голов подряд без промаха, чтобы разблокировать поджог; горение срабатывает на следующий гол.")]
    [Min(1)] public int fireConsecutiveHitsToIgnite = 1;
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
