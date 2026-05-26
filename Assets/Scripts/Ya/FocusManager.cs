using UnityEngine;

public class FocusManager : MonoBehaviour
{
    void OnApplicationFocus(bool hasFocus)
    {
        PauseGame(!hasFocus);
    }

    void OnApplicationPause(bool isPaused)
    {
        PauseGame(isPaused);
    }

    static void PauseGame(bool isPaused)
    {
        AudioListener.pause = isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
