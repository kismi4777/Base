using TMPro;
using UnityEngine;

public class HighScoreDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField] string format = "Рекорд: {0}";

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label == null || SaveManager.Instance == null)
            return;

        label.text = string.Format(format, SaveManager.Instance.Data.HighScore);
    }
}
