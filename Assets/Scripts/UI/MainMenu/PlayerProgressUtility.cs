using UnityEngine;

public static class PlayerProgressUtility
{
    public static bool HasSave => SaveManager.Instance != null;

    public static PlayerData Data => SaveManager.Instance?.Data;

    public static void EnsureStarterProfile()
    {
        if (!HasSave)
            return;

        PlayerData data = Data;
        if (string.IsNullOrWhiteSpace(data.PlayerName))
            data.PlayerName = MenuLocalization.Get("Игрок", "Player");

        if (data.ExperienceToNextLevel <= 0)
            data.ExperienceToNextLevel = 100;

        if (data.Level < 1)
            data.Level = 1;

        if (data.Coins <= 0 && data.Level == 1 && data.Experience == 0 && data.HighScore == 0)
            data.Coins = 12450;
    }

    public static void AddExperience(int amount)
    {
        if (!HasSave || amount <= 0)
            return;

        PlayerData data = Data;
        data.Experience += amount;

        while (data.Experience >= data.ExperienceToNextLevel)
        {
            data.Experience -= data.ExperienceToNextLevel;
            data.Level++;
            data.ExperienceToNextLevel = Mathf.Max(100, Mathf.RoundToInt(data.ExperienceToNextLevel * 1.15f));
        }

        SaveManager.Instance.Save();
    }

    public static bool TrySpendCoins(int price)
    {
        if (!HasSave || price <= 0)
            return false;

        if (Data.Coins < price)
            return false;

        Data.Coins -= price;
        SaveManager.Instance.Save();
        return true;
    }

    public static void AddCoins(int amount)
    {
        if (!HasSave || amount <= 0)
            return;

        Data.Coins += amount;
        SaveManager.Instance.Save();
    }
}
