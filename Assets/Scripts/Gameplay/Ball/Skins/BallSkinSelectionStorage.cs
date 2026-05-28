using UnityEngine;

/// <summary>Сохранение выбранного скина между сессиями.</summary>
public static class BallSkinSelectionStorage
{
    const string PlayerPrefsKey = "SelectedBallSkinId";

    public static BallSkinId SelectedSkin { get; private set; } = BallSkinId.Defolt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadOnBoot() => Load();

    public static void Load()
    {
        int saved = PlayerPrefs.GetInt(PlayerPrefsKey, (int)BallSkinId.Defolt);
        if (!System.Enum.IsDefined(typeof(BallSkinId), saved))
            saved = (int)BallSkinId.Defolt;

        SelectedSkin = (BallSkinId)saved;
    }

    public static void Save(BallSkinId skinId)
    {
        SelectedSkin = skinId;
        PlayerPrefs.SetInt(PlayerPrefsKey, (int)skinId);
        PlayerPrefs.Save();
    }
}
