using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Главное меню: хаб и навигация по экранам (магазин, персонажи, настройки, задания).</summary>
public sealed partial class MainMenuUI : MonoBehaviour
{
    [Header("Данные")]
    [SerializeField] BallSkinCatalog skinCatalog;

    [Header("Связи")]
    [SerializeField] Canvas targetCanvas;
    [SerializeField] TMP_FontAsset fontAsset;

    [Header("Оформление хаба")]
    [SerializeField] Color playBlockColor = new(0.12f, 0.22f, 0.14f, 0.95f);
    [SerializeField] Color shopBlockColor = new(0.18f, 0.1f, 0.22f, 0.95f);
    [SerializeField] Color charactersBlockColor = new(0.1f, 0.14f, 0.24f, 0.95f);

    MainMenuUiStyle _style;
    MainMenuToast _toast;

    GameObject _hubRoot;
    GameObject _settingsOverlay;
    GameObject _languageOverlay;

    MainMenuShopPanel _shopPanel;
    MainMenuShopPanel _goldShopPanel;
    MainMenuCharactersPanel _charactersPanel;
    MainMenuProfilePanel _profilePanel;
    MainMenuDailyTaskPanel _dailyTaskPanel;

    TMP_Text _playerNameLabel;
    TMP_Text _levelBadgeLabel;
    TMP_Text _xpLabel;
    Image _xpFill;
    TMP_Text _goldLabel;

    TMP_Text _dailyTitle;
    TMP_Text _dailyTimerLabel;
    readonly List<TMP_Text> _taskTitleLabels = new();
    readonly List<TMP_Text> _taskProgressLabels = new();
    readonly List<Image> _taskProgressFills = new();

    TMP_Text _settingsTitle;
    Slider _soundFxSlider;
    Slider _musicSlider;
    Button _languageOpenButton;
    TMP_Text _languagePreviewLabel;
    Button _langRuButton;
    Button _langEnButton;
    Button _langTrButton;
    Button _languageCloseButton;

    TMP_Text _playNavLabel;
    TMP_Text _shopNavLabel;
    TMP_Text _charsNavLabel;

    MainMenuDailyTaskPanel.TaskDefinition[] _dailyTasks;

    void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        _style = new MainMenuUiStyle { Font = fontAsset ?? MainMenuUiFantasyAssets.LoadFont() };

        FixCanvasForInput();
        EnsureEventSystemInputModule();
        EnsureSkinCatalog();
        InitDailyTaskDefinitions();

        MenuLocalization.LoadFromSave();
        MenuGameSettings.LoadFromSave();
        MenuAudioSettings.LoadAndApply();
        BallSkinSelectionStorage.Load();
        PlayerProgressUtility.EnsureStarterProfile();
        EnsureDemoDailyProgress();

        GetOrCreateToast();

        if (useSceneUi && TryLoadSceneUi())
        {
            CloseAllOverlays();
            RefreshHub();
            return;
        }

        BuildHub();
        BuildPanels();
        BuildSettingsOverlay();
        BuildLanguageOverlay();
        CloseAllOverlays();
        SaveUiReferences();
        WireUiListeners(GetComponent<MainMenuUiReferences>());
        RefreshHub();
    }

    void OnEnable()
    {
        MenuLocalization.LanguageChanged += OnLanguageChanged;
        MenuAudioSettings.SettingsChanged += RefreshSettingsPanel;
        MenuGameSettings.SettingsChanged += RefreshSettingsPanel;
    }

    void OnDisable()
    {
        MenuLocalization.LanguageChanged -= OnLanguageChanged;
        MenuAudioSettings.SettingsChanged -= RefreshSettingsPanel;
        MenuGameSettings.SettingsChanged -= RefreshSettingsPanel;
    }

    void InitDailyTaskDefinitions()
    {
        _dailyTasks = new[]
        {
            new MainMenuDailyTaskPanel.TaskDefinition
            {
                Icon = "🏀",
                TitleRu = "Забей 10 бросков",
                TitleEn = "Score 10 shots",
                DescRu = "Забейте 10 успешных бросков в матчах.",
                DescEn = "Score 10 successful shots in matches.",
                Target = 10,
                RewardXp = 200,
                GetProgress = d => d.DailyShotsProgress,
                IsClaimed = d => d.DailyReward0Claimed,
                SetClaimed = d => d.DailyReward0Claimed = true
            },
            new MainMenuDailyTaskPanel.TaskDefinition
            {
                Icon = "⚔",
                TitleRu = "Сыграй 3 матча",
                TitleEn = "Play 3 matches",
                DescRu = "Завершите 3 PvP-матча.",
                DescEn = "Complete 3 PvP matches.",
                Target = 3,
                RewardXp = 300,
                GetProgress = d => d.DailyMatchesProgress,
                IsClaimed = d => d.DailyReward1Claimed,
                SetClaimed = d => d.DailyReward1Claimed = true
            },
            new MainMenuDailyTaskPanel.TaskDefinition
            {
                Icon = "🏆",
                TitleRu = "Выиграй матч",
                TitleEn = "Win a match",
                DescRu = "Одержите победу в одном матче.",
                DescEn = "Win a single match.",
                Target = 1,
                RewardXp = 500,
                GetProgress = d => d.DailyWinsProgress,
                IsClaimed = d => d.DailyReward2Claimed,
                SetClaimed = d => d.DailyReward2Claimed = true
            }
        };
    }

    void EnsureSkinCatalog()
    {
        if (skinCatalog != null)
            return;

#if UNITY_EDITOR
        skinCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<BallSkinCatalog>("Assets/Data/BallSkinCatalog.asset");
#endif
        if (skinCatalog == null)
        {
            skinCatalog = ScriptableObject.CreateInstance<BallSkinCatalog>();
            skinCatalog.EnsurePopulated();
            Debug.LogWarning("MainMenuUI: BallSkinCatalog не назначен, используются значения по умолчанию.", this);
        }
    }

    void EnsureDemoDailyProgress()
    {
        if (!PlayerProgressUtility.HasSave)
            return;

        PlayerData data = PlayerProgressUtility.Data;
        if (data.DailyShotsProgress == 0 && data.DailyMatchesProgress == 0 && data.DailyWinsProgress == 0
            && !data.DailyReward0Claimed && !data.DailyReward1Claimed && !data.DailyReward2Claimed)
        {
            data.DailyShotsProgress = 6;
            data.DailyMatchesProgress = 1;
            data.DailyWinsProgress = 0;
        }
    }

    void FixCanvasForInput()
    {
        if (targetCanvas == null)
            return;

        CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
            targetCanvas.gameObject.AddComponent<GraphicRaycaster>();

        Canvas.ForceUpdateCanvases();
    }

    void EnsureEventSystemInputModule()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esGo = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem = esGo.GetComponent<EventSystem>();
        }

        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone == null)
            standalone = eventSystem.gameObject.AddComponent<StandaloneInputModule>();

        BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
        for (int i = 0; i < modules.Length; i++)
            modules[i].enabled = modules[i] == standalone;
    }

    void BuildPanels()
    {
        _shopPanel = new MainMenuShopPanel(targetCanvas, _style, isGoldShop: false, _toast, OnOverlayClosed);
        _goldShopPanel = new MainMenuShopPanel(targetCanvas, _style, isGoldShop: true, _toast, OnOverlayClosed);
        _charactersPanel = new MainMenuCharactersPanel(targetCanvas, _style, skinCatalog, _toast, OnOverlayClosed);
        _profilePanel = new MainMenuProfilePanel(targetCanvas, _style, _toast, OnOverlayClosed);
        _dailyTaskPanel = new MainMenuDailyTaskPanel(targetCanvas, _style, _dailyTasks, _toast, RefreshHub, OnOverlayClosed);

        _shopPanel.Build();
        _goldShopPanel.Build();
        _charactersPanel.Build();
        _profilePanel.Build();
        _dailyTaskPanel.Build();
    }

    public void StartGame()
    {
        CloseAllOverlays();
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    void CloseAllOverlays()
    {
        _languageOverlay?.SetActive(false);
        _settingsOverlay?.SetActive(false);
        _shopPanel?.CloseSilently();
        _goldShopPanel?.CloseSilently();
        _charactersPanel?.CloseSilently();
        _profilePanel?.CloseSilently();
        _dailyTaskPanel?.CloseSilently();
        ShowHub();
    }

    void ShowHub()
    {
        if (_hubRoot != null)
            _hubRoot.SetActive(true);
    }

    void OnOverlayClosed()
    {
        ShowHub();
        RefreshHub();
    }

    void OpenSettings()
    {
        CloseAllOverlays();
        if (_hubRoot != null)
            _hubRoot.SetActive(true);

        if (_settingsOverlay != null)
        {
            _settingsOverlay.SetActive(true);
            _settingsOverlay.transform.SetAsLastSibling();
        }

        RefreshSettingsPanel();
    }

    void OpenShop()
    {
        CloseAllOverlays();
        _hubRoot?.SetActive(false);
        _shopPanel.Open();
    }

    void OpenGoldShop()
    {
        CloseAllOverlays();
        _hubRoot?.SetActive(false);
        _goldShopPanel.Open();
    }

    void OpenCharacters()
    {
        CloseAllOverlays();
        _hubRoot?.SetActive(false);
        _charactersPanel.Open();
    }

    void OpenProfile()
    {
        CloseAllOverlays();
        _hubRoot?.SetActive(false);
        _profilePanel.Open();
    }

    void OpenDailyTask(int index)
    {
        CloseAllOverlays();
        _hubRoot?.SetActive(false);
        _dailyTaskPanel.OpenTask(index);
    }

    void OnLanguageChanged()
    {
        RefreshHub();
        RefreshLanguageOverlay();
    }

    void OpenLanguageOverlay()
    {
        if (_languageOverlay == null)
            return;

        _languageOverlay.SetActive(true);
        _languageOverlay.transform.SetAsLastSibling();
        RefreshLanguageOverlay();
    }

    void CloseLanguageOverlay()
    {
        if (_languageOverlay != null)
            _languageOverlay.SetActive(false);

        RefreshSettingsPanel();
    }

    void RefreshLanguageOverlay()
    {
        SetLanguageFlagSelected(_langRuButton, MenuLocalization.Current == MenuLanguage.Russian);
        SetLanguageFlagSelected(_langEnButton, MenuLocalization.Current == MenuLanguage.English);
        SetLanguageFlagSelected(_langTrButton, MenuLocalization.Current == MenuLanguage.Turkish);
    }

    void RefreshHub()
    {
        RefreshProfile();
        RefreshGold();
        RefreshDailyTasks();
        RefreshNavLabels();
        RefreshSettingsPanel();
    }

    void RefreshProfile()
    {
        if (!PlayerProgressUtility.HasSave)
            return;

        PlayerData data = PlayerProgressUtility.Data;
        if (_playerNameLabel != null)
            _playerNameLabel.text = data.PlayerName;

        if (_levelBadgeLabel != null)
            _levelBadgeLabel.text = data.Level.ToString();

        int toNext = Mathf.Max(1, data.ExperienceToNextLevel);
        int xp = Mathf.Clamp(data.Experience, 0, toNext);

        if (_xpLabel != null)
            _xpLabel.text = $"{xp} / {toNext}";

        Slider xpSlider = _xpFill != null ? _xpFill.GetComponentInParent<Slider>() : null;
        if (xpSlider != null)
            xpSlider.value = (float)xp / toNext;
        else if (_xpFill != null)
            _xpFill.fillAmount = (float)xp / toNext;
    }

    void RefreshGold()
    {
        if (_goldLabel == null || !PlayerProgressUtility.HasSave)
            return;

        _goldLabel.text = PlayerProgressUtility.Data.Coins.ToString("N0").Replace(",", " ");
    }

    void RefreshDailyTasks()
    {
        if (_dailyTitle != null)
            _dailyTitle.text = MenuLocalization.Get("ежедневные миссии", "daily missions", "günlük görevler");

        if (_dailyTimerLabel != null)
            _dailyTimerLabel.text = MenuLocalization.Get("до обновления: 12ч 45м", "until update: 12h 45m",
                "yenileme: 12s 45dk");

        if (!PlayerProgressUtility.HasSave)
            return;

        PlayerData data = PlayerProgressUtility.Data;

        for (int i = 0; i < _dailyTasks.Length && i < _taskTitleLabels.Count; i++)
        {
            MainMenuDailyTaskPanel.TaskDefinition task = _dailyTasks[i];
            int progress = Mathf.Clamp(task.GetProgress(data), 0, task.Target);
            bool claimed = task.IsClaimed(data);

            _taskTitleLabels[i].text = MenuLocalization.Get(task.TitleRu, task.TitleEn);

            if (i < _taskProgressLabels.Count)
                _taskProgressLabels[i].text = claimed
                    ? MenuLocalization.Get("Готово", "Done", "Tamamlandı")
                    : $"{progress} / {task.Target}";

            if (i < _taskProgressFills.Count)
            {
                float amount = task.Target > 0 ? (float)progress / task.Target : 0f;
                _taskProgressFills[i].fillAmount = amount;

                Slider slider = _taskProgressFills[i].GetComponentInParent<Slider>();
                if (slider != null)
                    slider.value = amount;
            }
        }
    }

    void RefreshNavLabels()
    {
        if (_playNavLabel != null)
        {
            _playNavLabel.text = MenuLocalization.Get("ИГРАТЬ", "PLAY");
            _playNavLabel.enableWordWrapping = false;
            _playNavLabel.enableAutoSizing = true;
            _playNavLabel.fontSizeMin = 22f;
            _playNavLabel.fontSizeMax = 46f;
        }

        if (_shopNavLabel != null)
            _shopNavLabel.text = MenuLocalization.Get("МАГАЗИН", "SHOP");

        if (_charsNavLabel != null)
            _charsNavLabel.text = MenuLocalization.Get("ПЕРСОНАЖИ", "CHARACTERS");
    }

    void RefreshSettingsPanel()
    {
        if (_settingsTitle != null)
            _settingsTitle.text = MenuLocalization.Get("НАСТРОЙКИ", "SETTINGS", "AYARLAR");

        if (_soundFxSlider != null)
        {
            _soundFxSlider.minValue = MenuAudioSettings.MinVolume;
            _soundFxSlider.maxValue = MenuAudioSettings.MaxVolume;
            _soundFxSlider.SetValueWithoutNotify(MenuAudioSettings.Volume);
        }

        if (_musicSlider != null)
        {
            _musicSlider.minValue = MenuGameSettings.MinVolume;
            _musicSlider.maxValue = MenuGameSettings.MaxVolume;
            _musicSlider.SetValueWithoutNotify(MenuGameSettings.MusicVolume);
        }

        if (_languagePreviewLabel != null)
            _languagePreviewLabel.text = MenuLocalization.GetLanguageDisplayName();

        if (_settingsOverlay != null)
        {
            Transform settings = MainMenuUiFantasyAssets.FindDeepChild(_settingsOverlay.transform, "Settings");
            if (settings != null)
                MainMenuUiFantasyConfigurator.LocalizeSettingsLabels(settings);
        }
    }

    void OnSoundFxChanged(float value) => MenuAudioSettings.Volume = value;

    void OnMusicChanged(float value) => MenuGameSettings.MusicVolume = value;

    void SetLanguage(MenuLanguage language)
    {
        MenuLocalization.Current = language;
        MenuLocalization.SaveToPlayerData();
        CloseLanguageOverlay();
    }

    static void SetLanguageFlagSelected(Button button, bool selected)
    {
        if (button == null)
            return;

        Transform check = button.transform.Find("Check");
        if (check != null)
        {
            check.gameObject.SetActive(selected);
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected ? new Color(0.35f, 0.62f, 0.98f, 1f) : Color.white;
    }

    void BuildHub()
    {
        if (targetCanvas == null || _hubRoot != null)
            return;

        _hubRoot = MainMenuUiFactory.CreateUiObject("MainMenuHUD", targetCanvas.transform);
        MainMenuUiFactory.StretchFullScreen(_hubRoot);

        MainMenuUiFantasyAssets.CreateHomeBackground(_hubRoot.transform);

        GameObject homeInstance = MainMenuUiFantasyAssets.InstantiatePrefab(
            MainMenuUiFantasyAssets.HomePanelPath, _hubRoot.transform, "HomePanel");
        if (homeInstance != null)
            MainMenuUiFantasyAssets.StretchFullScreen(homeInstance);

        ApplyFantasyHomeBindings();
    }

    void ApplyFantasyHomeBindings()
    {
        MainMenuUiFantasyConfigurator.ConfigureHomeHub(_hubRoot.transform, _style, _dailyTasks, OpenDailyTask);

        MainMenuUiFantasyAssets.HomeBindings bindings = MainMenuUiFantasyAssets.BindHomeHub(_hubRoot.transform);

        _playerNameLabel = bindings.PlayerNameLabel;
        _levelBadgeLabel = bindings.LevelBadgeLabel;
        _xpLabel = bindings.XpLabel;
        _xpFill = bindings.XpFill;
        _goldLabel = bindings.GoldLabel;

        MainMenuUiFantasyConfigurator.DailyTasksUi dailyUi = MainMenuUiFantasyConfigurator.BuildDailyTasksPanel(
            _hubRoot.transform, _style, _dailyTasks, OpenDailyTask);

        _dailyTitle = dailyUi.TitleLabel;
        _dailyTimerLabel = dailyUi.TimerLabel;
        _taskTitleLabels.Clear();
        _taskTitleLabels.AddRange(dailyUi.TaskTitleLabels);
        _taskProgressLabels.Clear();
        _taskProgressLabels.AddRange(dailyUi.TaskProgressLabels);
        _taskProgressFills.Clear();
        _taskProgressFills.AddRange(dailyUi.TaskProgressFills);

        _playNavLabel = bindings.PlayLabel;
        _shopNavLabel = bindings.ShopLabel;
        _charsNavLabel = bindings.CharactersLabel;

        WireButton(bindings.GoldPlusButton, OpenGoldShop);
        WireButton(bindings.SettingsButton, OpenSettings);
        WireButton(bindings.PlayButton, StartGame);
        WireButton(bindings.ShopButton, OpenShop);
        WireButton(bindings.CharactersButton, OpenCharacters);
    }

    void BuildSettingsOverlay()
    {
        _settingsOverlay = MainMenuUiFactory.CreateUiObject("SettingsOverlay", targetCanvas.transform);
        MainMenuUiFactory.StretchFullScreen(_settingsOverlay);

        GameObject settingsPanel = MainMenuUiFantasyAssets.InstantiatePrefab(
            MainMenuUiFantasyAssets.SettingsPanelPath, _settingsOverlay.transform, "SettingsPanel");
        if (settingsPanel != null)
            MainMenuUiFantasyAssets.StretchFullScreen(settingsPanel);

        MainMenuUiFantasyConfigurator.FantasySettingsUi settingsUi =
            MainMenuUiFantasyConfigurator.ConfigureSettingsOverlay(_settingsOverlay, _style);

        _settingsTitle = settingsUi.TitleLabel;
        _soundFxSlider = settingsUi.SoundFxSlider;
        _musicSlider = settingsUi.MusicSlider;
        _languageOpenButton = settingsUi.LanguageOpenButton;
        _languagePreviewLabel = settingsUi.LanguagePreviewLabel;

        if (_soundFxSlider != null)
            _soundFxSlider.onValueChanged.AddListener(OnSoundFxChanged);

        if (_musicSlider != null)
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);

        WireButton(settingsUi.CloseButton, CloseAllOverlays);
        WireButton(_languageOpenButton, OpenLanguageOverlay);
        _settingsOverlay.SetActive(false);
    }

    void BuildLanguageOverlay()
    {
        _languageOverlay = MainMenuUiFactory.CreateUiObject("LanguageOverlay", targetCanvas.transform);
        MainMenuUiFactory.StretchFullScreen(_languageOverlay);

        GameObject languagePanel = MainMenuUiFantasyAssets.InstantiatePrefab(
            MainMenuUiFantasyAssets.LanguagePanelPath, _languageOverlay.transform, "LanguagePanel");
        if (languagePanel != null)
            MainMenuUiFantasyAssets.StretchFullScreen(languagePanel);

        MainMenuUiFantasyConfigurator.FantasyLanguageUi languageUi =
            MainMenuUiFantasyConfigurator.ConfigureLanguageOverlay(_languageOverlay);

        _languageCloseButton = languageUi.CloseButton;
        _langRuButton = languageUi.RuButton;
        _langEnButton = languageUi.EnButton;
        _langTrButton = languageUi.TrButton;

        WireButton(_languageCloseButton, CloseLanguageOverlay);
        WireButton(_langRuButton, () => SetLanguage(MenuLanguage.Russian));
        WireButton(_langEnButton, () => SetLanguage(MenuLanguage.English));
        WireButton(_langTrButton, () => SetLanguage(MenuLanguage.Turkish));
        _languageOverlay.SetActive(false);
    }
}
