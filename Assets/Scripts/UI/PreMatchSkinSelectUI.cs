using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Экран выбора скина перед началом PvP-матча.</summary>
public sealed class PreMatchSkinSelectUI : MonoBehaviour
{
    [Header("Данные")]
    [SerializeField] BallSkinCatalog catalog;

    [Header("Связи")]
    [SerializeField] Canvas targetCanvas;
    [SerializeField] SlingshotShooter previewBall;
    [SerializeField] MatchStartGate matchStartGate;

    [Header("Шрифт")]
    [SerializeField] TMP_FontAsset fontAsset;

    [Header("Оформление")]
    [SerializeField] Color overlayColor = new(0f, 0f, 0f, 0.82f);
    [SerializeField] Color panelColor = new(0.12f, 0.14f, 0.2f, 0.96f);
    [SerializeField] Color buttonColor = new(0.22f, 0.28f, 0.38f, 1f);
    [SerializeField] Color buttonSelectedColor = new(0.35f, 0.55f, 0.85f, 1f);
    [SerializeField] Color confirmColor = new(0.2f, 0.62f, 0.35f, 1f);

    GameObject _panelRoot;
    TMP_Text _titleLabel;
    TMP_Text _skinNameLabel;
    TMP_Text _descriptionLabel;
    RectTransform _skinButtonsRoot;
    readonly List<SkinButtonBinding> _skinButtons = new();
    int _selectedIndex;

    struct SkinButtonBinding
    {
        public Button Button;
        public Image Background;
        public int CatalogIndex;
    }

    void Awake()
    {
        BallSkinSelectionStorage.Load();

        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BallSkinCatalog>();
            Debug.LogWarning("PreMatchSkinSelectUI: каталог не назначен, используются значения по умолчанию.", this);
        }

        catalog.EnsurePopulated();

        if (matchStartGate == null)
            matchStartGate = FindFirstObjectByType<MatchStartGate>();

        if (previewBall == null)
            previewBall = ResolvePlayerBall();

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        BuildUiIfNeeded();
        LockGameplayInput(true);

        _selectedIndex = Mathf.Max(0, catalog.IndexOf(BallSkinSelectionStorage.SelectedSkin));
        if (_selectedIndex < 0)
            _selectedIndex = 0;

        SelectIndex(_selectedIndex, save: false);
    }

    void OnDestroy()
    {
        if (_panelRoot != null)
            Destroy(_panelRoot);
    }

    public void ConfirmSelection()
    {
        BallSkinCatalog.Entry entry = catalog.GetEntryByIndex(_selectedIndex);
        BallSkinSelectionStorage.Save(entry.skinId);
        ApplyPreviewSkin(entry.skinId);

        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        LockGameplayInput(false);

        if (matchStartGate != null)
        {
            matchStartGate.StartMatch();
            return;
        }

        PvPBattleOrchestrator orchestrator = FindFirstObjectByType<PvPBattleOrchestrator>();
        if (orchestrator != null)
            orchestrator.InitializeMatch();
        else
            Debug.LogWarning("PreMatchSkinSelectUI: MatchStartGate и PvPBattleOrchestrator не найдены.", this);
    }

    void SelectIndex(int index, bool save)
    {
        if (catalog.EntryCount <= 0)
            return;

        _selectedIndex = Mathf.Clamp(index, 0, catalog.EntryCount - 1);
        BallSkinCatalog.Entry entry = catalog.GetEntryByIndex(_selectedIndex);

        if (_skinNameLabel != null)
            _skinNameLabel.text = entry.displayName;

        if (_descriptionLabel != null)
            _descriptionLabel.text = entry.abilityDescription;

        RefreshButtonHighlights();
        ApplyPreviewSkin(entry.skinId);

        if (save)
            BallSkinSelectionStorage.Save(entry.skinId);
    }

    void ApplyPreviewSkin(BallSkinId skinId)
    {
        if (previewBall == null)
            previewBall = ResolvePlayerBall();

        if (previewBall != null && previewBall.TryGetComponent(out BallSkinController skinController))
            skinController.SetSkin(skinId);
    }

    void RefreshButtonHighlights()
    {
        for (int i = 0; i < _skinButtons.Count; i++)
        {
            SkinButtonBinding binding = _skinButtons[i];
            if (binding.Background == null)
                continue;

            binding.Background.color = binding.CatalogIndex == _selectedIndex
                ? buttonSelectedColor
                : buttonColor;
        }
    }

    void LockGameplayInput(bool locked)
    {
        SlingshotShooter[] shooters = FindObjectsByType<SlingshotShooter>(FindObjectsSortMode.None);
        for (int i = 0; i < shooters.Length; i++)
        {
            SlingshotShooter shooter = shooters[i];
            if (shooter == null)
                continue;

            shooter.IsInputLocked = locked;
        }
    }

    SlingshotShooter ResolvePlayerBall()
    {
        SlingshotShooter[] shooters = FindObjectsByType<SlingshotShooter>(FindObjectsSortMode.None);
        for (int i = 0; i < shooters.Length; i++)
        {
            SlingshotShooter shooter = shooters[i];
            if (shooter != null && !shooter.gameObject.name.Contains("Bot"))
                return shooter;
        }

        return null;
    }

    void BuildUiIfNeeded()
    {
        if (targetCanvas == null || _panelRoot != null)
            return;

        _panelRoot = CreateUiObject("SkinSelectOverlay", targetCanvas.transform);
        StretchFullScreen(_panelRoot);

        Image overlay = _panelRoot.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = true;

        GameObject panel = CreateUiObject("Panel", _panelRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.1f);
        panelRect.anchorMax = new Vector2(0.92f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleLabel = CreateText("Title", panel.transform, "Выберите скин мяча", 42, FontStyles.Bold);
        AddLayoutElement(_titleLabel.gameObject, preferredHeight: 56f);

        _skinNameLabel = CreateText("SkinName", panel.transform, string.Empty, 34, FontStyles.Bold);
        AddLayoutElement(_skinNameLabel.gameObject, preferredHeight: 44f);

        _descriptionLabel = CreateText("Description", panel.transform, string.Empty, 24, FontStyles.Normal);
        _descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
        AddLayoutElement(_descriptionLabel.gameObject, preferredHeight: 110f);

        GameObject scrollRow = CreateUiObject("SkinButtonsScroll", panel.transform);
        AddLayoutElement(scrollRow, preferredHeight: 120f);
        ScrollRect scroll = scrollRow.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;

        GameObject viewport = CreateUiObject("Viewport", scrollRow.transform);
        StretchFullScreen(viewport);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewport.AddComponent<RectMask2D>();
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.15f);
        scroll.viewport = viewportRect;

        GameObject content = CreateUiObject("Content", viewport.transform);
        _skinButtonsRoot = content.GetComponent<RectTransform>();
        _skinButtonsRoot.anchorMin = new Vector2(0f, 0.5f);
        _skinButtonsRoot.anchorMax = new Vector2(0f, 0.5f);
        _skinButtonsRoot.pivot = new Vector2(0f, 0.5f);
        _skinButtonsRoot.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup buttonsLayout = content.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 10f;
        buttonsLayout.childAlignment = TextAnchor.MiddleLeft;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = true;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        scroll.content = _skinButtonsRoot;

        BuildSkinButtons();

        GameObject confirmRow = CreateUiObject("ConfirmRow", panel.transform);
        AddLayoutElement(confirmRow, preferredHeight: 72f);
        HorizontalLayoutGroup confirmLayout = confirmRow.AddComponent<HorizontalLayoutGroup>();
        confirmLayout.childAlignment = TextAnchor.MiddleCenter;
        confirmLayout.childControlWidth = false;
        confirmLayout.childControlHeight = true;

        Button confirmButton = CreateButton(confirmRow.transform, "В бой!", confirmColor, 280f, 64f);
        confirmButton.onClick.AddListener(ConfirmSelection);
    }

    void BuildSkinButtons()
    {
        _skinButtons.Clear();

        int count = catalog.EntryCount;
        for (int i = 0; i < count; i++)
        {
            BallSkinCatalog.Entry entry = catalog.GetEntryByIndex(i);
            int capturedIndex = i;

            Button button = CreateButton(_skinButtonsRoot, entry.displayName, buttonColor, 150f, 52f);
            button.onClick.AddListener(() => SelectIndex(capturedIndex, save: true));

            _skinButtons.Add(new SkinButtonBinding
            {
                Button = button,
                Background = button.GetComponent<Image>(),
                CatalogIndex = capturedIndex
            });
        }
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

        TMP_Text text = CreateText("Label", go.transform, label, 22, FontStyles.Bold);
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
