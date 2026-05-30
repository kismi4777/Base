using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Настройка UI-пакета FantasyRPG под логику главного меню.</summary>
public static class MainMenuUiFantasyConfigurator
{
    static readonly string[] HiddenHomeObjects =
    {
        "SubMenu",
        "Button_BossDungeon",
        "Home_MissionInfo"
    };

    static readonly string[] HiddenSettingsObjects =
    {
        "Button_Connected",
        "Button_Connect",
        "LineDivide",
        "Button_Logout"
    };

    static readonly string[] HiddenSettingsRowLabels =
    {
        "Push",
        "Alram",
        "Alarm",
        "Vibration"
    };

    public struct DailyTasksUi
    {
        public GameObject Root;
        public TMP_Text TitleLabel;
        public TMP_Text TimerLabel;
        public List<TMP_Text> TaskTitleLabels;
        public List<TMP_Text> TaskProgressLabels;
        public List<Image> TaskProgressFills;
        public List<Button> TaskButtons;
    }

    public struct FantasySettingsUi
    {
        public TMP_Text TitleLabel;
        public Button CloseButton;
        public Slider SoundFxSlider;
        public Slider MusicSlider;
        public Button LanguageOpenButton;
        public TMP_Text LanguagePreviewLabel;
    }

    public struct FantasyLanguageUi
    {
        public GameObject Root;
        public Button CloseButton;
        public Button RuButton;
        public Button EnButton;
        public Button TrButton;
    }

    public static void ConfigureHomeHub(Transform hubRoot, MainMenuUiStyle style,
        MainMenuDailyTaskPanel.TaskDefinition[] tasks, Action<int> onTaskClicked)
    {
        Transform home = MainMenuUiFantasyAssets.FindDeepChild(hubRoot, "Home");
        if (home == null)
            return;

        for (int i = 0; i < HiddenHomeObjects.Length; i++)
        {
            Transform node = MainMenuUiFantasyAssets.FindDeepChild(home, HiddenHomeObjects[i]);
            if (node != null)
                node.gameObject.SetActive(false);
        }

        DisableProfileButton(home.Find("UserLevel_Info"));
        ConfigurePlayButton(MainMenuUiFantasyAssets.FindText(home, "Text_Play")
            ?? MainMenuUiFantasyAssets.FindText(home, "Button_Play/Text_Play"));
    }

    public static DailyTasksUi BuildDailyTasksPanel(Transform hubRoot, MainMenuUiStyle style,
        MainMenuDailyTaskPanel.TaskDefinition[] tasks, Action<int> onTaskClicked)
    {
        DailyTasksUi ui = new()
        {
            TaskTitleLabels = new List<TMP_Text>(),
            TaskProgressLabels = new List<TMP_Text>(),
            TaskProgressFills = new List<Image>(),
            TaskButtons = new List<Button>()
        };

        Transform home = MainMenuUiFantasyAssets.FindDeepChild(hubRoot, "Home");
        if (home == null || tasks == null || tasks.Length == 0)
            return ui;

        ui.Root = MainMenuUiFactory.CreateUiObject("DailyTasksPanel", home);
        RectTransform panelRect = ui.Root.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(24f, 0f);
        panelRect.sizeDelta = new Vector2(480f, 520f);

        GameObject outerFrame = MainMenuUiFantasyAssets.InstantiatePrefab(
            MainMenuUiFantasyAssets.DailyTasksOuterFramePath, ui.Root.transform, "PanelFrame_01_Bg_Dark");
        if (outerFrame != null)
        {
            MainMenuUiFantasyAssets.StretchFullScreen(outerFrame);
            TMP_Text outerText = MainMenuUiFantasyAssets.FindText(outerFrame.transform, "Text");
            if (outerText != null && outerText.gameObject.name == "Text")
                outerText.gameObject.SetActive(false);
        }

        Transform outerTransform = outerFrame != null ? outerFrame.transform : ui.Root.transform;

        GameObject frame = MainMenuUiFantasyAssets.InstantiatePrefab(
            MainMenuUiFantasyAssets.DailyTasksPanelFramePath, outerTransform, "PanelFrame_02_Gray");
        if (frame != null)
        {
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(10f, 10f);
            frameRect.offsetMax = new Vector2(-10f, -10f);

            ui.TitleLabel = MainMenuUiFantasyAssets.FindText(frame.transform, "Text_Title");
            if (ui.TitleLabel != null)
            {
                ui.TitleLabel.text = MenuLocalization.Get("ежедневные миссии", "daily missions", "günlük görevler");
                ui.TitleLabel.enableAutoSizing = true;
                ui.TitleLabel.fontSizeMin = 24f;
                ui.TitleLabel.fontSizeMax = 36f;
                ui.TitleLabel.alignment = TextAlignmentOptions.Center;
                ui.TitleLabel.color = new Color(0.72f, 0.88f, 0.96f, 1f);
            }

            ui.TimerLabel = MainMenuUiFantasyAssets.FindText(frame.transform, "Text");
            if (ui.TimerLabel != null && ui.TimerLabel != ui.TitleLabel)
            {
                ui.TimerLabel.fontSize = 18f;
                ui.TimerLabel.alignment = TextAlignmentOptions.Center;
                ui.TimerLabel.color = new Color(0.86f, 0.82f, 0.72f, 1f);
            }
        }

        Transform frameTransform = frame != null ? frame.transform : ui.Root.transform;

        GameObject scrollGo = MainMenuUiFactory.CreateUiObject("TaskScroll", frameTransform);
        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(14f, 14f);
        scrollRect.offsetMax = new Vector2(-14f, -158f);

        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        GameObject viewport = MainMenuUiFactory.CreateUiObject("Viewport", scrollGo.transform);
        MainMenuUiFactory.StretchFullScreen(viewport);
        viewport.AddComponent<RectMask2D>();
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject listFrame = MainMenuUiFantasyAssets.InstantiatePrefab(
            MainMenuUiFantasyAssets.DailyTasksListFramePath, viewport.transform, "ListFrame_02_VerticalLayout");
        RectTransform listRect = listFrame != null ? listFrame.GetComponent<RectTransform>() : null;
        if (listRect != null)
        {
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = new Vector2(0f, 0f);
            listRect.localScale = Vector3.one;

            ContentSizeFitter listFitter = listFrame.GetComponent<ContentSizeFitter>()
                ?? listFrame.AddComponent<ContentSizeFitter>();
            listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup listLayout = listFrame.GetComponent<VerticalLayoutGroup>();
            if (listLayout != null)
            {
                listLayout.spacing = 12f;
                listLayout.childControlWidth = true;
                listLayout.childControlHeight = false;
                listLayout.childForceExpandWidth = true;
            }

            for (int i = listFrame.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = listFrame.transform.GetChild(i);
                if (!child.name.StartsWith("ListFrame_02_Demo", StringComparison.Ordinal))
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                else
#endif
                    UnityEngine.Object.Destroy(child.gameObject);
            }

            scroll.content = listRect;
        }

        Transform listParent = listFrame != null ? listFrame.transform : viewport.transform;

        for (int i = 0; i < tasks.Length; i++)
        {
            GameObject row = MainMenuUiFantasyAssets.InstantiateDailyTaskListRow(listParent, $"Task_{i}");
            if (row == null)
                continue;

            ConfigureFantasyDailyTaskRow(row, style, tasks[i], i, onTaskClicked, ui);
        }

        return ui;
    }

    static void ConfigureFantasyDailyTaskRow(GameObject row, MainMenuUiStyle style,
        MainMenuDailyTaskPanel.TaskDefinition task, int index, Action<int> onTaskClicked, DailyTasksUi ui)
    {
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.localScale = Vector3.one;
        rowRect.localRotation = Quaternion.identity;

        LayoutElement layout = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
        layout.preferredHeight = 148f;
        layout.minHeight = 148f;
        layout.flexibleWidth = 1f;

        Transform focus = row.transform.Find("Focus");
        if (focus != null)
            focus.gameObject.SetActive(false);

        Transform iconFrame = row.transform.Find("BasicFrame_Squard_l");
        if (iconFrame != null)
        {
            RectTransform iconFrameRect = iconFrame.GetComponent<RectTransform>();
            iconFrameRect.anchorMin = new Vector2(0f, 0.5f);
            iconFrameRect.anchorMax = new Vector2(0f, 0.5f);
            iconFrameRect.pivot = new Vector2(0f, 0.5f);
            iconFrameRect.anchoredPosition = new Vector2(8f, 0f);
            iconFrameRect.sizeDelta = new Vector2(96f, 96f);
        }

        TMP_Text title = MainMenuUiFantasyAssets.FindText(row.transform, "Text");
        if (title != null)
        {
            title.text = string.Empty;
            title.fontSize = 26f;
            title.enableWordWrapping = true;
            title.raycastTarget = false;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(112f, 18f);
            titleRect.sizeDelta = new Vector2(-124f, 72f);
            title.alignment = TextAlignmentOptions.TopLeft;
            ui.TaskTitleLabels.Add(title);
        }

        Slider progressSlider = InstantiateMissionProgressSlider(row.transform);
        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            RectTransform sliderRect = progressSlider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 0f);
            sliderRect.pivot = new Vector2(0.5f, 0f);
            sliderRect.anchoredPosition = new Vector2(0f, 10f);
            sliderRect.sizeDelta = new Vector2(-20f, 22f);

            TMP_Text valueLabel = MainMenuUiFantasyAssets.FindText(progressSlider.transform, "Text_Value");
            if (valueLabel != null)
            {
                valueLabel.fontSize = 14f;
                valueLabel.raycastTarget = false;
                ui.TaskProgressLabels.Add(valueLabel);
            }
            else
            {
                TMP_Text progress = MainMenuUiFactory.CreateText("Progress", row.transform, style, string.Empty, 14,
                    FontStyles.Normal, new Color(0.75f, 0.71f, 0.66f, 1f));
                RectTransform progressRect = progress.rectTransform;
                progressRect.anchorMin = new Vector2(1f, 0f);
                progressRect.anchorMax = new Vector2(1f, 0f);
                progressRect.pivot = new Vector2(1f, 0f);
                progressRect.anchoredPosition = new Vector2(-12f, 14f);
                progressRect.sizeDelta = new Vector2(72f, 22f);
                progress.alignment = TextAlignmentOptions.MidlineRight;
                progress.raycastTarget = false;
                ui.TaskProgressLabels.Add(progress);
            }

            if (progressSlider.fillRect != null)
            {
                Image fill = progressSlider.fillRect.GetComponent<Image>();
                if (fill != null)
                    ui.TaskProgressFills.Add(fill);
            }
        }
        else
        {
            TMP_Text progress = MainMenuUiFactory.CreateText("Progress", row.transform, style, string.Empty, 14,
                FontStyles.Normal, new Color(0.75f, 0.71f, 0.66f, 1f));
            RectTransform progressRect = progress.rectTransform;
            progressRect.anchorMin = new Vector2(0f, 0f);
            progressRect.anchorMax = new Vector2(1f, 0f);
            progressRect.pivot = new Vector2(0f, 0f);
            progressRect.anchoredPosition = new Vector2(112f, 12f);
            progressRect.sizeDelta = new Vector2(-124f, 24f);
            progress.alignment = TextAlignmentOptions.BottomLeft;
            progress.raycastTarget = false;
            ui.TaskProgressLabels.Add(progress);
        }

        Image rowBg = row.GetComponent<Image>();
        Button rowButton = row.GetComponent<Button>();
        if (rowButton == null && rowBg != null)
        {
            rowButton = row.AddComponent<Button>();
            rowButton.targetGraphic = rowBg;
            rowButton.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = rowButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            rowButton.colors = colors;
        }

        if (rowButton != null)
        {
            int captured = index;
            rowButton.onClick.AddListener(() => onTaskClicked?.Invoke(captured));
            ui.TaskButtons.Add(rowButton);
        }
    }

    static Slider InstantiateMissionProgressSlider(Transform parent)
    {
#if UNITY_EDITOR
        GameObject missionContents = UnityEditor.PrefabUtility.LoadPrefabContents(MainMenuUiFantasyAssets.DailyTaskPanelPath);
        if (missionContents == null)
            return null;

        Transform template = MainMenuUiFantasyAssets.FindDeepChild(missionContents.transform, "MissionList_Default");
        Slider slider = template != null ? template.GetComponentInChildren<Slider>(true) : null;
        if (slider == null)
        {
            UnityEditor.PrefabUtility.UnloadPrefabContents(missionContents);
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(slider.gameObject, parent);
        instance.name = "ProgressSlider";
        UnityEditor.PrefabUtility.UnloadPrefabContents(missionContents);
        return instance.GetComponent<Slider>();
#else
        return null;
#endif
    }

    public static FantasySettingsUi ConfigureSettingsOverlay(GameObject settingsRoot, MainMenuUiStyle style)
    {
        FantasySettingsUi ui = new();
        Transform settings = MainMenuUiFantasyAssets.FindDeepChild(settingsRoot.transform, "Settings");
        if (settings == null)
            return ui;

        for (int i = 0; i < HiddenSettingsObjects.Length; i++)
        {
            Transform node = settings.Find(HiddenSettingsObjects[i]);
            if (node != null)
                node.gameObject.SetActive(false);
        }

        HideSettingsRowsByLabel(settings, HiddenSettingsRowLabels);
        ConfigureSettingsLanguageButtons(settings);

        ui.TitleLabel = MainMenuUiFantasyAssets.FindText(settings, "Text_Title");
        ui.CloseButton = MainMenuUiFantasyAssets.FindButton(settings, "Button_Back");
        ui.SoundFxSlider = FindSliderByRowLabel(settings, "Sound");
        ui.MusicSlider = FindSliderByRowLabel(settings, "Music");

        if (ui.SoundFxSlider != null)
            ui.SoundFxSlider.interactable = true;

        if (ui.MusicSlider != null)
            ui.MusicSlider.interactable = true;

        Transform buttons = settings.Find("Buttons");
        if (buttons != null && buttons.childCount > 0)
        {
            Transform languageGroup = buttons.GetChild(0);
            ui.LanguageOpenButton = languageGroup.GetComponentInChildren<Button>(true);
            TMP_Text[] labels = languageGroup.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i].gameObject.name == "Text")
                {
                    ui.LanguagePreviewLabel = labels[i];
                    break;
                }
            }
        }

        LocalizeSettingsLabels(settings);
        return ui;
    }

    public static FantasyLanguageUi ConfigureLanguageOverlay(GameObject languageRoot)
    {
        FantasyLanguageUi ui = new() { Root = languageRoot };

        Transform popup = MainMenuUiFantasyAssets.FindDeepChild(languageRoot.transform, "Popup")
            ?? languageRoot.transform;
        ui.CloseButton = MainMenuUiFantasyAssets.FindButton(popup, "Button_Close");

        Transform flagList = MainMenuUiFantasyAssets.FindDeepChild(popup, "LanguageFlag_List");
        if (flagList == null)
            return ui;

        ConfigureLanguageFlagList(flagList, out ui.RuButton, out ui.EnButton, out ui.TrButton);

        TMP_Text title = MainMenuUiFantasyAssets.FindText(popup, "Text_Title");
        if (title != null)
            title.text = MenuLocalization.Get("ЯЗЫК", "LANGUAGE", "DİL");

        return ui;
    }

    static void ConfigureLanguageFlagList(Transform flagList, out Button ruButton, out Button enButton,
        out Button trButton)
    {
        ruButton = null;
        enButton = null;
        trButton = null;

        Image[] images = flagList.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject.name != "LanguageFlag")
                continue;

            Sprite sprite = images[i].sprite;
            MenuLanguage? language = ResolveLanguageFlag(sprite);
            if (language == null)
            {
                images[i].gameObject.SetActive(false);
                continue;
            }

            Button button = ConfigureLanguageFlagButton(images[i].gameObject, 120f, 90f);
            switch (language.Value)
            {
                case MenuLanguage.Russian:
                    images[i].gameObject.name = "LangRu";
                    ruButton = button;
                    break;
                case MenuLanguage.English:
                    images[i].gameObject.name = "LangEn";
                    enButton = button;
                    break;
                case MenuLanguage.Turkish:
                    images[i].gameObject.name = "LangTr";
                    trButton = button;
                    break;
            }
        }

        GridLayoutGroup grid = flagList.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.spacing = new Vector2(32f, 16f);
        }

        ContentSizeFitter fitter = flagList.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    static void ConfigureSettingsLanguageButtons(Transform settings)
    {
        Transform buttons = settings.Find("Buttons");
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.childCount; i++)
        {
            Transform group = buttons.GetChild(i);
            if (group.name != "Group")
                continue;

            group.gameObject.SetActive(i == 0);
        }
    }

    static void HideSettingsRowsByLabel(Transform settings, string[] labels)
    {
        TMP_Text[] texts = settings.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string value = texts[i].text;
            if (string.IsNullOrEmpty(value))
                continue;

            for (int l = 0; l < labels.Length; l++)
            {
                if (!value.Contains(labels[l], StringComparison.OrdinalIgnoreCase))
                    continue;

                HideSettingsRow(texts[i].transform);
                break;
            }
        }
    }

    static void HideSettingsRow(Transform from)
    {
        Transform node = from;
        while (node != null)
        {
            if (node.name == "List" && node.parent != null && node.parent.name == "List")
            {
                node.gameObject.SetActive(false);
                return;
            }

            node = node.parent;
        }
    }

    static Slider FindSliderByRowLabel(Transform settings, string labelPart)
    {
        TMP_Text[] texts = settings.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (!texts[i].text.Contains(labelPart, StringComparison.OrdinalIgnoreCase))
                continue;

            Transform row = texts[i].transform;
            while (row != null)
            {
                Slider slider = row.GetComponentInChildren<Slider>(true);
                if (slider != null)
                    return slider;

                if (row.name == "List" && row.parent != null && row.parent.name == "List")
                    break;

                row = row.parent;
            }
        }

        return null;
    }

    static MenuLanguage? ResolveLanguageFlag(Sprite sprite)
    {
        if (sprite == null)
            return null;

#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(sprite);
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
        if (guid == MainMenuUiFantasyAssets.GetLanguageFlagSpriteGuid(MenuLanguage.Russian))
            return MenuLanguage.Russian;
        if (guid == MainMenuUiFantasyAssets.GetLanguageFlagSpriteGuid(MenuLanguage.English))
            return MenuLanguage.English;
        if (guid == MainMenuUiFantasyAssets.GetLanguageFlagSpriteGuid(MenuLanguage.Turkish))
            return MenuLanguage.Turkish;
        return null;
#else
        string name = sprite.name.ToLowerInvariant();
        if (name.Contains("rus"))
            return MenuLanguage.Russian;
        if (name.Contains("eng"))
            return MenuLanguage.English;
        if (name.Contains("tur"))
            return MenuLanguage.Turkish;
        return null;
#endif
    }

    static void DisableProfileButton(Transform profile)
    {
        if (profile == null)
            return;

        Button button = profile.GetComponent<Button>();
        if (button != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEngine.Object.DestroyImmediate(button);
            else
#endif
                UnityEngine.Object.Destroy(button);
        }

        Graphic[] graphics = profile.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    static void ConfigurePlayButton(TMP_Text label)
    {
        if (label == null)
            return;

        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = 46f;
        label.alignment = TextAlignmentOptions.Center;
        label.horizontalAlignment = HorizontalAlignmentOptions.Center;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 8f);
        rect.offsetMax = new Vector2(-16f, -8f);
    }

    static Button ConfigureLanguageFlagButton(GameObject flag, float width, float height)
    {
        if (flag == null)
            return null;

        RectTransform rect = flag.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        LayoutElement layout = flag.GetComponent<LayoutElement>() ?? flag.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.minWidth = width;
        layout.minHeight = height;

        Image image = flag.GetComponent<Image>();
        Button button = flag.GetComponent<Button>();
        if (button == null && image != null)
        {
            button = flag.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.colors = colors;
        }

        Transform check = flag.transform.Find("Check");
        if (check != null)
            check.gameObject.SetActive(false);

        return button;
    }

    public static void LocalizeSettingsLabels(Transform settingsRoot)
    {
        TMP_Text[] labels = settingsRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string text = labels[i].text;
            if (text.Contains("Push", StringComparison.OrdinalIgnoreCase))
                labels[i].text = MenuLocalization.Get("Push-уведомления", "Push Alarm", "Push Bildirimi");
            else if (text.Contains("Sound", StringComparison.OrdinalIgnoreCase))
                labels[i].text = MenuLocalization.Get("Звук", "Sound Fx", "Ses Efekti");
            else if (text.Equals("Music", StringComparison.OrdinalIgnoreCase))
                labels[i].text = MenuLocalization.Get("Музыка", "Music", "Müzik");
            else if (text.Contains("Vibration", StringComparison.OrdinalIgnoreCase))
                labels[i].text = MenuLocalization.Get("Вибрация", "Vibration", "Titreşim");
            else if (text.Equals("Language", StringComparison.OrdinalIgnoreCase)
                     || text.Equals("English", StringComparison.OrdinalIgnoreCase))
                labels[i].text = MenuLocalization.GetLanguageDisplayName();
        }
    }
}
