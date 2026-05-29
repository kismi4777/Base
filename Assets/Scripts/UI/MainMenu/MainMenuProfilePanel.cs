using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuProfilePanel : MainMenuOverlayPanel
{
    readonly MainMenuToast _toast;
    TMP_InputField _nameInput;

    public MainMenuProfilePanel(Canvas canvas, MainMenuUiStyle style, MainMenuToast toast, Action onClose)
        : base(canvas, style, onClose)
    {
        _toast = toast;
    }

    protected override string GetOverlayName() => "ProfileOverlay";

    protected override Vector2 GetPanelSize() => new Vector2(520f, 360f);

    protected override void BuildContent(Transform panel)
    {
        TMP_Text hint = MainMenuUiFactory.CreateText("Hint", panel.transform, Style,
            MenuLocalization.Get("Имя игрока (заглушка редактора):", "Player name (editor stub):"),
            20, FontStyles.Normal);
        MainMenuUiFactory.AddLayoutElement(hint.gameObject, 32f);

        GameObject inputRow = MainMenuUiFactory.CreateUiObject("NameInputRow", panel.transform);
        MainMenuUiFactory.AddLayoutElement(inputRow, 52f);
        Image inputBg = inputRow.AddComponent<Image>();
        inputBg.color = new Color(0.05f, 0.06f, 0.1f, 1f);

        GameObject textArea = MainMenuUiFactory.CreateUiObject("Text", inputRow.transform);
        MainMenuUiFactory.StretchFullScreen(textArea);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.offsetMin = new Vector2(12f, 8f);
        textAreaRect.offsetMax = new Vector2(-12f, -8f);

        TMP_Text inputText = MainMenuUiFactory.CreateText("Text", textArea.transform, Style, string.Empty, 24, FontStyles.Normal);
        inputText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject placeholder = MainMenuUiFactory.CreateUiObject("Placeholder", inputRow.transform);
        MainMenuUiFactory.StretchFullScreen(placeholder);
        TMP_Text placeholderText = MainMenuUiFactory.CreateText("Placeholder", placeholder.transform, Style,
            MenuLocalization.Get("Введите имя", "Enter name"), 24, FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f, 1f));
        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform phRect = placeholder.GetComponent<RectTransform>();
        phRect.offsetMin = new Vector2(12f, 8f);
        phRect.offsetMax = new Vector2(-12f, -8f);

        _nameInput = inputRow.AddComponent<TMP_InputField>();
        _nameInput.textViewport = textAreaRect;
        _nameInput.textComponent = inputText;
        _nameInput.placeholder = placeholderText;
        _nameInput.lineType = TMP_InputField.LineType.SingleLine;
        _nameInput.characterLimit = 20;

        GameObject saveGo = MainMenuUiFactory.CreateUiObject("SaveButton", panel.transform);
        MainMenuUiFactory.AddLayoutElement(saveGo, 48f);
        Image saveImage = saveGo.AddComponent<Image>();
        saveImage.color = Style.ConfirmColor;
        Button save = saveGo.AddComponent<Button>();
        save.targetGraphic = saveImage;
        TMP_Text saveLabel = MainMenuUiFactory.CreateText("Label", saveGo.transform, Style,
            MenuLocalization.Get("Сохранить", "Save"), 20, FontStyles.Bold);
        MainMenuUiFactory.StretchFullScreen(saveLabel.gameObject);
        saveLabel.raycastTarget = false;
        save.onClick.AddListener(SaveProfile);
    }

    public bool TryBindExtended(Transform canvasRoot)
    {
        if (!TryBindExisting(canvasRoot, GetOverlayName()))
            return false;

        Transform panel = Root.transform.Find("Panel");
        if (panel == null)
            return false;

        _nameInput = panel.Find("NameInputRow")?.GetComponent<TMP_InputField>();
        Button save = panel.Find("SaveButton")?.GetComponent<Button>();
        if (save != null)
        {
            save.onClick.RemoveAllListeners();
            save.onClick.AddListener(SaveProfile);
        }

        return true;
    }

    void SaveProfile()
    {
        if (!PlayerProgressUtility.HasSave || _nameInput == null)
            return;

        string name = _nameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            _toast?.Show(MenuLocalization.Get("Имя не может быть пустым.", "Name cannot be empty."));
            return;
        }

        PlayerProgressUtility.Data.PlayerName = name;
        SaveManager.Instance.Save();
        _toast?.Show(MenuLocalization.Get("Профиль сохранён.", "Profile saved."));
        Close();
    }

    public override void Refresh()
    {
        TitleLabel.text = MenuLocalization.Get("ПРОФИЛЬ", "PROFILE");

        if (_nameInput != null && PlayerProgressUtility.HasSave)
            _nameInput.text = PlayerProgressUtility.Data.PlayerName;
    }
}
