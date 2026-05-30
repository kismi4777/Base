using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class MainMenuUI
{
    static readonly string[] GeneratedRootNames =
    {
        "MainMenuHUD",
        "ShopOverlay",
        "GoldShopOverlay",
        "CharactersOverlay",
        "ProfileOverlay",
        "DailyTaskOverlay",
        "SettingsOverlay",
        "LanguageOverlay",
        "Toast"
    };

    [Header("Редактирование в сцене")]
    [SerializeField] bool useSceneUi = true;

    /// <summary>Собрать UI как объекты сцены (видно в Editor без Play).</summary>
    public void BuildUiInScene()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

        ClearSceneUi();
        FixCanvasForInput();
        EnsureSkinCatalog();
        InitDailyTaskDefinitions();

        _style = new MainMenuUiStyle { Font = fontAsset ?? MainMenuUiFantasyAssets.LoadFont() };
        GetOrCreateToast();

        BuildHub();
        BuildPanels();
        BuildSettingsOverlay();
        BuildLanguageOverlay();
        CloseAllOverlays();
        SaveUiReferences();
        WireUiListeners(GetComponent<MainMenuUiReferences>());
        RefreshHub();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    /// <summary>Удалить сгенерированный UI из сцены.</summary>
    public void ClearSceneUi()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

        if (targetCanvas == null)
            return;

        for (int i = 0; i < GeneratedRootNames.Length; i++)
        {
            Transform child = targetCanvas.transform.Find(GeneratedRootNames[i]);
            if (child == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
            else
#endif
                Destroy(child.gameObject);
        }

        _hubRoot = null;
        _settingsOverlay = null;
        _languageOverlay = null;
        _shopPanel = null;
        _goldShopPanel = null;
        _charactersPanel = null;
        _profilePanel = null;
        _dailyTaskPanel = null;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    bool TryLoadSceneUi()
    {
        MainMenuUiReferences refs = GetComponent<MainMenuUiReferences>();
        if (refs == null || refs.HubRoot == null)
            return false;

        ApplyReferences(refs);
        InitPanelsFromScene(refs);
        WireUiListeners(refs);
        return true;
    }

    void ApplyReferences(MainMenuUiReferences refs)
    {
        _hubRoot = refs.HubRoot;
        _settingsOverlay = refs.SettingsOverlay;
        _languageOverlay = refs.LanguageOverlay;
        _playerNameLabel = refs.PlayerNameLabel;
        _levelBadgeLabel = refs.LevelBadgeLabel;
        _xpLabel = refs.XpLabel;
        _xpFill = refs.XpFill;
        _goldLabel = refs.GoldLabel;
        _dailyTitle = refs.DailyTitle;
        _dailyTimerLabel = refs.DailyTimerLabel;
        _taskTitleLabels.Clear();
        _taskTitleLabels.AddRange(refs.TaskTitleLabels);
        _taskProgressLabels.Clear();
        _taskProgressLabels.AddRange(refs.TaskProgressLabels);
        _taskProgressFills.Clear();
        _taskProgressFills.AddRange(refs.TaskProgressFills);
        _settingsTitle = refs.SettingsTitle;
        _soundFxSlider = refs.SoundFxSlider;
        _musicSlider = refs.MusicSlider;
        _languageOpenButton = refs.LanguageOpenButton;
        _langRuButton = refs.LangRuButton;
        _langEnButton = refs.LangEnButton;
        _langTrButton = refs.LangTrButton;
        _languageCloseButton = refs.LanguageCloseButton;

        if (_settingsOverlay != null && refs.LanguageOpenButton != null)
        {
            Transform settings = MainMenuUiFantasyAssets.FindDeepChild(_settingsOverlay.transform, "Settings");
            Transform buttons = settings != null ? settings.Find("Buttons") : null;
            if (buttons != null && buttons.childCount > 0)
            {
                TMP_Text[] labels = buttons.GetChild(0).GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i].gameObject.name == "Text")
                    {
                        _languagePreviewLabel = labels[i];
                        break;
                    }
                }
            }
        }

        _playNavLabel = refs.PlayNavLabel;
        _shopNavLabel = refs.ShopNavLabel;
        _charsNavLabel = refs.CharsNavLabel;
    }

    void InitPanelsFromScene(MainMenuUiReferences refs)
    {
        GetOrCreateToast();

        _shopPanel = new MainMenuShopPanel(targetCanvas, _style, isGoldShop: false, _toast, OnOverlayClosed);
        _goldShopPanel = new MainMenuShopPanel(targetCanvas, _style, isGoldShop: true, _toast, OnOverlayClosed);
        _charactersPanel = new MainMenuCharactersPanel(targetCanvas, _style, skinCatalog, _toast, OnOverlayClosed);
        _profilePanel = new MainMenuProfilePanel(targetCanvas, _style, _toast, OnOverlayClosed);
        _dailyTaskPanel = new MainMenuDailyTaskPanel(targetCanvas, _style, _dailyTasks, _toast, RefreshHub, OnOverlayClosed);

        _shopPanel.TryBindExtended(targetCanvas.transform);
        _goldShopPanel.TryBindExtended(targetCanvas.transform);
        _charactersPanel.TryBindExtended(targetCanvas.transform);
        _profilePanel.TryBindExtended(targetCanvas.transform);
        _dailyTaskPanel.TryBindExtended(targetCanvas.transform);
    }

    void WireUiListeners(MainMenuUiReferences refs)
    {
        WireButton(refs.GoldPlusButton, OpenGoldShop);
        WireButton(refs.SettingsButton, OpenSettings);
        WireButton(refs.PlayNavButton, StartGame);
        WireButton(refs.ShopNavButton, OpenShop);
        WireButton(refs.CharsNavButton, OpenCharacters);

        for (int i = 0; i < refs.DailyTaskButtons.Count; i++)
        {
            int captured = i;
            WireButton(refs.DailyTaskButtons[captured], () => OpenDailyTask(captured));
        }

        if (_soundFxSlider != null)
        {
            _soundFxSlider.onValueChanged.RemoveListener(OnSoundFxChanged);
            _soundFxSlider.onValueChanged.AddListener(OnSoundFxChanged);
        }

        if (_musicSlider != null)
        {
            _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        WireButton(_languageOpenButton, OpenLanguageOverlay);
        WireButton(_languageCloseButton, CloseLanguageOverlay);
        WireButton(_langRuButton, () => SetLanguage(MenuLanguage.Russian));
        WireButton(_langEnButton, () => SetLanguage(MenuLanguage.English));
        WireButton(_langTrButton, () => SetLanguage(MenuLanguage.Turkish));

        if (_settingsOverlay != null)
        {
            Button closeButton = MainMenuUiFantasyAssets.FindButton(_settingsOverlay.transform, "Button_Back");
            WireButton(closeButton, CloseAllOverlays);
        }
    }

    static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    MainMenuToast GetOrCreateToast()
    {
        _toast = GetComponent<MainMenuToast>();
        if (_toast == null)
            _toast = gameObject.AddComponent<MainMenuToast>();

        _toast.Initialize(targetCanvas, _style);
        return _toast;
    }

    void SaveUiReferences()
    {
        MainMenuUiReferences refs = GetComponent<MainMenuUiReferences>();
        if (refs == null)
            refs = gameObject.AddComponent<MainMenuUiReferences>();

        refs.HubRoot = _hubRoot;
        refs.SettingsOverlay = _settingsOverlay;
        refs.LanguageOverlay = _languageOverlay;

        refs.PlayerNameLabel = _playerNameLabel;
        refs.LevelBadgeLabel = _levelBadgeLabel;
        refs.XpLabel = _xpLabel;
        refs.XpFill = _xpFill;
        refs.GoldLabel = _goldLabel;

        refs.DailyTitle = _dailyTitle;
        refs.DailyTimerLabel = _dailyTimerLabel;
        refs.TaskTitleLabels = new List<TMP_Text>(_taskTitleLabels);
        refs.TaskProgressLabels = new List<TMP_Text>(_taskProgressLabels);
        refs.TaskProgressFills = new List<Image>(_taskProgressFills);

        refs.SettingsTitle = _settingsTitle;
        refs.SoundFxSlider = _soundFxSlider;
        refs.MusicSlider = _musicSlider;
        refs.LanguageOpenButton = _languageOpenButton;
        refs.LangRuButton = _langRuButton;
        refs.LangEnButton = _langEnButton;
        refs.LangTrButton = _langTrButton;
        refs.LanguageCloseButton = _languageCloseButton;

        if (refs.LangRuButton == null || refs.LangEnButton == null || refs.LangTrButton == null)
        {
            Transform languageRoot = _languageOverlay != null
                ? MainMenuUiFantasyAssets.FindDeepChild(_languageOverlay.transform, "LanguageFlag_List")
                : null;
            if (languageRoot != null)
            {
                if (refs.LangRuButton == null)
                    refs.LangRuButton = MainMenuUiFantasyAssets.FindButton(languageRoot, "LangRu");
                if (refs.LangEnButton == null)
                    refs.LangEnButton = MainMenuUiFantasyAssets.FindButton(languageRoot, "LangEn");
                if (refs.LangTrButton == null)
                    refs.LangTrButton = MainMenuUiFantasyAssets.FindButton(languageRoot, "LangTr");
            }
        }

        if (refs.LanguageOpenButton == null && _settingsOverlay != null)
        {
            Transform settings = MainMenuUiFantasyAssets.FindDeepChild(_settingsOverlay.transform, "Settings");
            Transform buttons = settings != null ? settings.Find("Buttons") : null;
            if (buttons != null && buttons.childCount > 0)
                refs.LanguageOpenButton = buttons.GetChild(0).GetComponentInChildren<Button>(true);
        }

        refs.PlayNavLabel = _playNavLabel;
        refs.ShopNavLabel = _shopNavLabel;
        refs.CharsNavLabel = _charsNavLabel;

        MainMenuUiFantasyAssets.HomeBindings fantasy = MainMenuUiFantasyAssets.BindHomeHub(_hubRoot.transform);
        refs.GoldPlusButton = fantasy.GoldPlusButton;
        refs.SettingsButton = fantasy.SettingsButton;
        refs.PlayNavButton = fantasy.PlayButton;
        refs.ShopNavButton = fantasy.ShopButton;
        refs.CharsNavButton = fantasy.CharactersButton;

        refs.DailyTaskButtons.Clear();
        Transform dailyPanel = MainMenuUiFantasyAssets.FindDeepChild(_hubRoot.transform, "DailyTasksPanel");
        Transform taskContent = dailyPanel != null
            ? MainMenuUiFantasyAssets.FindDeepChild(dailyPanel, "Content")
            : null;
        if (taskContent != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform row = taskContent.Find($"Task_{i}");
                if (row != null)
                    refs.DailyTaskButtons.Add(row.GetComponent<Button>());
            }
        }

        refs.ShopOverlay = targetCanvas.transform.Find("ShopOverlay")?.gameObject;
        refs.GoldShopOverlay = targetCanvas.transform.Find("GoldShopOverlay")?.gameObject;
        refs.CharactersOverlay = targetCanvas.transform.Find("CharactersOverlay")?.gameObject;
        refs.ProfileOverlay = targetCanvas.transform.Find("ProfileOverlay")?.gameObject;
        refs.DailyTaskOverlay = targetCanvas.transform.Find("DailyTaskOverlay")?.gameObject;
        refs.ToastRoot = targetCanvas.transform.Find("Toast")?.gameObject;
    }
}
