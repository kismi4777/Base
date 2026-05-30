using System;

public enum MenuLanguage
{
    Russian = 0,
    English = 1,
    Turkish = 2
}

public static class MenuLocalization
{
    public static event Action LanguageChanged;

    static MenuLanguage _language = MenuLanguage.Russian;

    public static MenuLanguage Current
    {
        get => _language;
        set
        {
            if (_language == value)
                return;

            _language = value;
            LanguageChanged?.Invoke();
        }
    }

    public static void LoadFromSave()
    {
        if (SaveManager.Instance == null)
            return;

        _language = SaveManager.Instance.Data.Language switch
        {
            1 => MenuLanguage.English,
            2 => MenuLanguage.Turkish,
            _ => MenuLanguage.Russian
        };
    }

    public static void SaveToPlayerData()
    {
        if (SaveManager.Instance == null)
            return;

        SaveManager.Instance.Data.Language = (int)_language;
        SaveManager.Instance.Save();
    }

    public static string GetLanguageDisplayName() => Current switch
    {
        MenuLanguage.English => "English",
        MenuLanguage.Turkish => "Türkçe",
        _ => "Русский"
    };

    public static string Get(string ru, string en, string tr = null)
    {
        return Current switch
        {
            MenuLanguage.English => en,
            MenuLanguage.Turkish => tr ?? en,
            _ => ru
        };
    }
}
