using System;

[Serializable]
public class PlayerData
{
    public int Coins;
    public int HighScore;
    public bool IsSoundOn = true;
    public float SoundVolume = 1f;

    public string PlayerName = "Игрок";
    public int Level = 1;
    public int Experience;
    public int ExperienceToNextLevel = 100;

    /// <summary>0 — русский, 1 — английский.</summary>
    public int Language;

    public int DailyShotsProgress;
    public int DailyMatchesProgress;
    public int DailyWinsProgress;
    public bool DailyReward0Claimed;
    public bool DailyReward1Claimed;
    public bool DailyReward2Claimed;

    // Устаревшее поле — мигрируется в HighScore при загрузке
    public int MaxScore;
}
