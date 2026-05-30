using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Ссылки на объекты UI главного меню для редактирования в сцене.</summary>
public sealed class MainMenuUiReferences : MonoBehaviour
{
    public GameObject HubRoot;
    public GameObject SettingsOverlay;

    public TMP_Text PlayerNameLabel;
    public TMP_Text LevelBadgeLabel;
    public TMP_Text XpLabel;
    public Image XpFill;
    public TMP_Text GoldLabel;

    public TMP_Text DailyTitle;
    public TMP_Text DailyTimerLabel;
    public List<TMP_Text> TaskTitleLabels = new();
    public List<TMP_Text> TaskProgressLabels = new();
    public List<Image> TaskProgressFills = new();

    public TMP_Text SettingsTitle;
    public Slider SoundFxSlider;
    public Slider MusicSlider;
    public Button LanguageOpenButton;
    public Button LangRuButton;
    public Button LangEnButton;
    public Button LangTrButton;
    public Button LanguageCloseButton;

    public GameObject LanguageOverlay;

    public TMP_Text PlayNavLabel;
    public TMP_Text ShopNavLabel;
    public TMP_Text CharsNavLabel;

    public Button GoldPlusButton;
    public Button SettingsButton;
    public Button PlayNavButton;
    public Button ShopNavButton;
    public Button CharsNavButton;
    public readonly List<Button> DailyTaskButtons = new();

    public GameObject ShopOverlay;
    public GameObject GoldShopOverlay;
    public GameObject CharactersOverlay;
    public GameObject ProfileOverlay;
    public GameObject DailyTaskOverlay;
    public GameObject ToastRoot;
}
