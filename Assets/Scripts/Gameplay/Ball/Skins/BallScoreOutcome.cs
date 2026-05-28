/// <summary>Результат обработки гола способностями скина.</summary>
public struct BallScoreOutcome
{
    public bool IsFriendlyHealOnly;
    public int HoopDamage;
    public int ShieldDamage;
    public bool IsCritical;
    public bool IgnoreShieldDefense;
    public int PaladinShieldHeal;
    public int WampirShieldHeal;
    public bool ApplyBurn;
    public bool ApplyPoison;
    public bool ApplyAntiHeal;
    public bool GoblinGoldSteal;
    public bool GnomGoldReward;
    public bool GolemShieldBuff;
}
