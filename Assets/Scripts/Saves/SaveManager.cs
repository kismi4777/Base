using System;
using UnityEngine;
using BananaParty.YandexGames;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public PlayerData Data = new PlayerData();

    const string LocalSaveKey = "LocalSave";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadData(Action onLoadedCallback)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        string json = PlayerPrefs.GetString(LocalSaveKey, "");
        if (!string.IsNullOrEmpty(json))
            Data = JsonUtility.FromJson<PlayerData>(json);

        MigrateLegacyFields();
        onLoadedCallback?.Invoke();
#else
        PlayerAccount.GetCloudSaveData(
            onSuccessCallback: data =>
            {
                if (!string.IsNullOrEmpty(data))
                    Data = JsonUtility.FromJson<PlayerData>(data);

                MigrateLegacyFields();
                onLoadedCallback?.Invoke();
            },
            onErrorCallback: error =>
            {
                Debug.LogWarning("������ �������� ����������: " + error);
                MigrateLegacyFields();
                onLoadedCallback?.Invoke();
            });
#endif
    }

    void MigrateLegacyFields()
    {
        if (Data.HighScore == 0 && Data.MaxScore > 0)
            Data.HighScore = Data.MaxScore;
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data);

#if !UNITY_WEBGL || UNITY_EDITOR
        PlayerPrefs.SetString(LocalSaveKey, json);
        PlayerPrefs.Save();
#else
        PlayerAccount.SetCloudSaveData(json);
#endif
    }
}
