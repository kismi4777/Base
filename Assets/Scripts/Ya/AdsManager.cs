using System;
using UnityEngine;
using BananaParty.YandexGames; // Правильное пространство имен

public class AdsManager : MonoBehaviour
{
    // Делаем скрипт Синглтоном для удобного доступа отовсюду
    public static AdsManager Instance { get; private set; }

    private void Awake()
    {
        // Гарантируем, что менеджер будет только один и не уничтожится при смене сцен
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

    // Вызов рекламы за вознаграждение (Rewarded Video)
    public void ShowRewarded(Action onRewarded, Action onClose = null)
    {
        // Обязательно глушим игру и останавливаем время ДО вызова рекламы
        AudioListener.pause = true;
        Time.timeScale = 0f;

#if !UNITY_WEBGL || UNITY_EDITOR
        // Заглушка для тестов внутри Unity:
        // Сразу выдаем награду и снимаем игру с паузы, чтобы не было ошибок
        Debug.Log("[AdsManager] Эмуляция просмотра Rewarded Video в редакторе...");
        onRewarded?.Invoke();
        ResumeGame(onClose);
#else
        // Реальный вызов рекламы для браузера (использует JavaScript плагина)
        VideoAd.Show(
            onOpenCallback: null,
            onRewardedCallback: () => onRewarded?.Invoke(),
            onCloseCallback: () => ResumeGame(onClose),
            onErrorCallback: (error) => 
            {
                Debug.LogWarning("[AdsManager] Ошибка показа Rewarded: " + error);
                ResumeGame(onClose);
            }
        );
#endif
    }

    // Вызов обычной межстраничной рекламы (Interstitial)
    public void ShowInterstitial(Action onClose = null)
    {
        // Обязательно глушим игру и останавливаем время ДО вызова рекламы
        AudioListener.pause = true;
        Time.timeScale = 0f;

#if !UNITY_WEBGL || UNITY_EDITOR
        // Заглушка для тестов внутри Unity
        Debug.Log("[AdsManager] Эмуляция просмотра Interstitial в редакторе...");
        ResumeGame(onClose);
#else
        // Реальный вызов рекламы для браузера
        InterstitialAd.Show(
            onOpenCallback: null,
            onCloseCallback: (wasShown) => ResumeGame(onClose),
            onErrorCallback: (error) => 
            {
                Debug.LogWarning("[AdsManager] Ошибка показа Interstitial: " + error);
                ResumeGame(onClose);
            }
        );
#endif
    }

    // Вспомогательный метод для плавного возврата игры в норму
    private void ResumeGame(Action onCloseCallback)
    {
        // Включаем звук и физику обратно
        AudioListener.pause = false;
        Time.timeScale = 1f;

        // Вызываем код, который должен сработать после закрытия рекламы (если он есть)
        onCloseCallback?.Invoke();
    }
}