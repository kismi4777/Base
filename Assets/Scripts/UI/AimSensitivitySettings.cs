using UnityEngine;

/// <summary>
/// Чувствительность прицеливания (множитель длины натяжения). Сохраняется в PlayerPrefs.
/// </summary>
public static class AimSensitivitySettings
{
    const string PrefKey = "aim_sensitivity";

    public const float MinSensitivity = 0.2f;
    public const float MaxSensitivity = 1.5f;
    public const float DefaultSensitivity = 0.5f;

    public static float Sensitivity
    {
        get => PlayerPrefs.GetFloat(PrefKey, DefaultSensitivity);
        set
        {
            PlayerPrefs.SetFloat(PrefKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
            PlayerPrefs.Save();
        }
    }
}
