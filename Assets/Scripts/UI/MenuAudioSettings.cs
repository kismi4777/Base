using UnityEngine;

public static class MenuAudioSettings
{
    public const float MinVolume = 0f;
    public const float MaxVolume = 1f;

    public static event System.Action SettingsChanged;

    public static bool IsSoundOn
    {
        get => SaveManager.Instance != null && SaveManager.Instance.Data.IsSoundOn;
        set
        {
            if (SaveManager.Instance == null)
                return;

            SaveManager.Instance.Data.IsSoundOn = value;
            Apply();
            SaveManager.Instance.Save();
            SettingsChanged?.Invoke();
        }
    }

    public static float Volume
    {
        get => SaveManager.Instance != null
            ? Mathf.Clamp(SaveManager.Instance.Data.SoundVolume, MinVolume, MaxVolume)
            : 1f;
        set
        {
            if (SaveManager.Instance == null)
                return;

            SaveManager.Instance.Data.SoundVolume = Mathf.Clamp(value, MinVolume, MaxVolume);
            Apply();
            SaveManager.Instance.Save();
            SettingsChanged?.Invoke();
        }
    }

    public static void LoadAndApply()
    {
        Apply();
    }

    public static void Apply()
    {
        if (SaveManager.Instance == null)
            return;

        bool on = SaveManager.Instance.Data.IsSoundOn;
        float volume = Mathf.Clamp(SaveManager.Instance.Data.SoundVolume, MinVolume, MaxVolume);
        AudioListener.volume = on ? volume : 0f;
    }
}
