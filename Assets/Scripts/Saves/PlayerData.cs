using System;

[Serializable]
public class PlayerData
{
    public int Coins;
    public int HighScore;
    public bool IsSoundOn = true;

    // Устаревшее поле — мигрируется в HighScore при загрузке
    public int MaxScore;
}
