using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuCharactersPanel : MainMenuOverlayPanel
{
    readonly BallSkinCatalog _catalog;
    readonly MainMenuToast _toast;

    TMP_Text _skinNameLabel;
    TMP_Text _descriptionLabel;
    RectTransform _buttonsRoot;
    readonly List<SkinButtonBinding> _buttons = new();
    int _selectedIndex;

    struct SkinButtonBinding
    {
        public Button Button;
        public Image Background;
        public int Index;
    }

    public MainMenuCharactersPanel(Canvas canvas, MainMenuUiStyle style, BallSkinCatalog catalog,
        MainMenuToast toast, Action onClose)
        : base(canvas, style, onClose)
    {
        _catalog = catalog;
        _toast = toast;
    }

    protected override string GetOverlayName() => "CharactersOverlay";

    protected override string GetFantasyPrefabPath() => MainMenuUiFantasyAssets.CharactersPanelPath;

    protected override string GetFantasyPanelRootName() => "CharacterSelect";

    protected override Vector2 GetPanelSize() => new Vector2(920f, 640f);

    protected override void BuildContent(Transform panel)
    {
        BallSkinSelectionStorage.Load();
        _catalog.EnsurePopulated();

        _skinNameLabel = MainMenuUiFactory.CreateText("SkinName", panel.transform, Style, string.Empty, 28, FontStyles.Bold);
        _skinNameLabel.alignment = TextAlignmentOptions.Center;
        MainMenuUiFactory.AddLayoutElement(_skinNameLabel.gameObject, 40f);

        _descriptionLabel = MainMenuUiFactory.CreateText("Description", panel.transform, Style, string.Empty, 18, FontStyles.Normal);
        _descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
        MainMenuUiFactory.AddLayoutElement(_descriptionLabel.gameObject, 90f);

        GameObject scrollRow = MainMenuUiFactory.CreateUiObject("SkinScroll", panel.transform);
        MainMenuUiFactory.AddLayoutElement(scrollRow, 130f);
        ScrollRect scroll = scrollRow.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;

        GameObject viewport = MainMenuUiFactory.CreateUiObject("Viewport", scrollRow.transform);
        MainMenuUiFactory.StretchFullScreen(viewport);
        viewport.AddComponent<RectMask2D>();
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.2f);
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = MainMenuUiFactory.CreateUiObject("Content", viewport.transform);
        _buttonsRoot = content.GetComponent<RectTransform>();
        _buttonsRoot.anchorMin = new Vector2(0f, 0.5f);
        _buttonsRoot.anchorMax = new Vector2(0f, 0.5f);
        _buttonsRoot.pivot = new Vector2(0f, 0.5f);

        HorizontalLayoutGroup buttonsLayout = content.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 10f;
        buttonsLayout.childAlignment = TextAnchor.MiddleLeft;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandHeight = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _buttonsRoot;

        BuildSkinButtons();

        GameObject confirmGo = MainMenuUiFactory.CreateUiObject("ConfirmButton", panel.transform);
        MainMenuUiFactory.AddLayoutElement(confirmGo, 52f);
        Image confirmImage = confirmGo.AddComponent<Image>();
        confirmImage.color = Style.ConfirmColor;
        Button confirm = confirmGo.AddComponent<Button>();
        confirm.targetGraphic = confirmImage;
        TMP_Text confirmLabel = MainMenuUiFactory.CreateText("Label", confirmGo.transform, Style,
            MenuLocalization.Get("Выбрать", "Select"), 20, FontStyles.Bold);
        MainMenuUiFactory.StretchFullScreen(confirmLabel.gameObject);
        confirmLabel.raycastTarget = false;
        confirm.onClick.AddListener(ConfirmSelection);

        _selectedIndex = Mathf.Max(0, _catalog.IndexOf(BallSkinSelectionStorage.SelectedSkin));
        SelectIndex(_selectedIndex, save: false);
    }

    void BuildSkinButtons()
    {
        _buttons.Clear();
        int count = _catalog.EntryCount;

        for (int i = 0; i < count; i++)
        {
            BallSkinCatalog.Entry entry = _catalog.GetEntryByIndex(i);
            int captured = i;

            GameObject go = MainMenuUiFactory.CreateUiObject($"Skin_{entry.skinId}", _buttonsRoot);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 150f;
            layout.preferredHeight = 52f;

            Image bg = go.AddComponent<Image>();
            bg.color = Style.ButtonColor;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => SelectIndex(captured, save: false));

            TMP_Text label = MainMenuUiFactory.CreateText("Label", go.transform, Style, entry.displayName, 18, FontStyles.Bold);
            MainMenuUiFactory.StretchFullScreen(label.gameObject);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            _buttons.Add(new SkinButtonBinding { Button = button, Background = bg, Index = captured });
        }
    }

    void SelectIndex(int index, bool save)
    {
        if (_catalog.EntryCount <= 0)
            return;

        _selectedIndex = Mathf.Clamp(index, 0, _catalog.EntryCount - 1);
        BallSkinCatalog.Entry entry = _catalog.GetEntryByIndex(_selectedIndex);

        if (_skinNameLabel != null)
            _skinNameLabel.text = entry.displayName;

        if (_descriptionLabel != null)
            _descriptionLabel.text = entry.abilityDescription;

        for (int i = 0; i < _buttons.Count; i++)
        {
            SkinButtonBinding binding = _buttons[i];
            if (binding.Background != null)
                binding.Background.color = binding.Index == _selectedIndex ? Style.ButtonSelectedColor : Style.ButtonColor;
        }

        if (save)
            BallSkinSelectionStorage.Save(entry.skinId);
    }

    void ConfirmSelection()
    {
        BallSkinCatalog.Entry entry = _catalog.GetEntryByIndex(_selectedIndex);
        BallSkinSelectionStorage.Save(entry.skinId);
        _toast?.Show(MenuLocalization.Get(
            $"Выбран скин: {entry.displayName}",
            $"Selected skin: {entry.displayName}"));
        Close();
    }

    public override void Refresh()
    {
        TitleLabel.text = MenuLocalization.Get("ПЕРСОНАЖИ", "CHARACTERS");
        SelectIndex(_selectedIndex, save: false);
    }

    public bool TryBindExtended(Transform canvasRoot)
    {
        if (!TryBindExisting(canvasRoot, GetOverlayName()))
            return false;

        _catalog.EnsurePopulated();
        BallSkinSelectionStorage.Load();

        Transform panel = Root.transform.Find("Panel");
        if (panel == null)
            return false;

        _skinNameLabel = panel.Find("SkinName")?.GetComponent<TMP_Text>();
        _descriptionLabel = panel.Find("Description")?.GetComponent<TMP_Text>();
        _buttonsRoot = panel.Find("SkinScroll/Viewport/Content")?.GetComponent<RectTransform>();

        _buttons.Clear();
        if (_buttonsRoot != null)
        {
            for (int i = 0; i < _buttonsRoot.childCount; i++)
            {
                Transform child = _buttonsRoot.GetChild(i);
                Button button = child.GetComponent<Button>();
                Image bg = child.GetComponent<Image>();
                if (button == null || bg == null)
                    continue;

                int catalogIndex = ParseSkinButtonIndex(child.name, _catalog);
                if (catalogIndex < 0)
                    catalogIndex = _buttons.Count;

                int captured = catalogIndex;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectIndex(captured, save: false));
                _buttons.Add(new SkinButtonBinding { Button = button, Background = bg, Index = captured });
            }
        }

        Button confirm = panel.Find("ConfirmButton")?.GetComponent<Button>();
        if (confirm != null)
        {
            confirm.onClick.RemoveAllListeners();
            confirm.onClick.AddListener(ConfirmSelection);
        }

        _selectedIndex = Mathf.Max(0, _catalog.IndexOf(BallSkinSelectionStorage.SelectedSkin));
        return true;
    }

    static int ParseSkinButtonIndex(string objectName, BallSkinCatalog catalog)
    {
        if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("Skin_") || catalog == null)
            return -1;

        string id = objectName.Substring("Skin_".Length);
        if (!System.Enum.TryParse(id, out BallSkinId skinId))
            return -1;

        return catalog.IndexOf(skinId);
    }
}
