using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Pop-up победы и поражения после окончания PvP-матча.</summary>
public sealed class MatchResultPopupUI : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField] Canvas targetCanvas;
    [SerializeField] PvPBattleOrchestrator battleOrchestrator;
    [SerializeField] TMP_FontAsset fontAsset;

    [Header("Оформление")]
    [SerializeField] Color overlayColor = new(0f, 0f, 0f, 0.78f);
    [SerializeField] Color winPanelColor = new(0.1f, 0.22f, 0.14f, 0.97f);
    [SerializeField] Color losePanelColor = new(0.24f, 0.1f, 0.1f, 0.97f);
    [SerializeField] Color primaryButtonColor = new(0.2f, 0.62f, 0.35f, 1f);
    [SerializeField] Color secondaryButtonColor = new(0.22f, 0.28f, 0.38f, 1f);

    GameObject _winRoot;
    GameObject _loseRoot;
    bool _shown;

    void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (battleOrchestrator == null)
            battleOrchestrator = FindFirstObjectByType<PvPBattleOrchestrator>();

        BuildUiIfNeeded();
        HideAll();
    }

    void OnEnable()
    {
        if (battleOrchestrator == null)
            battleOrchestrator = FindFirstObjectByType<PvPBattleOrchestrator>();

        if (battleOrchestrator == null)
            return;

        battleOrchestrator.PlayerWonEvent.AddListener(ShowWin);
        battleOrchestrator.BotWonEvent.AddListener(ShowLose);
    }

    void OnDisable()
    {
        if (battleOrchestrator == null)
            return;

        battleOrchestrator.PlayerWonEvent.RemoveListener(ShowWin);
        battleOrchestrator.BotWonEvent.RemoveListener(ShowLose);
    }

    void OnDestroy()
    {
        if (_winRoot != null)
            Destroy(_winRoot);

        if (_loseRoot != null)
            Destroy(_loseRoot);
    }

    public void ShowWin()
    {
        if (_shown)
            return;

        _shown = true;
        HideAll();
        if (_winRoot != null)
            _winRoot.SetActive(true);
    }

    public void ShowLose()
    {
        if (_shown)
            return;

        _shown = true;
        HideAll();
        if (_loseRoot != null)
            _loseRoot.SetActive(true);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void RestartMatch()
    {
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    void HideAll()
    {
        if (_winRoot != null)
            _winRoot.SetActive(false);

        if (_loseRoot != null)
            _loseRoot.SetActive(false);
    }

    void BuildUiIfNeeded()
    {
        if (targetCanvas == null || (_winRoot != null && _loseRoot != null))
            return;

        _winRoot = BuildPopup(
            "WinPopup",
            "Победа!",
            "Вы разбили кольцо соперника.",
            winPanelColor,
            showRestartButton: true);

        _loseRoot = BuildPopup(
            "LosePopup",
            "Поражение",
            "Ваше кольцо уничтожено.",
            losePanelColor,
            showRestartButton: false);
    }

    GameObject BuildPopup(
        string rootName,
        string title,
        string subtitle,
        Color panelColor,
        bool showRestartButton)
    {
        GameObject root = CreateUiObject(rootName, targetCanvas.transform);
        StretchFullScreen(root);
        root.transform.SetAsLastSibling();

        Image overlay = root.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = true;

        GameObject panel = CreateUiObject("Panel", root.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.18f, 0.28f);
        panelRect.anchorMax = new Vector2(0.82f, 0.72f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text titleLabel = CreateText("Title", panel.transform, title, 52, FontStyles.Bold);
        AddLayoutElement(titleLabel.gameObject, preferredHeight: 64f);

        TMP_Text subtitleLabel = CreateText("Subtitle", panel.transform, subtitle, 26, FontStyles.Normal);
        subtitleLabel.alignment = TextAlignmentOptions.Center;
        AddLayoutElement(subtitleLabel.gameObject, preferredHeight: 72f);

        GameObject buttonsRow = CreateUiObject("Buttons", panel.transform);
        AddLayoutElement(buttonsRow, preferredHeight: 68f);

        HorizontalLayoutGroup buttonsLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 16f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = true;

        Button menuButton = CreateButton(buttonsRow.transform, "Главное меню", secondaryButtonColor, 240f, 64f);
        menuButton.onClick.AddListener(GoToMainMenu);

        if (showRestartButton)
        {
            Button restartButton = CreateButton(buttonsRow.transform, "Ещё раз", primaryButtonColor, 240f, 64f);
            restartButton.onClick.AddListener(RestartMatch);
        }

        root.SetActive(false);
        return root;
    }

    Button CreateButton(Transform parent, string label, Color color, float width, float height)
    {
        GameObject go = CreateUiObject($"Button_{label}", parent);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        Image image = go.AddComponent<Image>();
        image.color = color;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.85f;
        colors.selectedColor = color;
        button.colors = colors;

        TMP_Text text = CreateText("Label", go.transform, label, 24, FontStyles.Bold);
        StretchFullScreen(text.gameObject);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }

    TMP_Text CreateText(string name, Transform parent, string value, float fontSize, FontStyles style)
    {
        GameObject go = CreateUiObject(name, parent);
        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
            text.font = fontAsset;

        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.enableWordWrapping = true;
        return text;
    }

    static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFullScreen(GameObject go)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void AddLayoutElement(GameObject go, float preferredHeight)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();

        element.preferredHeight = preferredHeight;
    }
}
