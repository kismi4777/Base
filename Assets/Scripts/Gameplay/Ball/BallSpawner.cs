using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет появлением мячей: один активный для броска, пул для повторного использования.
/// Новый мяч появляется через заданную задержку после броска предыдущего.
/// </summary>
public class BallSpawner : MonoBehaviour
{
    [SerializeField] SlingshotShooter ballPrefab;
    [Tooltip("Стартовый мяч игрока на сцене. Если задан, спавнер использует только его как исходную точку.")]
    [SerializeField] SlingshotShooter scenePlayerBall;
    [SerializeField] float respawnDelay = 1f;
    [Tooltip("Сколько мячей подготовить в пуле заранее")]
    [Min(1)] [SerializeField] int prewarmCount = 2;

    readonly List<SlingshotShooter> _pool = new();
    Vector3 _spawnPosition;
    Quaternion _spawnRotation;
    Transform _spawnParent;
    SlingshotShooter _throwableBall;
    Coroutine _respawnRoutine;

    /// <summary>
    /// Текущий мяч, готовый к броску (после RegisterBall), либо null во время полёта.
    /// </summary>
    public SlingshotShooter CurrentThrowableBall => _throwableBall;

    /// <summary>
    /// Вызывается, когда активный мяч снова готов к броску (после респавна или старта сцены).
    /// </summary>
    public event Action<SlingshotShooter> ThrowableBallReady;

    public static BallSpawner Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        CacheSpawnPoseFromSceneBall();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (_throwableBall != null)
        {
            RegisterBall(_throwableBall);
            return;
        }

        if (ballPrefab == null)
        {
            Debug.LogError("BallSpawner: не назначен префаб мяча.", this);
            return;
        }

        SpawnThrowableBall();
    }

    void CacheSpawnPoseFromSceneBall()
    {
        SlingshotShooter sceneBall = ResolveScenePlayerBall();
        if (sceneBall == null)
            return;

        Transform t = sceneBall.transform;
        _spawnPosition = t.position;
        _spawnRotation = t.rotation;
        _spawnParent = t.parent;
        _throwableBall = sceneBall;
    }

    SlingshotShooter ResolveScenePlayerBall()
    {
        if (scenePlayerBall != null)
            return scenePlayerBall;

        SlingshotShooter[] shooters = FindObjectsByType<SlingshotShooter>(FindObjectsSortMode.None);
        for (int i = 0; i < shooters.Length; i++)
        {
            SlingshotShooter shooter = shooters[i];
            if (shooter == null)
                continue;

            if (shooter.gameObject.name.Contains("Bot"))
                continue;

            return shooter;
        }

        return FindFirstObjectByType<SlingshotShooter>();
    }

    public void RegisterBall(SlingshotShooter ball)
    {
        if (ball == null)
            return;

        Unsubscribe(ball);
        Subscribe(ball);
        _throwableBall = ball;
        ball.PrepareForThrow();

        if (ball.TryGetComponent(out BallFloorDespawn floorDespawn))
            floorDespawn.ResetForReuse();

        ThrowableBallReady?.Invoke(ball);
    }

    public void OnBallThrown(SlingshotShooter ball)
    {
        if (ball != _throwableBall)
            return;

        _throwableBall = null;

        if (_respawnRoutine != null)
            StopCoroutine(_respawnRoutine);

        _respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    public void ReleaseBall(SlingshotShooter ball)
    {
        if (ball == null)
            return;

        Unsubscribe(ball);

        if (ball == _throwableBall)
            _throwableBall = null;

        ball.CleanupBeforeDespawn();
        ball.gameObject.SetActive(false);

        if (!_pool.Contains(ball))
            _pool.Add(ball);
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        _respawnRoutine = null;
        SpawnThrowableBall();
    }

    void SpawnThrowableBall()
    {
        SlingshotShooter ball = TakeFromPool();
        if (ball == null)
            ball = CreateBallInstance();

        Transform t = ball.transform;
        t.SetParent(_spawnParent, true);
        t.SetPositionAndRotation(_spawnPosition, _spawnRotation);

        ball.gameObject.SetActive(true);
        RegisterBall(ball);
    }

    SlingshotShooter TakeFromPool()
    {
        while (_pool.Count > 0)
        {
            int last = _pool.Count - 1;
            SlingshotShooter candidate = _pool[last];
            _pool.RemoveAt(last);

            if (candidate != null)
                return candidate;
        }

        return null;
    }

    SlingshotShooter CreateBallInstance()
    {
        SlingshotShooter instance = Instantiate(ballPrefab, _spawnPosition, _spawnRotation, _spawnParent);
        PrewarmPoolIfNeeded();
        return instance;
    }

    void PrewarmPoolIfNeeded()
    {
        if (ballPrefab == null)
            return;

        int targetPoolSize = Mathf.Max(0, prewarmCount - 1);
        while (_pool.Count < targetPoolSize)
        {
            SlingshotShooter extra = Instantiate(ballPrefab, _spawnPosition, _spawnRotation, _spawnParent);
            extra.gameObject.SetActive(false);
            _pool.Add(extra);
        }
    }

    void Subscribe(SlingshotShooter ball) => ball.OnThrown += HandleBallThrown;

    void Unsubscribe(SlingshotShooter ball) => ball.OnThrown -= HandleBallThrown;

    void HandleBallThrown(SlingshotShooter ball) => OnBallThrown(ball);
}
