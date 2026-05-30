using UnityEngine;

/// <summary>Игровые настройки меню (заглушки для функций без полной реализации).</summary>
public static class MenuGameSettings
{
    public const float MinVolume = 0f;
    public const float MaxVolume = 1f;

    public static event System.Action SettingsChanged;

    static bool _pushAlarmOn = true;
    static float _musicVolume = 0.5f;
    static bool _vibrationOn;

    public static bool PushAlarmOn
    {
        get => SaveManager.Instance != null ? SaveManager.Instance.Data.PushAlarmOn : _pushAlarmOn;
        set
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.PushAlarmOn = value;
                SaveManager.Instance.Save();
            }
            else
                _pushAlarmOn = value;

            SettingsChanged?.Invoke();
        }
    }

    public static float MusicVolume
    {
        get => SaveManager.Instance != null
            ? Mathf.Clamp(SaveManager.Instance.Data.MusicVolume, MinVolume, MaxVolume)
            : _musicVolume;
        set
        {
            value = Mathf.Clamp(value, MinVolume, MaxVolume);
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.MusicVolume = value;
                SaveManager.Instance.Save();
            }
            else
                _musicVolume = value;

            SettingsChanged?.Invoke();
        }
    }

    public static bool VibrationOn
    {
        get => SaveManager.Instance != null ? SaveManager.Instance.Data.VibrationOn : _vibrationOn;
        set
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.VibrationOn = value;
                SaveManager.Instance.Save();
            }
            else
                _vibrationOn = value;

            SettingsChanged?.Invoke();
        }
    }

    public static void LoadFromSave()
    {
        if (SaveManager.Instance == null)
            return;

        _pushAlarmOn = SaveManager.Instance.Data.PushAlarmOn;
        _musicVolume = SaveManager.Instance.Data.MusicVolume;
        _vibrationOn = SaveManager.Instance.Data.VibrationOn;
    }
}
