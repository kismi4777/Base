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
    TMP_Text _languageLabel;
    TMP_Text _soundLabel;
    TMP_Text _volumeLabel;
    Toggle _soundToggle;
    Slider _volumeSlider;
    Button _langRuButton;
    Button _langEnButton;

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

        _style = new MainMenuUiStyle { Font = fontAsset };

        FixCanvasForInput();
        EnsureEventSystemInputModule();
        EnsureSkinCatalog();
        InitDailyTaskDefinitions();

        MenuLocalization.LoadFromSave();
        MenuAudioSettings.LoadAndApply();
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
        CloseAllOverlays();
        SaveUiReferences();
        WireUiListeners(GetComponent<MainMenuUiReferences>());
        RefreshHub();
    }

    void OnEnable()
    {
        MenuLocalization.LanguageChanged += OnLanguageChanged;
        MenuAudioSettings.SettingsChanged += RefreshSettingsPanel;
    }

    void OnDisable()
    {
        MenuLocalization.LanguageChanged -= OnLanguageChanged;
        MenuAudioSettings.SettingsChanged -= RefreshSettingsPanel;
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

    void OnLanguageChanged() => RefreshHub();

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

        if (_xpFill != null)
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
            _dailyTitle.text = MenuLocalization.Get("ЕЖЕДНЕВНЫЕ ЗАДАНИЯ", "DAILY TASKS");

        if (_dailyTimerLabel != null)
            _dailyTimerLabel.text = MenuLocalization.Get("До обновления: 12ч 45м", "Until update: 12h 45m");

        if (!PlayerProgressUtility.HasSave)
            return;

        PlayerData data = PlayerProgressUtility.Data;

        for (int i = 0; i < _dailyTasks.Length && i < _taskTitleLabels.Count; i++)
        {
            MainMenuDailyTaskPanel.TaskDefinition task = _dailyTasks[i];
            int progress = Mathf.Clamp(task.GetProgress(data), 0, task.Target);
            bool claimed = task.IsClaimed(data);

            _taskTitleLabels[i].text = $"{task.Icon}  {MenuLocalization.Get(task.TitleRu, task.TitleEn)}";

            if (i < _taskProgressLabels.Count)
                _taskProgressLabels[i].text = claimed
                    ? MenuLocalization.Get("Готово", "Done")
                    : $"{progress} / {task.Target}";

            if (i < _taskProgressFills.Count)
                _taskProgressFills[i].fillAmount = task.Target > 0 ? (float)progress / task.Target : 0f;
        }
    }

    void RefreshNavLabels()
    {
        if (_playNavLabel != null)
            _playNavLabel.text = MenuLocalization.Get("ИГРАТЬ", "PLAY");

        if (_shopNavLabel != null)
            _shopNavLabel.text = MenuLocalization.Get("МАГАЗИН", "SHOP");

        if (_charsNavLabel != null)
            _charsNavLabel.text = MenuLocalization.Get("ПЕРСОНАЖИ", "CHARACTERS");
    }

    void RefreshSettingsPanel()
    {
        if (_settingsTitle != null)
            _settingsTitle.text = MenuLocalization.Get("НАСТРОЙКИ", "SETTINGS");

        if (_languageLabel != null)
            _languageLabel.text = MenuLocalization.Get("Язык", "Language");

        if (_soundLabel != null)
            _soundLabel.text = MenuLocalization.Get("Звук", "Sound");

        if (_volumeLabel != null)
            _volumeLabel.text = MenuLocalization.Get("Громкость", "Volume");

        if (_soundToggle != null)
            _soundToggle.SetIsOnWithoutNotify(MenuAudioSettings.IsSoundOn);

        if (_volumeSlider != null)
        {
            _volumeSlider.SetValueWithoutNotify(MenuAudioSettings.Volume);
            _volumeSlider.interactable = MenuAudioSettings.IsSoundOn;
        }

        bool isRu = MenuLocalization.Current == MenuLanguage.Russian;
        if (_langRuButton != null)
            _langRuButton.GetComponent<Image>().color = isRu ? _style.AccentBlue : _style.PanelColor;

        if (_langEnButton != null)
            _langEnButton.GetComponent<Image>().color = !isRu ? _style.AccentBlue : _style.PanelColor;
    }

    void OnSoundToggleChanged(bool on)
    {
        MenuAudioSettings.IsSoundOn = on;
        if (_volumeSlider != null)
            _volumeSlider.interactable = on;
    }

    void OnVolumeChanged(float value) => MenuAudioSettings.Volume = value;

    void SetLanguage(MenuLanguage language)
    {
        MenuLocalization.Current = language;
        MenuLocalization.SaveToPlayerData();
    }

    void BuildHub()
    {
        if (targetCanvas == null || _hubRoot != null)
            return;

        _hubRoot = MainMenuUiFactory.CreateUiObject("MainMenuHUD", targetCanvas.transform);
        MainMenuUiFactory.StretchFullScreen(_hubRoot);

        BuildTopLeftProfile(_hubRoot.transform);
        BuildTopRightBar(_hubRoot.transform);
        BuildDailyTasksPanel(_hubRoot.transform);
        BuildBottomNavigation(_hubRoot.transform);
    }

    void BuildTopLeftProfile(Transform parent)
    {
        GameObject panel = MainMenuUiFactory.CreatePanel("ProfilePanel", parent, _style,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(460f, 110f));

        Button profileButton = panel.AddComponent<Button>();
        profileButton.transition = Selectable.Transition.ColorTint;
        profileButton.targetGraphic = panel.GetComponent<Image>();
        profileButton.onClick.AddListener(OpenProfile);

        GameObject avatar = MainMenuUiFactory.CreateUiObject("Avatar", panel.transform);
        RectTransform avatarRect = avatar.GetComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0f, 0.5f);
        avatarRect.anchorMax = new Vector2(0f, 0.5f);
        avatarRect.pivot = new Vector2(0f, 0.5f);
        avatarRect.anchoredPosition = new Vector2(16f, 0f);
        avatarRect.sizeDelta = new Vector2(78f, 78f);
        Image avatarImage = avatar.AddComponent<Image>();
        avatarImage.color = new Color(0.35f, 0.28f, 0.22f, 1f);
        avatarImage.raycastTarget = false;

        GameObject levelBadge = MainMenuUiFactory.CreateUiObject("LevelBadge", avatar.transform);
        RectTransform badgeRect = levelBadge.GetComponent<RectTransform>();
        badgeRect.anchorMin = Vector2.zero;
        badgeRect.anchorMax = Vector2.zero;
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(6f, 6f);
        badgeRect.sizeDelta = new Vector2(34f, 34f);
        Image badgeBg = levelBadge.AddComponent<Image>();
        badgeBg.color = _style.AccentBlue;
        badgeBg.raycastTarget = false;
        _levelBadgeLabel = MainMenuUiFactory.CreateText("Level", levelBadge.transform, _style, "1", 18, FontStyles.Bold);
        MainMenuUiFactory.StretchFullScreen(_levelBadgeLabel.gameObject);
        _levelBadgeLabel.alignment = TextAlignmentOptions.Center;
        _levelBadgeLabel.raycastTarget = false;

        GameObject info = MainMenuUiFactory.CreateUiObject("Info", panel.transform);
        RectTransform infoRect = info.GetComponent<RectTransform>();
        infoRect.anchorMin = Vector2.zero;
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = new Vector2(108f, 14f);
        infoRect.offsetMax = new Vector2(-12f, -14f);

        _playerNameLabel = MainMenuUiFactory.CreateText("PlayerName", info.transform, _style, "Игрок", 28, FontStyles.Bold);
        RectTransform nameRect = _playerNameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = new Vector2(0f, 36f);
        _playerNameLabel.raycastTarget = false;

        GameObject xpBar = MainMenuUiFactory.CreateUiObject("XpBar", info.transform);
        RectTransform xpRect = xpBar.GetComponent<RectTransform>();
        xpRect.anchorMin = new Vector2(0f, 0f);
        xpRect.anchorMax = new Vector2(1f, 0f);
        xpRect.pivot = new Vector2(0f, 0f);
        xpRect.anchoredPosition = new Vector2(0f, 4f);
        xpRect.sizeDelta = new Vector2(0f, 28f);
        Image xpBg = xpBar.AddComponent<Image>();
        xpBg.color = new Color(0.05f, 0.05f, 0.08f, 1f);
        xpBg.raycastTarget = false;

        GameObject xpFillGo = MainMenuUiFactory.CreateUiObject("Fill", xpBar.transform);
        MainMenuUiFactory.StretchFullScreen(xpFillGo);
        _xpFill = xpFillGo.AddComponent<Image>();
        _xpFill.color = _style.AccentBlue;
        _xpFill.type = Image.Type.Filled;
        _xpFill.fillMethod = Image.FillMethod.Horizontal;
        _xpFill.raycastTarget = false;

        _xpLabel = MainMenuUiFactory.CreateText("XpText", xpBar.transform, _style, "0 / 100", 16, FontStyles.Normal);
        MainMenuUiFactory.StretchFullScreen(_xpLabel.gameObject);
        _xpLabel.alignment = TextAlignmentOptions.Center;
        _xpLabel.raycastTarget = false;
    }

    void BuildTopRightBar(Transform parent)
    {
        GameObject bar = MainMenuUiFactory.CreateUiObject("TopRightBar", parent);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(1f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(1f, 1f);
        barRect.anchoredPosition = new Vector2(-24f, -24f);
        barRect.sizeDelta = new Vector2(420f, 64f);

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        GameObject goldPanel = MainMenuUiFactory.CreatePanel("Gold", bar.transform, _style,
            Vector2.zero, Vector2.one, Vector2.zero, new Vector2(200f, 56f));
        LayoutElement goldLayout = goldPanel.AddComponent<LayoutElement>();
        goldLayout.preferredWidth = 200f;
        goldLayout.preferredHeight = 56f;

        MainMenuUiFactory.CreateText("CoinIcon", goldPanel.transform, _style, "🪙", 26, FontStyles.Normal);

        _goldLabel = MainMenuUiFactory.CreateText("GoldAmount", goldPanel.transform, _style, "0", 24, FontStyles.Bold, _style.GoldTextColor);
        RectTransform goldRect = _goldLabel.rectTransform;
        goldRect.anchorMin = Vector2.zero;
        goldRect.anchorMax = Vector2.one;
        goldRect.offsetMin = new Vector2(52f, 0f);
        goldRect.offsetMax = new Vector2(-40f, 0f);
        _goldLabel.alignment = TextAlignmentOptions.MidlineLeft;

        Button goldPlus = MainMenuUiFactory.CreateIconButton(goldPanel.transform, _style, "+", 32f);
        goldPlus.gameObject.name = "GoldPlusButton";
        RectTransform plusRect = goldPlus.GetComponent<RectTransform>();
        plusRect.anchorMin = new Vector2(1f, 0.5f);
        plusRect.anchorMax = new Vector2(1f, 0.5f);
        plusRect.pivot = new Vector2(1f, 0.5f);
        plusRect.anchoredPosition = new Vector2(-8f, 0f);
        goldPlus.onClick.AddListener(OpenGoldShop);

        Button settingsButton = MainMenuUiFactory.CreateIconButton(bar.transform, _style, "⚙", 56f);
        settingsButton.gameObject.name = "SettingsButton";
        settingsButton.onClick.AddListener(OpenSettings);
    }

    void BuildDailyTasksPanel(Transform parent)
    {
        GameObject panel = MainMenuUiFactory.CreatePanel("DailyTasks", parent, _style,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(380f, 420f));

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        _dailyTitle = MainMenuUiFactory.CreateText("DailyTitle", panel.transform, _style,
            "ЕЖЕДНЕВНЫЕ ЗАДАНИЯ", 22, FontStyles.Bold, _style.GoldTextColor);
        _dailyTitle.alignment = TextAlignmentOptions.Center;
        MainMenuUiFactory.AddLayoutElement(_dailyTitle.gameObject, 34f);

        _taskTitleLabels.Clear();
        _taskProgressLabels.Clear();
        _taskProgressFills.Clear();

        for (int i = 0; i < _dailyTasks.Length; i++)
        {
            int captured = i;
            GameObject row = MainMenuUiFactory.CreateUiObject($"Task_{i}", panel.transform);
            MainMenuUiFactory.AddLayoutElement(row, 88f);
            Image rowBg = row.AddComponent<Image>();
            rowBg.color = new Color(0f, 0f, 0f, 0.25f);

            Button rowButton = row.AddComponent<Button>();
            rowButton.targetGraphic = rowBg;
            rowButton.onClick.AddListener(() => OpenDailyTask(captured));

            TMP_Text title = MainMenuUiFactory.CreateText("Title", row.transform, _style, string.Empty, 17, FontStyles.Normal);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(10f, -8f);
            titleRect.sizeDelta = new Vector2(-20f, 28f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.raycastTarget = false;
            _taskTitleLabels.Add(title);

            GameObject progressBar = MainMenuUiFactory.CreateUiObject("Progress", row.transform);
            RectTransform progRect = progressBar.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0f, 0f);
            progRect.anchorMax = new Vector2(1f, 0f);
            progRect.pivot = new Vector2(0.5f, 0f);
            progRect.anchoredPosition = new Vector2(0f, 10f);
            progRect.sizeDelta = new Vector2(-20f, 18f);
            Image progBg = progressBar.AddComponent<Image>();
            progBg.color = new Color(0.04f, 0.04f, 0.06f, 1f);
            progBg.raycastTarget = false;

            GameObject fillGo = MainMenuUiFactory.CreateUiObject("Fill", progressBar.transform);
            MainMenuUiFactory.StretchFullScreen(fillGo);
            Image fill = fillGo.AddComponent<Image>();
            fill.color = _style.AccentBlue;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.raycastTarget = false;
            _taskProgressFills.Add(fill);

            TMP_Text progressText = MainMenuUiFactory.CreateText("ProgressText", progressBar.transform, _style, "0 / 0", 14, FontStyles.Normal);
            MainMenuUiFactory.StretchFullScreen(progressText.gameObject);
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.raycastTarget = false;
            _taskProgressLabels.Add(progressText);

            TMP_Text reward = MainMenuUiFactory.CreateText("Reward", row.transform, _style,
                $"XP {_dailyTasks[i].RewardXp}", 14, FontStyles.Bold, _style.GoldTextColor);
            RectTransform rewardRect = reward.rectTransform;
            rewardRect.anchorMin = new Vector2(1f, 1f);
            rewardRect.anchorMax = new Vector2(1f, 1f);
            rewardRect.pivot = new Vector2(1f, 1f);
            rewardRect.anchoredPosition = new Vector2(-10f, -8f);
            rewardRect.sizeDelta = new Vector2(80f, 24f);
            reward.alignment = TextAlignmentOptions.MidlineRight;
            reward.raycastTarget = false;
        }

        _dailyTimerLabel = MainMenuUiFactory.CreateText("Timer", panel.transform, _style, string.Empty, 15, FontStyles.Italic,
            new Color(0.75f, 0.75f, 0.75f, 1f));
        _dailyTimerLabel.alignment = TextAlignmentOptions.Center;
        MainMenuUiFactory.AddLayoutElement(_dailyTimerLabel.gameObject, 28f);
    }

    void BuildBottomNavigation(Transform parent)
    {
        GameObject row = MainMenuUiFactory.CreateUiObject("BottomNav", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.04f, 0f);
        rowRect.anchorMax = new Vector2(0.96f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 28f);
        rowRect.sizeDelta = new Vector2(0f, 220f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        Button playBtn = CreateNavBlock(row.transform, playBlockColor, out _playNavLabel);
        playBtn.onClick.AddListener(StartGame);

        Button shopBtn = CreateNavBlock(row.transform, shopBlockColor, out _shopNavLabel);
        shopBtn.onClick.AddListener(OpenShop);

        Button charsBtn = CreateNavBlock(row.transform, charactersBlockColor, out _charsNavLabel);
        charsBtn.onClick.AddListener(OpenCharacters);
    }

    Button CreateNavBlock(Transform parent, Color bgColor, out TMP_Text titleLabel)
    {
        GameObject go = MainMenuUiFactory.CreateUiObject("NavBlock", parent);
        LayoutElement element = go.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.minHeight = 200f;

        Image bg = go.AddComponent<Image>();
        bg.color = bgColor;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = _style.PanelBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = bg;

        titleLabel = MainMenuUiFactory.CreateText("Title", go.transform, _style, string.Empty, 38, FontStyles.Bold, _style.GoldTextColor);
        MainMenuUiFactory.StretchFullScreen(titleLabel.gameObject);
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.raycastTarget = false;

        return button;
    }

    void BuildSettingsOverlay()
    {
        _settingsOverlay = MainMenuUiFactory.CreateUiObject("SettingsOverlay", targetCanvas.transform);
        MainMenuUiFactory.StretchFullScreen(_settingsOverlay);

        GameObject backdrop = MainMenuUiFactory.CreateUiObject("Backdrop", _settingsOverlay.transform);
        MainMenuUiFactory.StretchFullScreen(backdrop);
        Image dim = backdrop.AddComponent<Image>();
        dim.color = _style.OverlayDim;
        Button backdropButton = backdrop.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.targetGraphic = dim;
        backdropButton.onClick.AddListener(CloseAllOverlays);

        GameObject panel = MainMenuUiFactory.CreatePanel("SettingsPanel", _settingsOverlay.transform, _style,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 420f));

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        _settingsTitle = MainMenuUiFactory.CreateText("Title", panel.transform, _style, "НАСТРОЙКИ", 32, FontStyles.Bold, _style.GoldTextColor);
        _settingsTitle.alignment = TextAlignmentOptions.Center;
        MainMenuUiFactory.AddLayoutElement(_settingsTitle.gameObject, 44f);

        GameObject closeGo = MainMenuUiFactory.CreateUiObject("CloseButton", panel.transform);
        MainMenuUiFactory.AddLayoutElement(closeGo, 48f);
        Image closeImage = closeGo.AddComponent<Image>();
        closeImage.color = _style.ButtonColor;
        Button closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImage;
        TMP_Text closeLabel = MainMenuUiFactory.CreateText("Label", closeGo.transform, _style,
            MenuLocalization.Get("Закрыть", "Close"), 20, FontStyles.Bold);
        MainMenuUiFactory.StretchFullScreen(closeLabel.gameObject);
        closeLabel.raycastTarget = false;
        closeBtn.onClick.AddListener(CloseAllOverlays);

        _languageLabel = MainMenuUiFactory.CreateText("LangLabel", panel.transform, _style, "Язык", 22, FontStyles.Normal);
        MainMenuUiFactory.AddLayoutElement(_languageLabel.gameObject, 30f);

        GameObject langRow = MainMenuUiFactory.CreateUiObject("LangRow", panel.transform);
        MainMenuUiFactory.AddLayoutElement(langRow, 52f);
        HorizontalLayoutGroup langLayout = langRow.AddComponent<HorizontalLayoutGroup>();
        langLayout.spacing = 12f;
        langLayout.childAlignment = TextAnchor.MiddleCenter;
        langLayout.childControlWidth = true;
        langLayout.childForceExpandWidth = true;

        _langRuButton = MainMenuUiFactory.CreateButton(langRow.transform, _style, "Русский", _style.PanelColor, 48f);
        _langRuButton.onClick.AddListener(() => SetLanguage(MenuLanguage.Russian));

        _langEnButton = MainMenuUiFactory.CreateButton(langRow.transform, _style, "English", _style.PanelColor, 48f);
        _langEnButton.onClick.AddListener(() => SetLanguage(MenuLanguage.English));

        _soundLabel = MainMenuUiFactory.CreateText("SoundLabel", panel.transform, _style, "Звук", 22, FontStyles.Normal);
        MainMenuUiFactory.AddLayoutElement(_soundLabel.gameObject, 30f);

        GameObject soundRow = MainMenuUiFactory.CreateUiObject("SoundRow", panel.transform);
        MainMenuUiFactory.AddLayoutElement(soundRow, 48f);
        _soundToggle = CreateSoundToggle(soundRow.transform);
        _soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);

        _volumeLabel = MainMenuUiFactory.CreateText("VolumeLabel", panel.transform, _style, "Громкость", 22, FontStyles.Normal);
        MainMenuUiFactory.AddLayoutElement(_volumeLabel.gameObject, 30f);

        GameObject sliderRow = MainMenuUiFactory.CreateUiObject("VolumeSliderRow", panel.transform);
        MainMenuUiFactory.AddLayoutElement(sliderRow, 40f);
        _volumeSlider = CreateVolumeSlider(sliderRow.transform);
        _volumeSlider.minValue = MenuAudioSettings.MinVolume;
        _volumeSlider.maxValue = MenuAudioSettings.MaxVolume;
        _volumeSlider.value = MenuAudioSettings.Volume;
        _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        Button panelBlocker = panel.AddComponent<Button>();
        panelBlocker.transition = Selectable.Transition.None;
        panelBlocker.onClick.AddListener(() => { });

        _settingsOverlay.SetActive(false);
    }

    Toggle CreateSoundToggle(Transform parent)
    {
        GameObject go = MainMenuUiFactory.CreateUiObject("SoundToggle", parent);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.preferredHeight = 40f;

        Toggle toggle = go.AddComponent<Toggle>();
        GameObject bg = MainMenuUiFactory.CreateUiObject("Background", go.transform);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.pivot = new Vector2(0f, 0.5f);
        bgRect.sizeDelta = new Vector2(36f, 36f);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.18f, 1f);
        toggle.targetGraphic = bgImage;

        GameObject check = MainMenuUiFactory.CreateUiObject("Checkmark", bg.transform);
        MainMenuUiFactory.StretchFullScreen(check);
        Image checkImage = check.AddComponent<Image>();
        checkImage.color = _style.AccentBlue;
        toggle.graphic = checkImage;

        MainMenuUiFactory.CreateText("Label", go.transform, _style,
            MenuLocalization.Get("Включить звук", "Sound on"), 20, FontStyles.Normal);
        return toggle;
    }

    Slider CreateVolumeSlider(Transform parent)
    {
        GameObject go = MainMenuUiFactory.CreateUiObject("VolumeSlider", parent);
        MainMenuUiFactory.StretchFullScreen(go);
        Slider slider = go.AddComponent<Slider>();

        GameObject bg = MainMenuUiFactory.CreateUiObject("Background", go.transform);
        MainMenuUiFactory.StretchFullScreen(bg);
        bg.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);

        GameObject fillArea = MainMenuUiFactory.CreateUiObject("Fill Area", go.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = MainMenuUiFactory.CreateUiObject("Fill", fillArea.transform);
        MainMenuUiFactory.StretchFullScreen(fill);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = _style.AccentBlue;

        GameObject handle = MainMenuUiFactory.CreateUiObject("Handle", go.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 22f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        return slider;
    }
}
