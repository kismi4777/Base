using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class MainMenuOverlayPanel
{
    protected readonly Canvas Canvas;
    protected readonly MainMenuUiStyle Style;
    protected readonly Action OnClose;

    public GameObject Root { get; protected set; }
    protected TMP_Text TitleLabel;

    protected MainMenuOverlayPanel(Canvas canvas, MainMenuUiStyle style, Action onClose)
    {
        Canvas = canvas;
        Style = style;
        OnClose = onClose;
    }

    public bool IsOpen => Root != null && Root.activeSelf;

    protected virtual string GetFantasyPrefabPath() => null;

    public virtual void Build()
    {
        string fantasyPath = GetFantasyPrefabPath();
        if (!string.IsNullOrEmpty(fantasyPath))
        {
            BuildFantasyOverlay(fantasyPath);
            return;
        }

        Root = MainMenuUiFactory.CreateUiObject(GetOverlayName(), Canvas.transform);
        MainMenuUiFactory.StretchFullScreen(Root);

        GameObject backdrop = MainMenuUiFactory.CreateUiObject("Backdrop", Root.transform);
        MainMenuUiFactory.StretchFullScreen(backdrop);
        Image dim = backdrop.AddComponent<Image>();
        dim.color = Style.OverlayDim;
        dim.raycastTarget = true;
        Button backdropButton = backdrop.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.targetGraphic = dim;
        backdropButton.onClick.AddListener(() => Close());

        GameObject panel = MainMenuUiFactory.CreatePanel("Panel", Root.transform, Style,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, GetPanelSize());

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        TitleLabel = MainMenuUiFactory.CreateText("Title", panel.transform, Style, string.Empty, 30, FontStyles.Bold, Style.GoldTextColor);
        TitleLabel.alignment = TextAlignmentOptions.Center;
        MainMenuUiFactory.AddLayoutElement(TitleLabel.gameObject, 40f);

        BuildContent(panel.transform);

        GameObject backGo = MainMenuUiFactory.CreateUiObject("BackButton", panel.transform);
        MainMenuUiFactory.AddLayoutElement(backGo, 48f);
        Image backImage = backGo.AddComponent<Image>();
        backImage.color = Style.ButtonColor;
        Button back = backGo.AddComponent<Button>();
        back.targetGraphic = backImage;
        TMP_Text backLabel = MainMenuUiFactory.CreateText("Label", backGo.transform, Style,
            MenuLocalization.Get("Назад", "Back"), 20, FontStyles.Bold);
        MainMenuUiFactory.StretchFullScreen(backLabel.gameObject);
        backLabel.alignment = TextAlignmentOptions.Center;
        backLabel.raycastTarget = false;
        back.onClick.AddListener(() => Close());

        BlockPanelClickPropagation(panel);

        Root.SetActive(false);
    }

    void BuildFantasyOverlay(string prefabPath)
    {
        Root = MainMenuUiFantasyAssets.BuildFantasyOverlay(GetOverlayName(), prefabPath, Canvas.transform);
        Transform panelRoot = MainMenuUiFantasyAssets.FindDeepChild(Root.transform, GetFantasyPanelRootName())
            ?? (Root.transform.childCount > 0 ? Root.transform.GetChild(0) : null);

        TitleLabel = panelRoot != null
            ? panelRoot.GetComponentInChildren<TMP_Text>(true)
            : null;

        MainMenuUiFantasyAssets.WireCloseButtons(Root.transform, () => Close());
        Root.SetActive(false);
    }

    protected virtual string GetFantasyPanelRootName()
    {
        return GetOverlayName().Replace("Overlay", string.Empty);
    }

    protected abstract string GetOverlayName();
    protected abstract Vector2 GetPanelSize();
    protected abstract void BuildContent(Transform panel);
    public abstract void Refresh();

    public void Open()
    {
        if (Root == null)
            Build();

        Root.SetActive(true);
        Root.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close(bool notify = true)
    {
        if (Root != null)
            Root.SetActive(false);

        if (notify)
            OnClose?.Invoke();
    }

    public void CloseSilently() => Close(notify: false);

    public bool TryBindExisting(Transform canvasRoot, string overlayName)
    {
        if (canvasRoot == null)
            return false;

        Transform found = canvasRoot.Find(overlayName);
        if (found == null)
            return false;

        Root = found.gameObject;

        Transform panel = Root.transform.Find("Panel");
        if (panel != null)
        {
            TitleLabel = panel.Find("Title")?.GetComponent<TMP_Text>();

            Transform back = panel.Find("BackButton");
            if (back != null)
            {
                Button backButton = back.GetComponent<Button>();
                if (backButton != null)
                {
                    backButton.onClick.RemoveAllListeners();
                    backButton.onClick.AddListener(() => Close());
                }
            }

            return true;
        }

        if (!string.IsNullOrEmpty(GetFantasyPrefabPath()))
        {
            MainMenuUiFantasyAssets.WireCloseButtons(Root.transform, () => Close());
            return true;
        }

        return false;
    }

    protected static void BlockPanelClickPropagation(GameObject panel)
    {
        Button blocker = panel.AddComponent<Button>();
        blocker.transition = Selectable.Transition.None;
        blocker.onClick.AddListener(() => { });
    }
}
