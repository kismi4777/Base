using UnityEngine;

/// <summary>
/// Серии попаданий и точных бросков за матч (не сбрасываются при респавне мяча из пула).
/// </summary>
public sealed class BallAbilityMatchState : MonoBehaviour
{
    public static BallAbilityMatchState Instance { get; private set; }

    readonly int[] _consecutiveScores = { 0, 0 };
    readonly int[] _accurateThrows = { 0, 0 };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static BallAbilityMatchState EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var host = new GameObject(nameof(BallAbilityMatchState));
        return host.AddComponent<BallAbilityMatchState>();
    }

    public int GetConsecutiveScores(PvPTeam team) => _consecutiveScores[TeamIndex(team)];

    public int GetAccurateThrows(PvPTeam team) => _accurateThrows[TeamIndex(team)];

    public void RegisterScore(PvPTeam team)
    {
        int index = TeamIndex(team);
        _consecutiveScores[index]++;
        _accurateThrows[index]++;
    }

    public void RegisterMiss(PvPTeam team) => _consecutiveScores[TeamIndex(team)] = 0;

    public void ResetMatch()
    {
        _consecutiveScores[0] = 0;
        _consecutiveScores[1] = 0;
        _accurateThrows[0] = 0;
        _accurateThrows[1] = 0;
    }

    static int TeamIndex(PvPTeam team) => team == PvPTeam.Bot ? 1 : 0;
}
