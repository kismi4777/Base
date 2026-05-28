using System;
using UnityEngine;

/// <summary>Золото в рамках PvP-матча (отдельно от сохранения PlayerData.Coins).</summary>
public sealed class BattleGoldWallet : MonoBehaviour
{
    public static BattleGoldWallet Instance { get; private set; }

    public event Action<PvPTeam, int> GoldChanged;

    [SerializeField] int playerStartGold = 20;
    [SerializeField] int botStartGold = 20;

    int _playerGold;
    int _botGold;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _playerGold = playerStartGold;
        _botGold = botStartGold;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetGold(PvPTeam team) =>
        team == PvPTeam.Player ? _playerGold : _botGold;

    public void AddGold(PvPTeam team, int amount)
    {
        if (amount <= 0)
            return;

        if (team == PvPTeam.Player)
            _playerGold += amount;
        else
            _botGold += amount;

        GoldChanged?.Invoke(team, GetGold(team));
    }

    public void StealGold(PvPTeam thief, int amount)
    {
        if (amount <= 0)
            return;

        PvPTeam victim = thief == PvPTeam.Player ? PvPTeam.Bot : PvPTeam.Player;
        int victimGold = GetGold(victim);
        int stolen = Mathf.Min(amount, victimGold);

        if (stolen <= 0)
            return;

        if (victim == PvPTeam.Player)
            _playerGold -= stolen;
        else
            _botGold -= stolen;

        AddGold(thief, stolen);
    }
}
