using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Здоровье кольца и полоска здоровья над ним.
/// Урон наносится только когда мяч проходит через плоскость кольца (залетает внутрь).
/// </summary>
public class HoopHealth : MonoBehaviour
{
    public event Action<HoopHealth> Scored;
    public event Action<HoopHealth> HealthDepleted;

    [Header("Связи")]
    [SerializeField] Rigidbody targetBall;

    [Header("PvP: чьё кольцо")]
    [Tooltip("Отключено — гол засчитывается от любого мяча (как раньше). Иначе урон только если бросил соперник.")]
    [SerializeField] bool usePvPTeamFilter;
    [SerializeField] PvPTeam defendedTeam = PvPTeam.Player;
    public bool UsesPvPTeamFilter => usePvPTeamFilter;
    public PvPTeam DefendedTeam => defendedTeam;

    [Header("Параметры здоровья")]
    [SerializeField] int maxHealth = 5;
    [SerializeField] int damagePerScore = 1;

    [Header("Засчёт гола")]
    [SerializeField] float retriggerCooldown = 0.15f;

    [Header("Поведение при обнулении")]
    [Tooltip("Уничтожить объект кольца при достижении 0 здоровья.")]
    [SerializeField] bool destroyOnZeroHealth;
    [Tooltip("Если не уничтожаем объект, можно отключить эти объекты при 0 HP.")]
    [SerializeField] GameObject[] objectsToDisableOnZeroHealth;

    [Header("Полоска здоровья")]
    [SerializeField] Transform healthBarRoot;
    [Tooltip("Зелёная полоска — уменьшается сразу при уроне.")]
    [SerializeField] Image fillImmediate;
    [Tooltip("Скорость уменьшения зелёной полоски (единиц fillAmount в секунду).")]
    [SerializeField] float immediateFillCatchUpSpeed = 1.6f;
    [Tooltip("Жёлтая полоска — догоняет с задержкой.")]
    [SerializeField] Image fillDelayed;
    [Tooltip("Пауза перед тем, как жёлтая полоска начнёт уменьшаться.")]
    [SerializeField] float delayedFillStartDelay = 0.55f;
    [Tooltip("Скорость уменьшения жёлтой полоски (единиц fillAmount в секунду).")]
    [SerializeField] float delayedFillCatchUpSpeed = 0.35f;

    int _currentHealth;
    float _immediateTarget01 = 1f;
    float _immediateFill01 = 1f;
    float _delayedTarget01 = 1f;
    float _delayedFill01 = 1f;
    float _delayedCatchUpTimer;
    readonly HashSet<int> _enteredBallIds = new();

    float _lastScoreTime = -10f;
    bool _relocateBusy;

    public bool IsRelocateBusy => _relocateBusy;

    void Awake()
    {
        _currentHealth = Mathf.Max(1, maxHealth);
        _immediateTarget01 = 1f;
        _immediateFill01 = 1f;
        _delayedTarget01 = 1f;
        _delayedFill01 = 1f;
        _delayedCatchUpTimer = 0f;

        if (healthBarRoot != null)
            healthBarRoot.gameObject.SetActive(true);

        BindBallIfNeeded(); 
        UpdateBarInstant();
    }

    void Update()
    {
        UpdateImmediateFill();
        UpdateDelayedFill();
    }

    public void OnBallLaunched(Rigidbody ball)
    {
        targetBall = ball;
        _enteredBallIds.Clear();
    }

    public void ResetShot()
    {
        _enteredBallIds.Clear();
    }

    public void RegisterEntry(Rigidbody ball)
    {
        if (!IsValidBall(ball))
            return;

        _enteredBallIds.Add(ball.GetInstanceID());
    }

    public void RegisterExit(Rigidbody ball)
    {
        if (!IsValidBall(ball))
            return;

        int ballId = ball.GetInstanceID();
        if (!_enteredBallIds.Remove(ballId))
            return;

        if (_currentHealth <= 0 || _relocateBusy)
            return;

        if (Time.time - _lastScoreTime < retriggerCooldown)
            return;

        if (usePvPTeamFilter && !IsOpponentBall(ball))
            return;

        ApplyDamage();
        _lastScoreTime = Time.time;
    }

    void ApplyDamage()
    {
        _currentHealth = Mathf.Clamp(_currentHealth - damagePerScore, 0, maxHealth);
        UpdateBarOnDamage();
        Scored?.Invoke(this);

        if (_currentHealth <= 0)
            OnHealthDepleted();
    }

    /// <summary>
    /// Сбрасывает только состояние гола для следующего броска. Здоровье не меняется.
    /// </summary>
    public void ClearShotTrackingForRelocate()
    {
        _enteredBallIds.Clear();
    }

    public void SetRelocateBusy(bool busy)
    {
        _relocateBusy = busy;
    }

    /// <summary>
    /// Публичная настройка PvP-фильтра для спавнеров/оркестратора.
    /// </summary>
    public void ConfigurePvPTeamFilter(bool enabled, PvPTeam team)
    {
        usePvPTeamFilter = enabled;
        defendedTeam = team;
    }

    /// <summary>
    /// Ждёт, пока зелёная полоска HP догонит значение после урона.
    /// </summary>
    public IEnumerator WaitForHealthBarAfterDamage()
    {
        while (!Mathf.Approximately(_immediateFill01, _immediateTarget01))
            yield return null;
    }

    void UpdateBarOnDamage()
    {
        float target01 = GetHealth01();
        _immediateTarget01 = target01;
        _delayedTarget01 = target01;
        _delayedCatchUpTimer = delayedFillStartDelay;
    }

    void UpdateImmediateFill()
    {
        if (fillImmediate == null)
            return;

        if (Mathf.Approximately(_immediateFill01, _immediateTarget01))
            return;

        _immediateFill01 = Mathf.MoveTowards(
            _immediateFill01,
            _immediateTarget01,
            immediateFillCatchUpSpeed * Time.deltaTime
        );
        fillImmediate.fillAmount = _immediateFill01;
    }

    void UpdateDelayedFill()
    {
        if (fillDelayed == null)
            return;

        if (_delayedCatchUpTimer > 0f)
        {
            _delayedCatchUpTimer -= Time.deltaTime;
            return;
        }

        if (Mathf.Approximately(_delayedFill01, _delayedTarget01))
            return;

        _delayedFill01 = Mathf.MoveTowards(
            _delayedFill01,
            _delayedTarget01,
            delayedFillCatchUpSpeed * Time.deltaTime
        );
        fillDelayed.fillAmount = _delayedFill01;
    }

    void OnHealthDepleted()
    {
        HealthDepleted?.Invoke(this);
        UpdateBarInstant();

        if (destroyOnZeroHealth)
        {
            Destroy(gameObject);
            return;
        }

        if (objectsToDisableOnZeroHealth == null)
            return;

        for (int i = 0; i < objectsToDisableOnZeroHealth.Length; i++)
        {
            GameObject go = objectsToDisableOnZeroHealth[i];
            if (go != null)
                go.SetActive(false);
        }
    }

    void UpdateBarInstant()
    {
        float health01 = GetHealth01();
        _immediateTarget01 = health01;
        _immediateFill01 = health01;
        _delayedTarget01 = health01;
        _delayedFill01 = health01;
        _delayedCatchUpTimer = 0f;

        if (fillImmediate != null)
            fillImmediate.fillAmount = health01;

        if (fillDelayed != null)
            fillDelayed.fillAmount = health01;
    }

    float GetHealth01()
    {
        if (maxHealth <= 0)
            return 0f;

        return Mathf.Clamp01((float)_currentHealth / maxHealth);
    }

    void BindBallIfNeeded()
    {
        if (targetBall != null)
            return;

        SlingshotShooter shooter = FindFirstObjectByType<SlingshotShooter>();
        if (shooter != null && shooter.TryGetComponent(out Rigidbody rb))
            targetBall = rb;
    }

    bool IsValidBall(Rigidbody ball)
    {
        if (ball == null)
            return false;

        // В PvP на сцене два мяча (игрок + бот), поэтому не ограничиваемся targetBall.
        if (usePvPTeamFilter)
            return ball.TryGetComponent(out BallBouncePhysics _);

        if (targetBall != null)
            return ball == targetBall;

        return ball.TryGetComponent(out BallBouncePhysics _);
    }

    /// <summary>
    /// Урон по кольцу только если мяч бросил соперник защищающейся стороны.
    /// </summary>
    bool IsOpponentBall(Rigidbody ball)
    {
        if (ball == null)
            return false;

        if (!ball.TryGetComponent(out BallThrowOwnership ownership))
            return false;

        return ownership.LastThrower != defendedTeam;
    }
}
