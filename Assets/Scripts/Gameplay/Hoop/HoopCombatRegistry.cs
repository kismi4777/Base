using UnityEngine;

/// <summary>Кэширует кольца и щиты по команде для способностей (Orc, Golem и т.д.).</summary>
public sealed class HoopCombatRegistry : MonoBehaviour
{
    public static HoopCombatRegistry Instance { get; private set; }

    [SerializeField] HoopHealth playerHoop;
    [SerializeField] HoopHealth botHoop;
    [SerializeField] ShieldHealth playerShield;
    [SerializeField] ShieldHealth botShield;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoResolveIfNeeded();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public HoopHealth GetHoop(PvPTeam team)
    {
        AutoResolveIfNeeded();
        return team == PvPTeam.Player ? playerHoop : botHoop;
    }

    public ShieldHealth GetShield(PvPTeam team)
    {
        AutoResolveIfNeeded();

        ShieldHealth shield = team == PvPTeam.Player ? playerShield : botShield;
        if (shield != null)
            return shield;

        HoopHealth hoop = GetHoop(team);
        if (hoop == null)
            return null;

        shield = hoop.GetComponentInChildren<ShieldHealth>(true);
        if (team == PvPTeam.Player)
            playerShield = shield;
        else if (team == PvPTeam.Bot)
            botShield = shield;

        return shield;
    }

    public HoopStatusEffects GetStatusEffects(PvPTeam team)
    {
        HoopHealth hoop = GetHoop(team);
        return hoop != null ? hoop.GetComponent<HoopStatusEffects>() : null;
    }

    void AutoResolveIfNeeded()
    {
        if (playerHoop != null && botHoop != null && playerShield != null && botShield != null)
            return;

        HoopHealth[] hoops = FindObjectsByType<HoopHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < hoops.Length; i++)
        {
            HoopHealth hoop = hoops[i];
            if (hoop == null || !hoop.UsesPvPTeamFilter)
                continue;

            if (hoop.DefendedTeam == PvPTeam.Player)
                playerHoop = hoop;
            else if (hoop.DefendedTeam == PvPTeam.Bot)
                botHoop = hoop;
        }

        ShieldHealth[] shields = FindObjectsByType<ShieldHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < shields.Length; i++)
        {
            ShieldHealth shield = shields[i];
            if (shield == null)
                continue;

            if (shield.Team == PvPTeam.Player)
                playerShield = shield;
            else if (shield.Team == PvPTeam.Bot)
                botShield = shield;
        }
    }
}
