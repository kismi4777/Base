using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Здоровье щита у кольца (отдельно от HP самого кольца).</summary>
public sealed class ShieldHealth : MonoBehaviour
{
    public event Action<ShieldHealth, int> Damaged;
    public event Action<ShieldHealth, int> Healed;
    public event Action<ShieldHealth> Depleted;

    [Header("Команда")]
    [SerializeField] PvPTeam team = PvPTeam.Player;
    public PvPTeam Team => team;

    [Header("Параметры")]
    [SerializeField] int maxHealth = 5;
    [Min(0)] public int bonusMaxHealth;

    [Header("UI (опционально)")]
    [SerializeField] Image fillImmediate;

    int _currentHealth;

    public int MaxHealth => Mathf.Max(1, maxHealth + bonusMaxHealth);
    public int CurrentHealth => _currentHealth;
    public float Health01 => MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)_currentHealth / MaxHealth);

    void Awake()
    {
        _currentHealth = MaxHealth;
        RefreshBarInstant();
    }

    public void ConfigureTeam(PvPTeam value) => team = value;

    /// <returns>Фактически нанесённый урон.</returns>
    public int TakeDamage(int amount, bool ignoreDefense = false)
    {
        if (amount <= 0 || _currentHealth <= 0)
            return 0;

        int applied = Mathf.Min(amount, _currentHealth);
        _currentHealth -= applied;
        RefreshBarInstant();
        Damaged?.Invoke(this, applied);

        if (_currentHealth <= 0)
            Depleted?.Invoke(this);

        return applied;
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        int before = _currentHealth;
        _currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
        int healed = _currentHealth - before;
        if (healed <= 0)
            return;

        RefreshBarInstant();
        Healed?.Invoke(this, healed);
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0)
            return;

        bonusMaxHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
        RefreshBarInstant();
    }

    public bool IsHealingBlocked(HoopStatusEffects statusEffects) =>
        statusEffects != null && statusEffects.HasAntiHeal;

    void RefreshBarInstant()
    {
        if (fillImmediate == null)
            return;

        fillImmediate.fillAmount = Health01;
    }
}
