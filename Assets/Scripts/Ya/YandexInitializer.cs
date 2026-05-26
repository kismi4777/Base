using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BananaParty.YandexGames;

public class YandexInitializer : MonoBehaviour
{
    IEnumerator Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        yield return null;
        SaveManager.Instance.LoadData(LoadMainMenu);
#else
        yield return YandexGamesSdk.Initialize(OnSdkInitialized);
#endif
    }

    void OnSdkInitialized()
    {
        SaveManager.Instance.LoadData(() =>
        {
            YandexGamesSdk.GameReady();
            LoadMainMenu();
        });
    }

    static void LoadMainMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
