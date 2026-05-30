using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Пути и утилиты для UI-пака GUI Pro-FantasyRPG (Layer Lab).</summary>
public static class MainMenuUiFantasyAssets
{
    const string PackRoot = "Assets/Layer Lab/GUI Pro-FantasyRPG";

    public static readonly string FontPath = $"{PackRoot}/ResourcesData/Fonts/Alata-Regular SDF.asset";
    public static readonly string HomeBackgroundPath = $"{PackRoot}/ResourcesData/Sprites/Demo/Demo_Background/BackGround_Sample_03_Home_1920.png";

    public static readonly string HomePanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/Home.prefab";
    public static readonly string ShopPanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/Shop_Chest.prefab";
    public static readonly string GoldShopPanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/Shop_Gold.prefab";
    public static readonly string CharactersPanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/CharacterSelect.prefab";
    public static readonly string ProfilePanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/Character.prefab";
    public static readonly string DailyTaskPanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/Mission.prefab";
    public static readonly string SettingsPanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/Settings.prefab";
    public static readonly string ToastFramePath = $"{PackRoot}/Prefabs/Prefabs_Component_Frames/BasicFrame_Square_m_Dark.prefab";
    public static readonly string DailyTasksOuterFramePath =
        $"{PackRoot}/Prefabs/Prefabs_Component_Frames/PanelFrame_01_Bg_Dark.prefab";
    public static readonly string DailyTasksPanelFramePath =
        $"{PackRoot}/Prefabs/Prefabs_Component_Frames/PanelFrame_02_Gray.prefab";
    public static readonly string DailyTasksListFramePath =
        $"{PackRoot}/Prefabs/Prefabs_Component_Frames/ListFrame_02_VerticalLayout_Demo 1.prefab";
    public static readonly string LanguagePanelPath = $"{PackRoot}/Prefabs/Prefabs_DemoScene_Panels/Language.prefab";
    public static readonly string SettingsLanguageRowFramePath =
        $"{PackRoot}/Prefabs/Prefabs_Component_Frames/LineTextFrame_05_White.prefab";
    const string DailyTaskRowTemplateName = "ListFrame_02_Demo";
    const string MissionListRowTemplateName = "MissionList_Default";
    const string RusLanguageFlagSpriteGuid = "8ff0221bfa7af412689476c7cde3cf15";
    const string EngLanguageFlagSpriteGuid = "6157da686ab5a453fa43773496521c8d";
    const string TurLanguageFlagSpriteGuid = "9d5db93b932984fae867e9c4fa79ed6e";

    public struct HomeBindings
    {
        public Transform HomeRoot;
        public TMP_Text PlayerNameLabel;
        public TMP_Text LevelBadgeLabel;
        public TMP_Text XpLabel;
        public Image XpFill;
        public TMP_Text GoldLabel;
        public Button GoldPlusButton;
        public Button SettingsButton;
        public Button PlayButton;
        public TMP_Text PlayLabel;
        public Button ShopButton;
        public TMP_Text ShopLabel;
        public Button CharactersButton;
        public TMP_Text CharactersLabel;
    }

    public struct SettingsBindings
    {
        public GameObject Root;
        public TMP_Text TitleLabel;
        public Toggle SoundToggle;
        public Slider VolumeSlider;
        public Button LangRuButton;
        public Button LangEnButton;
        public Button CloseButton;
    }

    public static TMP_FontAsset LoadFont()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
#else
        return null;
#endif
    }

    public static GameObject InstantiateLanguageFlag(Transform parent, MenuLanguage language, string rename)
    {
#if UNITY_EDITOR
        string spriteGuid = GetLanguageFlagSpriteGuid(language);

        GameObject languageContents = UnityEditor.PrefabUtility.LoadPrefabContents(LanguagePanelPath);
        if (languageContents == null)
            return null;

        Transform template = FindLanguageFlagBySprite(languageContents.transform, spriteGuid);
        if (template == null)
        {
            UnityEditor.PrefabUtility.UnloadPrefabContents(languageContents);
            Debug.LogError($"MainMenuUiFantasyAssets: флаг языка не найден — {language}");
            return null;
        }

        GameObject flag = UnityEngine.Object.Instantiate(template.gameObject, parent);
        if (!string.IsNullOrEmpty(rename))
            flag.name = rename;

        UnityEditor.PrefabUtility.UnloadPrefabContents(languageContents);
        return flag;
#else
        return null;
#endif
    }

    public static string GetLanguageFlagSpriteGuid(MenuLanguage language) => language switch
    {
        MenuLanguage.English => EngLanguageFlagSpriteGuid,
        MenuLanguage.Turkish => TurLanguageFlagSpriteGuid,
        _ => RusLanguageFlagSpriteGuid
    };

    public static GameObject InstantiateDailyTaskListRow(Transform parent, string rename)
    {
#if UNITY_EDITOR
        GameObject listContents = UnityEditor.PrefabUtility.LoadPrefabContents(DailyTasksListFramePath);
        if (listContents == null)
            return null;

        Transform template = FindDeepChild(listContents.transform, DailyTaskRowTemplateName);
        if (template == null)
        {
            UnityEditor.PrefabUtility.UnloadPrefabContents(listContents);
            Debug.LogError($"MainMenuUiFantasyAssets: шаблон строки не найден — {DailyTaskRowTemplateName}");
            return null;
        }

        GameObject row = UnityEngine.Object.Instantiate(template.gameObject, parent);
        if (!string.IsNullOrEmpty(rename))
            row.name = rename;

        UnityEditor.PrefabUtility.UnloadPrefabContents(listContents);
        return row;
#else
        return null;
#endif
    }

    public static GameObject InstantiateMissionListRow(Transform parent, string rename)
    {
#if UNITY_EDITOR
        GameObject missionContents = UnityEditor.PrefabUtility.LoadPrefabContents(DailyTaskPanelPath);
        if (missionContents == null)
            return null;

        Transform template = FindDeepChild(missionContents.transform, MissionListRowTemplateName);
        if (template == null)
        {
            UnityEditor.PrefabUtility.UnloadPrefabContents(missionContents);
            Debug.LogError($"MainMenuUiFantasyAssets: шаблон строки не найден — {MissionListRowTemplateName}");
            return null;
        }

        GameObject row = UnityEngine.Object.Instantiate(template.gameObject, parent);
        if (!string.IsNullOrEmpty(rename))
            row.name = rename;

        UnityEditor.PrefabUtility.UnloadPrefabContents(missionContents);
        return row;
#else
        return null;
#endif
    }

    public static GameObject InstantiatePrefab(string assetPath, Transform parent, string rename = null)
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError($"MainMenuUiFantasyAssets: префаб не найден — {assetPath}");
            return null;
        }

        GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
        if (instance == null)
            return null;

        if (!string.IsNullOrEmpty(rename))
            instance.name = rename;

        return instance;
#else
        return null;
#endif
    }

    public static void StretchFullScreen(GameObject go)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    public static GameObject CreateHomeBackground(Transform parent)
    {
        GameObject bg = MainMenuUiFactory.CreateUiObject("Background", parent);
        StretchFullScreen(bg);

        Image image = bg.AddComponent<Image>();
#if UNITY_EDITOR
        Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(HomeBackgroundPath);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }
#endif
        image.color = Color.white;
        image.raycastTarget = false;
        return bg;
    }

    public static HomeBindings BindHomeHub(Transform hubRoot)
    {
        Transform home = FindDeepChild(hubRoot, "Home");
        HomeBindings bindings = new() { HomeRoot = home };

        if (home == null)
            return bindings;

        Transform profile = home.Find("UserLevel_Info");
        if (profile != null)
        {
            bindings.PlayerNameLabel = MainMenuUiFantasyAssets.FindText(profile, "TextName");
            bindings.LevelBadgeLabel = MainMenuUiFantasyAssets.FindText(profile, "LevelBadge/Text")
                ?? MainMenuUiFantasyAssets.FindText(profile, "LevelBadge");

            Transform xpSlider = MainMenuUiFantasyAssets.FindDeepChild(profile, "Slider_Level_l_Demo")
                ?? MainMenuUiFantasyAssets.FindDeepChild(profile, "Slider");
            if (xpSlider != null)
            {
                bindings.XpLabel = MainMenuUiFantasyAssets.FindText(xpSlider, "Text_Value")
                    ?? MainMenuUiFantasyAssets.FindText(xpSlider, "Text (TMP)");
                Slider slider = xpSlider.GetComponent<Slider>();
                if (slider != null && slider.fillRect != null)
                    bindings.XpFill = slider.fillRect.GetComponent<Image>();
                else
                    bindings.XpFill = MainMenuUiFantasyAssets.FindImage(xpSlider, "Fill");
            }
        }

        Transform coin = FindDeepChild(home, "Coin");
        if (coin != null)
        {
            bindings.GoldLabel = FindText(coin, "Text_Count")
                ?? FindText(coin, "Text (TMP)")
                ?? FindText(coin, "Text");
            bindings.GoldPlusButton = FindButton(coin, "Button_Add");
        }

        bindings.SettingsButton = FindButton(home, "Button_Top_Setting");
        bindings.PlayButton = FindButton(home, "Button_Play");
        bindings.PlayLabel = FindText(home, "Button_Play/Text_Play")
            ?? FindText(home, "Text_Play");

        Transform shop = FindDeepChild(home, "Shop");
        if (shop != null)
        {
            bindings.ShopButton = shop.GetComponent<Button>() ?? shop.GetComponentInChildren<Button>();
            bindings.ShopLabel = shop.GetComponentInChildren<TMP_Text>();
        }

        Transform heroes = FindDeepChild(home, "Heroes");
        if (heroes != null)
        {
            bindings.CharactersButton = heroes.GetComponent<Button>() ?? heroes.GetComponentInChildren<Button>();
            bindings.CharactersLabel = heroes.GetComponentInChildren<TMP_Text>();
        }

        return bindings;
    }

    public static SettingsBindings BindSettingsOverlay(GameObject settingsRoot)
    {
        SettingsBindings bindings = new() { Root = settingsRoot };
        Transform root = settingsRoot.transform;

        bindings.TitleLabel = FindText(root, "Text_Title");
        bindings.CloseButton = FindButton(root, "Button_Back");

        Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
        if (toggles.Length > 0)
            bindings.SoundToggle = toggles[0];

        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        if (sliders.Length > 0)
            bindings.VolumeSlider = sliders[0];

        List<Button> langCandidates = FindButtonsByNameContains(root, "Button_BlueGray");
        if (langCandidates.Count >= 2)
        {
            bindings.LangRuButton = langCandidates[0];
            bindings.LangEnButton = langCandidates[1];
        }

        return bindings;
    }

    public static GameObject BuildFantasyOverlay(string overlayName, string prefabPath, Transform canvasRoot)
    {
        GameObject root = MainMenuUiFactory.CreateUiObject(overlayName, canvasRoot);
        StretchFullScreen(root);

        GameObject panel = InstantiatePrefab(prefabPath, root.transform);
        if (panel != null)
            StretchFullScreen(panel);

        root.SetActive(false);
        return root;
    }

    public static void WireCloseButtons(Transform root, UnityAction onClose)
    {
        WireNamedButtons(root, onClose, "Button_Back", "Button_Home");
    }

    public static void WireNamedButtons(Transform root, UnityAction onClose, params string[] names)
    {
        if (root == null || onClose == null)
            return;

        for (int i = 0; i < names.Length; i++)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int b = 0; b < buttons.Length; b++)
            {
                if (buttons[b].gameObject.name != names[i])
                    continue;

                buttons[b].onClick.RemoveListener(onClose);
                buttons[b].onClick.AddListener(onClose);
            }
        }
    }

    public static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null)
            return null;

        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    public static TMP_Text FindText(Transform parent, string path)
    {
        if (parent == null)
            return null;

        Transform node = parent.Find(path);
        if (node == null)
            node = FindDeepChild(parent, path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path);

        return node != null ? node.GetComponent<TMP_Text>() : null;
    }

    public static Image FindImage(Transform parent, string childName)
    {
        Transform node = FindDeepChild(parent, childName);
        return node != null ? node.GetComponent<Image>() : null;
    }

    public static Button FindButton(Transform parent, string name)
    {
        Transform node = FindDeepChild(parent, name);
        return node != null ? node.GetComponent<Button>() : null;
    }

    static Transform FindLanguageFlagBySprite(Transform root, string spriteGuid)
    {
#if UNITY_EDITOR
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject.name != "LanguageFlag")
                continue;

            Sprite sprite = images[i].sprite;
            if (sprite == null)
                continue;

            string path = UnityEditor.AssetDatabase.GetAssetPath(sprite);
            if (UnityEditor.AssetDatabase.AssetPathToGUID(path) == spriteGuid)
                return images[i].transform;
        }
#endif
        return null;
    }

    static List<Button> FindButtonsByNameContains(Transform root, string namePart)
    {
        List<Button> result = new();
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name.Contains(namePart))
                result.Add(buttons[i]);
        }

        return result;
    }
}
