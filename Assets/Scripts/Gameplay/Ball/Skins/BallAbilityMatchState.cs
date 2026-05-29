using System;
using UnityEngine;

/// <summary>
/// Серии попаданий и точных бросков за матч (не сбрасываются при респавне мяча из пула).
/// </summary>
public sealed class BallAbilityMatchState : MonoBehaviour
{
    public static BallAbilityMatchState Instance { get; private set; }

    public event Action StateChanged;

    readonly int[] _consecutiveScores = { 0, 0 };
    readonly int[] _accurateThrows = { 0, 0 };
    readonly bool[] _fireCharged = { false, false };
    readonly bool[] _golemCharged = { false, false };

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

    public bool IsFireCharged(PvPTeam team) => _fireCharged[TeamIndex(team)];

    public bool IsGolemCharged(PvPTeam team) => _golemCharged[TeamIndex(team)];

    public void RegisterScore(PvPTeam team)
    {
        int index = TeamIndex(team);
        _consecutiveScores[index]++;
        _accurateThrows[index]++;
        NotifyChanged();
    }

    public void RegisterMiss(PvPTeam team)
    {
        int index = TeamIndex(team);
        _consecutiveScores[index] = 0;
        _fireCharged[index] = false;
        NotifyChanged();
    }

    public void SetFireCharged(PvPTeam team, bool charged)
    {
        int index = TeamIndex(team);
        if (_fireCharged[index] == charged)
            return;

        _fireCharged[index] = charged;
        NotifyChanged();
    }

    public void SetGolemCharged(PvPTeam team, bool charged)
    {
        int index = TeamIndex(team);
        if (_golemCharged[index] == charged)
            return;

        _golemCharged[index] = charged;
        NotifyChanged();
    }

    public void ResetConsecutiveScores(PvPTeam team)
    {
        int index = TeamIndex(team);
        if (_consecutiveScores[index] == 0)
            return;

        _consecutiveScores[index] = 0;
        NotifyChanged();
    }

    public void ResetMatch()
    {
        _consecutiveScores[0] = 0;
        _consecutiveScores[1] = 0;
        _accurateThrows[0] = 0;
        _accurateThrows[1] = 0;
        _fireCharged[0] = false;
        _fireCharged[1] = false;
        _golemCharged[0] = false;
        _golemCharged[1] = false;
        NotifyChanged();
    }

    void NotifyChanged() => StateChanged?.Invoke();

    static int TeamIndex(PvPTeam team) => team == PvPTeam.Bot ? 1 : 0;
}
