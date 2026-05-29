using System;

public enum MenuLanguage
{
    Russian = 0,
    English = 1
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

        int code = SaveManager.Instance.Data.Language;
        _language = code == 1 ? MenuLanguage.English : MenuLanguage.Russian;
    }

    public static void SaveToPlayerData()
    {
        if (SaveManager.Instance == null)
            return;

        SaveManager.Instance.Data.Language = (int)_language;
        SaveManager.Instance.Save();
    }

    public static string Get(string ru, string en) =>
        _language == MenuLanguage.English ? en : ru;
}
