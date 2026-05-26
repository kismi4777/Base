using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button playButton;

    void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(StartGame);
    }

    void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(StartGame);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(SceneNames.Gameplay);
    }
}
