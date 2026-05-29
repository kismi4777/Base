using System.Collections;
using TMPro;
using UnityEngine;

public sealed class MainMenuToast : MonoBehaviour
{
    [SerializeField] float showDuration = 2.2f;

    MainMenuUiStyle _style;
    GameObject _root;
    TMP_Text _label;
    Coroutine _hideRoutine;

    public void Initialize(Canvas canvas, MainMenuUiStyle style)
    {
        _style = style;
        _root = MainMenuUiFactory.CreateUiObject("Toast", canvas.transform);
        RectTransform rect = _root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(640f, 64f);

        UnityEngine.UI.Image bg = _root.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.05f, 0.08f, 0.12f, 0.94f);

        _label = MainMenuUiFactory.CreateText("Text", _root.transform, _style, string.Empty, 22, FontStyles.Normal);
        MainMenuUiFactory.StretchFullScreen(_label.gameObject);
        _label.alignment = TextAlignmentOptions.Center;

        _root.SetActive(false);
        _root.transform.SetAsLastSibling();
    }

    public void Show(string message)
    {
        if (_root == null)
            return;

        _label.text = message;
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);
        _root.SetActive(false);
        _hideRoutine = null;
    }
}
