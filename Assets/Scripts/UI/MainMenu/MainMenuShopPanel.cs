using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuShopPanel : MainMenuOverlayPanel
{
    readonly bool _isGoldShop;
    readonly MainMenuToast _toast;

    public MainMenuShopPanel(Canvas canvas, MainMenuUiStyle style, bool isGoldShop, MainMenuToast toast, Action onClose)
        : base(canvas, style, onClose)
    {
        _isGoldShop = isGoldShop;
        _toast = toast;
    }

    protected override string GetOverlayName() => _isGoldShop ? "GoldShopOverlay" : "ShopOverlay";

    protected override Vector2 GetPanelSize() => _isGoldShop ? new Vector2(560f, 480f) : new Vector2(900f, 620f);

    protected override void BuildContent(Transform panel)
    {
        if (_isGoldShop)
            BuildGoldOffers(panel);
        else
            BuildMainShop(panel);
    }

    void BuildMainShop(Transform panel)
    {
        string[] namesRu = { "Сундук скинов", "Усиление XP", "Рамка профиля", "Буст монет", "Щит арены", "Стартовый набор" };
        string[] namesEn = { "Skin chest", "XP boost", "Profile frame", "Coin boost", "Arena shield", "Starter pack" };
        int[] prices = { 500, 300, 150, 800, 450, 1200 };

        GameObject grid = MainMenuUiFactory.CreateUiObject("Grid", panel);
        MainMenuUiFactory.AddLayoutElement(grid, 420f);

        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(260f, 120f);
        gridLayout.spacing = new Vector2(12f, 12f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;

        for (int i = 0; i < namesRu.Length; i++)
        {
            int captured = i;
            GameObject card = MainMenuUiFactory.CreateUiObject($"Item_{i}", grid.transform);
            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0f, 0f, 0f, 0.3f);

            string title = MenuLocalization.Get(namesRu[captured], namesEn[captured]);
            TMP_Text name = MainMenuUiFactory.CreateText("Name", card.transform, Style, title, 18, FontStyles.Bold);
            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(10f, -36f);
            nameRect.offsetMax = new Vector2(-10f, -8f);

            Button buy = MainMenuUiFactory.CreateButton(card.transform, Style,
                MenuLocalization.Get("Купить", "Buy"), Style.ConfirmColor, 36f);
            RectTransform buyRect = buy.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.anchoredPosition = new Vector2(0f, 10f);
            buyRect.sizeDelta = new Vector2(120f, 36f);

            int price = prices[captured];
            buy.onClick.AddListener(() => TryPurchase(title, price, isStub: true));
        }
    }

    void BuildGoldOffers(Transform panel)
    {
        string[] packsRu = { "+500 золота", "+2000 золота", "+10000 золота" };
        string[] packsEn = { "+500 gold", "+2000 gold", "+10000 gold" };
        int[] rewards = { 500, 2000, 10000 };
        for (int i = 0; i < packsRu.Length; i++)
        {
            int captured = i;
            GameObject row = MainMenuUiFactory.CreateUiObject($"GoldPack_{i}", panel);
            MainMenuUiFactory.AddLayoutElement(row, 72f);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;

            TMP_Text label = MainMenuUiFactory.CreateText("Label", row.transform, Style,
                MenuLocalization.Get(packsRu[captured], packsEn[captured]), 22, FontStyles.Normal);
            MainMenuUiFactory.AddLayoutElement(label.gameObject, 48f, 0f);

            Button buy = MainMenuUiFactory.CreateButton(row.transform, Style,
                MenuLocalization.Get("Получить", "Get"), Style.AccentBlue, 48f, 180f);
            buy.onClick.AddListener(() =>
            {
                PlayerProgressUtility.AddCoins(rewards[captured]);
                _toast?.Show(MenuLocalization.Get("Золото получено (заглушка IAP).", "Gold received (IAP stub)."));
                Refresh();
            });
        }

        TMP_Text hint = MainMenuUiFactory.CreateText("Hint", panel.transform, Style,
            MenuLocalization.Get("Покупки за реальные деньги — заглушка.", "Real-money purchases are a stub."),
            16, FontStyles.Italic, new Color(0.75f, 0.75f, 0.75f, 1f));
        hint.alignment = TextAlignmentOptions.Center;
        MainMenuUiFactory.AddLayoutElement(hint.gameObject, 36f);
    }

    void TryPurchase(string itemName, int price, bool isStub)
    {
        if (!PlayerProgressUtility.HasSave)
            return;

        if (isStub)
        {
            if (!PlayerProgressUtility.TrySpendCoins(price))
            {
                _toast?.Show(MenuLocalization.Get("Недостаточно золота.", "Not enough gold."));
                return;
            }

            _toast?.Show(MenuLocalization.Get(
                $"Куплено: {itemName} (заглушка).",
                $"Purchased: {itemName} (stub)."));
            Refresh();
            return;
        }

        _toast?.Show(MenuLocalization.Get("Скоро в игре.", "Coming soon."));
    }

    public override void Refresh()
    {
        TitleLabel.text = _isGoldShop
            ? MenuLocalization.Get("ПОКУПКА ЗОЛОТА", "BUY GOLD")
            : MenuLocalization.Get("МАГАЗИН", "SHOP");
    }

    public bool TryBindExtended(Transform canvasRoot)
    {
        if (!TryBindExisting(canvasRoot, GetOverlayName()))
            return false;

        Transform panel = Root.transform.Find("Panel");
        if (panel == null)
            return false;

        if (_isGoldShop)
            RewireGoldOffers(panel);
        else
            RewireMainShop(panel);

        return true;
    }

    void RewireMainShop(Transform panel)
    {
        string[] namesRu = { "Сундук скинов", "Усиление XP", "Рамка профиля", "Буст монет", "Щит арены", "Стартовый набор" };
        string[] namesEn = { "Skin chest", "XP boost", "Profile frame", "Coin boost", "Arena shield", "Starter pack" };
        int[] prices = { 500, 300, 150, 800, 450, 1200 };

        Transform grid = panel.Find("Grid");
        if (grid == null)
            return;

        for (int i = 0; i < grid.childCount && i < namesRu.Length; i++)
        {
            int captured = i;
            string title = MenuLocalization.Get(namesRu[captured], namesEn[captured]);
            int price = prices[captured];
            Button buy = grid.GetChild(captured).GetComponentInChildren<Button>();
            if (buy == null)
                continue;

            buy.onClick.RemoveAllListeners();
            buy.onClick.AddListener(() => TryPurchase(title, price, isStub: true));
        }
    }

    void RewireGoldOffers(Transform panel)
    {
        int[] rewards = { 500, 2000, 10000 };
        for (int i = 0; i < rewards.Length; i++)
        {
            int captured = i;
            Transform row = panel.Find($"GoldPack_{i}");
            if (row == null)
                continue;

            Button buy = row.GetComponentInChildren<Button>();
            if (buy == null)
                continue;

            buy.onClick.RemoveAllListeners();
            buy.onClick.AddListener(() =>
            {
                PlayerProgressUtility.AddCoins(rewards[captured]);
                _toast?.Show(MenuLocalization.Get("Золото получено (заглушка IAP).", "Gold received (IAP stub)."));
                Refresh();
            });
        }
    }
}
