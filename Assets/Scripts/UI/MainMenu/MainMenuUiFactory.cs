using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuUiFactory
{
    public static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void StretchFullScreen(GameObject go)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void AddLayoutElement(GameObject go, float preferredHeight, float preferredWidth = -1f)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();

        element.preferredHeight = preferredHeight;
        if (preferredWidth > 0f)
            element.preferredWidth = preferredWidth;
    }

    public static GameObject CreatePanel(string name, Transform parent, MainMenuUiStyle style,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject panel = CreateUiObject(name, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image bg = panel.AddComponent<Image>();
        bg.color = style.PanelColor;
        bg.raycastTarget = true;

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = style.PanelBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        return panel;
    }

    public static GameObject CreateFullscreenOverlay(string name, Transform canvasRoot, MainMenuUiStyle style, out Image dimImage)
    {
        GameObject overlay = CreateUiObject(name, canvasRoot);
        StretchFullScreen(overlay);

        dimImage = overlay.AddComponent<Image>();
        dimImage.color = style.OverlayDim;
        dimImage.raycastTarget = true;

        return overlay;
    }

    public static void AddBackdropClose(GameObject overlay, Image dimImage, UnityEngine.Events.UnityAction onClose)
    {
        Button backdrop = overlay.AddComponent<Button>();
        backdrop.transition = Selectable.Transition.None;
        backdrop.targetGraphic = dimImage;
        backdrop.onClick.AddListener(onClose);
    }

    public static TMP_Text CreateText(string name, Transform parent, MainMenuUiStyle style, string value,
        float fontSize, FontStyles fontStyle, Color? color = null)
    {
        GameObject go = CreateUiObject(name, parent);
        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        if (style.Font != null)
            text.font = style.Font;

        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color ?? Color.white;
        text.enableWordWrapping = true;
        return text;
    }

    public static Button CreateButton(Transform parent, MainMenuUiStyle style, string label, Color color,
        float height, float width = 0f)
    {
        GameObject go = CreateUiObject($"Button_{label}", parent);
        if (width > 0f)
        {
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
        }

        if (height > 0f)
            AddLayoutElement(go, height);

        Image image = go.AddComponent<Image>();
        image.color = color;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText("Label", go.transform, style, label, 20, FontStyles.Bold);
        StretchFullScreen(text.gameObject);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }

    public static Button CreateIconButton(Transform parent, MainMenuUiStyle style, string icon, float size)
    {
        GameObject go = CreateUiObject("IconButton", parent);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = size;
        layout.preferredHeight = size;

        Image image = go.AddComponent<Image>();
        image.color = style.PanelColor;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = style.PanelBorderColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = CreateText("Icon", go.transform, style, icon, 28, FontStyles.Normal);
        StretchFullScreen(label.gameObject);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return button;
    }
}
