using System;
using UnityEngine;

/// <summary>Блокирует начало PvP-матча, пока игрок не выберет скин.</summary>
public sealed class MatchStartGate : MonoBehaviour
{
    public static MatchStartGate Instance { get; private set; }

    public event Action MatchStarted;

    [SerializeField] PvPBattleOrchestrator battleOrchestrator;
    [SerializeField] bool requireSkinSelection = true;

    public bool IsMatchStarted { get; private set; }
    public bool RequiresSkinSelection => requireSkinSelection;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (battleOrchestrator == null)
            battleOrchestrator = FindFirstObjectByType<PvPBattleOrchestrator>();

        if (requireSkinSelection && battleOrchestrator != null)
            battleOrchestrator.enabled = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool ShouldBlockMatchStart() => requireSkinSelection && !IsMatchStarted;

    public void StartMatch()
    {
        if (IsMatchStarted)
            return;

        IsMatchStarted = true;
        BallAbilityMatchState.EnsureExists().ResetMatch();

        if (battleOrchestrator != null)
        {
            battleOrchestrator.enabled = true;
            battleOrchestrator.InitializeMatch();
        }

        MatchStarted?.Invoke();
    }
}
