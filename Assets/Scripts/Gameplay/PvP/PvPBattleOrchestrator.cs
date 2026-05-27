using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Режим игрок vs бот: по очереди бросают один мяч, победа при обнулении HP чужого кольца.
/// Точность бота — вероятность «идеального» прицела; при промахе направление сдвигается по горизонтали.
/// </summary>
public sealed class PvPBattleOrchestrator : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] BallSpawner ballSpawner;
    [SerializeField] HoopHealth playerDefendedHoop;
    [SerializeField] HoopHealth botDefendedHoop;
    [SerializeField] bool autoResolveHoopsByTeam = true;
    [Tooltip("Отдельный мяч бота (Ball_Bot) с компонентом SlingshotShooter.")]
    [SerializeField] SlingshotShooter botShooter;
    [Tooltip("Куда бот целится (центр кольца игрока). Если пусто — позиция playerDefendedHoop.")]
    [SerializeField] Transform botAimTarget;

    [Header("Бот")]
    [Tooltip("Вероятность 0–100: бросок по направлению на кольцо игрока; иначе — промах в сторону.")]
    [Range(0f, 100f)]
    [SerializeField] float botAccuracyPercent = 55f;
    [SerializeField] float botAimDelaySeconds = 0.45f;
    [SerializeField] float normalizedPullMin = 0.72f;
    [SerializeField] float normalizedPullMax = 0.95f;
    [SerializeField] float missYawDegreesMin = 18f;
    [SerializeField] float missYawDegreesMax = 42f;
    [SerializeField] float playerUnlockDelayAfterBotThrow = 1.15f;

    [Header("События")]
    [SerializeField] UnityEvent onPlayerWon;
    [SerializeField] UnityEvent onBotWon;

    BallSpawner _spawner;
    Coroutine _botThrowRoutine;
    bool _nextTurnIsBot;
    bool _matchEnded;
    SlingshotShooter _ballSubscribedForThrown;

    void Awake()
    {
        _nextTurnIsBot = false;
    }

    void Start()
    {
        _spawner = ballSpawner != null ? ballSpawner : BallSpawner.Instance;
        if (_spawner == null)
        {
            Debug.LogError("PvPBattleOrchestrator: не найден BallSpawner.", this);
            enabled = false;
            return;
        }

        ResolveBotShooterIfNeeded();
        _spawner.ThrowableBallReady += HandleThrowableBallReady;
        if (!TryResolveHoopsByTeamIfNeeded())
            StartCoroutine(WaitForHoopsRoutine());

        SubscribeToHoops();

        if (_spawner.CurrentThrowableBall != null)
            HandleThrowableBallReady(_spawner.CurrentThrowableBall);
    }

    void Update()
    {
        // Бот-мяч не должен принимать пользовательский ввод.
        if (botShooter != null)
            botShooter.IsInputLocked = true;
    }

    void OnDestroy()
    {
        if (_spawner != null)
            _spawner.ThrowableBallReady -= HandleThrowableBallReady;

        if (playerDefendedHoop != null)
            playerDefendedHoop.HealthDepleted -= HandleHoopDepleted;

        if (botDefendedHoop != null)
            botDefendedHoop.HealthDepleted -= HandleHoopDepleted;

        UnsubscribeThrown();
    }

    IEnumerator WaitForHoopsRoutine()
    {
        while (!_matchEnded)
        {
            if (TryResolveHoopsByTeamIfNeeded())
            {
                SubscribeToHoops();
                yield break;
            }

            yield return null;
        }
    }

    void SubscribeToHoops()
    {
        if (playerDefendedHoop != null)
        {
            playerDefendedHoop.HealthDepleted -= HandleHoopDepleted;
            playerDefendedHoop.HealthDepleted += HandleHoopDepleted;
        }

        if (botDefendedHoop != null)
        {
            botDefendedHoop.HealthDepleted -= HandleHoopDepleted;
            botDefendedHoop.HealthDepleted += HandleHoopDepleted;
        }
    }

    bool TryResolveHoopsByTeamIfNeeded()
    {
        if (!autoResolveHoopsByTeam)
            return playerDefendedHoop != null && botDefendedHoop != null;

        if (playerDefendedHoop != null && botDefendedHoop != null)
            return true;

        HoopHealth[] hoops = FindObjectsByType<HoopHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < hoops.Length; i++)
        {
            HoopHealth hoop = hoops[i];
            if (hoop == null || !hoop.UsesPvPTeamFilter)
                continue;

            if (hoop.DefendedTeam == PvPTeam.Player)
                playerDefendedHoop = hoop;
            else if (hoop.DefendedTeam == PvPTeam.Bot)
                botDefendedHoop = hoop;
        }

        return playerDefendedHoop != null && botDefendedHoop != null;
    }

    void UnsubscribeThrown()
    {
        if (_ballSubscribedForThrown == null)
            return;

        _ballSubscribedForThrown.OnThrown -= HandleBallThrown;
        _ballSubscribedForThrown = null;
    }

    void HandleHoopDepleted(HoopHealth hoop)
    {
        if (_matchEnded)
            return;

        _matchEnded = true;

        if (hoop == playerDefendedHoop)
            onBotWon?.Invoke();
        else if (hoop == botDefendedHoop)
            onPlayerWon?.Invoke();

        StopBotRoutineIfAny();
        LockCurrentBallInput();
    }
    void HandleThrowableBallReady(SlingshotShooter ball)
    {
        UnsubscribeThrown();
        if (ball == null)
            return;

        _ballSubscribedForThrown = ball;
        ball.OnThrown += HandleBallThrown;

        StopBotRoutineIfAny();

        if (_matchEnded)
        {
            ball.IsInputLocked = true;
            return;
        }

        if (_nextTurnIsBot)
        {
            ball.IsInputLocked = true;
            if (_botThrowRoutine == null)
                _botThrowRoutine = StartCoroutine(BotThrowRoutine(ball));
        }
        else
        {
            ball.IsInputLocked = false;
        }
    }

    void HandleBallThrown(SlingshotShooter _)
    {
        _nextTurnIsBot = !_nextTurnIsBot;

        if (!_nextTurnIsBot || _matchEnded)
            return;

        StopBotRoutineIfAny();
        SlingshotShooter playerBall = _spawner != null ? _spawner.CurrentThrowableBall : null;
        if (playerBall != null)
            playerBall.IsInputLocked = true;

        _botThrowRoutine = StartCoroutine(BotThrowRoutine(playerBall));
    }

    IEnumerator BotThrowRoutine(SlingshotShooter ball)
    {
        if (botAimDelaySeconds > 0f)
            yield return new WaitForSeconds(botAimDelaySeconds);

        if (_matchEnded)
            yield break;

        SlingshotShooter shooter = ResolveBotShooterIfNeeded();
        if (shooter == null)
        {
            // Фолбэк: если отдельный мяч бота не задан, используем текущий мяч (старое поведение).
            shooter = ball;
        }

        shooter.IsInputLocked = true;
        shooter.PrepareForThrow();

        Vector3 hoopPos = botAimTarget != null ? botAimTarget.position : playerDefendedHoop.transform.position;
        Vector3 from = shooter.transform.position;
        Vector3 planar = hoopPos - from;
        planar.y = 0f;

        bool rollHit = UnityEngine.Random.Range(0f, 100f) < botAccuracyPercent;
        if (!rollHit)
        {
            float yawDeg = UnityEngine.Random.Range(missYawDegreesMin, missYawDegreesMax);
            if (UnityEngine.Random.value < 0.5f)
                yawDeg = -yawDeg;

            planar = Quaternion.AngleAxis(yawDeg, Vector3.up) * planar;
        }

        float pull = UnityEngine.Random.Range(normalizedPullMin, normalizedPullMax);
        Vector3 force = shooter.ComputeThrowForceForPlanarDirection(planar, pull);
        bool launched = shooter.TryLaunchScripted(force, PvPTeam.Bot);

        if (launched)
            StartCoroutine(UnlockPlayerBallAfterDelayRoutine());
        else if (ball != null)
            ball.IsInputLocked = false;

        _nextTurnIsBot = false;
        _botThrowRoutine = null;
    }

    void StopBotRoutineIfAny()
    {
        if (_botThrowRoutine == null)
            return;

        StopCoroutine(_botThrowRoutine);
        _botThrowRoutine = null;
    }

    void LockCurrentBallInput()
    {
        if (_spawner == null || _spawner.CurrentThrowableBall == null)
            return;

        _spawner.CurrentThrowableBall.IsInputLocked = true;
    }

    IEnumerator UnlockPlayerBallAfterDelayRoutine()
    {
        if (playerUnlockDelayAfterBotThrow > 0f)
            yield return new WaitForSeconds(playerUnlockDelayAfterBotThrow);

        if (_matchEnded || _spawner == null || _spawner.CurrentThrowableBall == null)
            yield break;

        _spawner.CurrentThrowableBall.IsInputLocked = false;
    }

    SlingshotShooter ResolveBotShooterIfNeeded()
    {
        if (botShooter != null)
        {
            botShooter.IsInputLocked = true;
            return botShooter;
        }

        SlingshotShooter[] shooters = FindObjectsByType<SlingshotShooter>(FindObjectsSortMode.None);
        for (int i = 0; i < shooters.Length; i++)
        {
            SlingshotShooter candidate = shooters[i];
            if (candidate == null)
                continue;

            if (candidate.gameObject.name.Contains("Bot"))
            {
                botShooter = candidate;
                botShooter.IsInputLocked = true;
                return botShooter;
            }
        }

        return null;
    }
}
