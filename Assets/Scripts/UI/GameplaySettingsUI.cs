using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplaySettingsUI : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Button openSettingsButton;
    [SerializeField] Button closeSettingsButton;
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] TMP_Text sensitivityValueLabel;

    void Awake()
    {
        DisableLabelRaycasts(openSettingsButton);
        DisableLabelRaycasts(closeSettingsButton);

        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(OpenSettings);
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = AimSensitivitySettings.MinSensitivity;
            sensitivitySlider.maxValue = AimSensitivitySettings.MaxSensitivity;
            sensitivitySlider.value = AimSensitivitySettings.Sensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        CloseSettings();
        UpdateSensitivityLabel();
    }

    void OnDestroy()
    {
        if (openSettingsButton != null)
            openSettingsButton.onClick.RemoveListener(OpenSettings);
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.RemoveListener(CloseSettings);
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        UpdateSensitivityLabel();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void OnSensitivityChanged(float value)
    {
        AimSensitivitySettings.Sensitivity = value;
        UpdateSensitivityLabel();
    }

    void UpdateSensitivityLabel()
    {
        if (sensitivityValueLabel == null || sensitivitySlider == null)
            return;

        sensitivityValueLabel.text = $"{Mathf.RoundToInt(sensitivitySlider.value * 100)}%";
    }

    static void DisableLabelRaycasts(Button button)
    {
        if (button == null)
            return;

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
            labels[i].raycastTarget = false;
    }
}
