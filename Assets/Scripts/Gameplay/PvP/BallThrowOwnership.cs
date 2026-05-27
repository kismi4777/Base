using UnityEngine;

/// <summary>
/// Последний бросок мяча (игрок или бот). Нужен, чтобы гол засчитывался только в чужое кольцо.
/// </summary>
[DisallowMultipleComponent]
public class BallThrowOwnership : MonoBehaviour
{
    [SerializeField] PvPTeam lastThrower = PvPTeam.Player;

    public PvPTeam LastThrower => lastThrower;

    public void SetLastThrower(PvPTeam team) => lastThrower = team;
}
