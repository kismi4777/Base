using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Отображаемые имена и описания способностей для UI выбора скина.</summary>
[CreateAssetMenu(fileName = "BallSkinCatalog", menuName = "Gameplay/Ball Skin Catalog")]
public sealed class BallSkinCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public BallSkinId skinId;
        public string displayName;
        [TextArea(2, 5)] public string abilityDescription;
    }

    [SerializeField] Entry[] entries;

    public IReadOnlyList<Entry> Entries => entries;

    public bool TryGetEntry(BallSkinId skinId, out Entry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].skinId == skinId)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    public int IndexOf(BallSkinId skinId)
    {
        EnsurePopulated();
        if (entries == null)
            return -1;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].skinId == skinId)
                return i;
        }

        return -1;
    }

    public Entry GetEntryByIndex(int index)
    {
        EnsurePopulated();
        if (entries == null || entries.Length == 0)
            return default;

        index = Mathf.Clamp(index, 0, entries.Length - 1);
        return entries[index];
    }

    public int EntryCount
    {
        get
        {
            EnsurePopulated();
            return entries != null ? entries.Length : 0;
        }
    }

    public void EnsurePopulated()
    {
        if (entries == null || entries.Length == 0)
            entries = CreateDefaultEntries();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (entries != null && entries.Length > 0)
            return;

        entries = CreateDefaultEntries();
    }
#endif

    public static Entry[] CreateDefaultEntries() => new[]
    {
        EntryFor(BallSkinId.Defolt, "Обычный", "Без особой способности."),
        EntryFor(BallSkinId.Dragon, "Дракон", "Чем дольше полёт мяча, тем выше урон."),
        EntryFor(BallSkinId.Fire, "Огонь", "Два гола подряд в чужое кольцо (два броска без промаха между ними) поджигают его: периодический урон по HP."),
        EntryFor(BallSkinId.Gnom, "Гном", "Отскок от щита: критический урон и немного золота."),
        EntryFor(BallSkinId.Goblin, "Гоблин", "Каждый 3-й точный гол: крит и кража золота у врага."),
        EntryFor(BallSkinId.Golem, "Голем", "Два гола подряд увеличивают максимальное HP вашего щита."),
        EntryFor(BallSkinId.Gorgylia, "Горгилия", "Гол накладывает антилечение на врага."),
        EntryFor(BallSkinId.Orc, "Орк", "Урон выше, чем больше разница HP щитов."),
        EntryFor(BallSkinId.Paladin, "Паладин", "Гол в своё кольцо восстанавливает HP кольца и щита."),
        EntryFor(BallSkinId.Ricar, "Рыцарь", "Урон сквозь защиту и щит врага."),
        EntryFor(BallSkinId.Skelet, "Скелет", "Промах по дужке или щиту наносит небольшой урон."),
        EntryFor(BallSkinId.Wampir, "Вампир", "50% урона от гола восстанавливает ваш щит."),
        EntryFor(BallSkinId.Warior, "Воин", "Если у кольца врага 30% HP или меньше — каждый гол наносит двойной урон."),
        EntryFor(BallSkinId.Zombie, "Зомби", "30% шанс наложить яд при броске."),
        EntryFor(BallSkinId.MyBall, "Мой мяч", "Косметический скин без способности.")
    };

    static Entry EntryFor(BallSkinId id, string title, string description) => new()
    {
        skinId = id,
        displayName = title,
        abilityDescription = description
    };
}
