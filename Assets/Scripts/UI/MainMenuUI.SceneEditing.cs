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

        _style = new MainMenuUiStyle { Font = fontAsset };
        GetOrCreateToast();

        BuildHub();
        BuildPanels();
        BuildSettingsOverlay();
        CloseAllOverlays();
        SaveUiReferences();
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
        _languageLabel = refs.LanguageLabel;
        _soundLabel = refs.SoundLabel;
        _volumeLabel = refs.VolumeLabel;
        _soundToggle = refs.SoundToggle;
        _volumeSlider = refs.VolumeSlider;
        _langRuButton = refs.LangRuButton;
        _langEnButton = refs.LangEnButton;
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
        WireButton(refs.ProfileOpenButton, OpenProfile);
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

        if (_soundToggle != null)
        {
            _soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
            _soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }

        if (_volumeSlider != null)
        {
            _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        WireButton(_langRuButton, () => SetLanguage(MenuLanguage.Russian));
        WireButton(_langEnButton, () => SetLanguage(MenuLanguage.English));

        if (_settingsOverlay != null)
        {
            Transform backdrop = _settingsOverlay.transform.Find("Backdrop");
            if (backdrop != null)
                WireButton(backdrop.GetComponent<Button>(), CloseAllOverlays);

            Button closeButton = _settingsOverlay.transform.Find("Panel/CloseButton")?.GetComponent<Button>();
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
        refs.LanguageLabel = _languageLabel;
        refs.SoundLabel = _soundLabel;
        refs.VolumeLabel = _volumeLabel;
        refs.SoundToggle = _soundToggle;
        refs.VolumeSlider = _volumeSlider;
        refs.LangRuButton = _langRuButton;
        refs.LangEnButton = _langEnButton;

        refs.PlayNavLabel = _playNavLabel;
        refs.ShopNavLabel = _shopNavLabel;
        refs.CharsNavLabel = _charsNavLabel;

        refs.ProfileOpenButton = _hubRoot?.transform.Find("ProfilePanel")?.GetComponent<Button>();
        refs.GoldPlusButton = _hubRoot?.transform.Find("TopRightBar/Gold/GoldPlusButton")?.GetComponent<Button>();
        refs.SettingsButton = _hubRoot?.transform.Find("TopRightBar/SettingsButton")?.GetComponent<Button>();

        refs.PlayNavButton = FindNavButton(0);
        refs.ShopNavButton = FindNavButton(1);
        refs.CharsNavButton = FindNavButton(2);

        refs.DailyTaskButtons.Clear();
        if (_hubRoot != null)
        {
            Transform daily = _hubRoot.transform.Find("DailyTasks");
            if (daily != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Transform row = daily.Find($"Task_{i}");
                    if (row != null)
                        refs.DailyTaskButtons.Add(row.GetComponent<Button>());
                }
            }
        }

        refs.ShopOverlay = targetCanvas.transform.Find("ShopOverlay")?.gameObject;
        refs.GoldShopOverlay = targetCanvas.transform.Find("GoldShopOverlay")?.gameObject;
        refs.CharactersOverlay = targetCanvas.transform.Find("CharactersOverlay")?.gameObject;
        refs.ProfileOverlay = targetCanvas.transform.Find("ProfileOverlay")?.gameObject;
        refs.DailyTaskOverlay = targetCanvas.transform.Find("DailyTaskOverlay")?.gameObject;
        refs.ToastRoot = targetCanvas.transform.Find("Toast")?.gameObject;
    }

    Button FindNavButton(int index)
    {
        Transform bottom = _hubRoot?.transform.Find("BottomNav");
        if (bottom == null || index >= bottom.childCount)
            return null;

        return bottom.GetChild(index).GetComponent<Button>();
    }

    static Button FindButtonInChildren(Transform root, string pathPrefix, string childName)
    {
        if (root == null)
            return null;

        Transform parent = root.Find(pathPrefix);
        if (parent == null)
            return null;

        Transform button = parent.Find(childName);
        return button != null ? button.GetComponent<Button>() : parent.GetComponentInChildren<Button>();
    }
}
