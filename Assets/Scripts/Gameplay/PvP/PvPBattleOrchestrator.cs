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
    [Header("Точка спавна мяча бота (строго мировые координаты)")]
    [Tooltip("Координаты с объекта Ball_Bot в сцене 2_Gameplay. Перед каждым броском бота мяч ставится только сюда.")]
    [SerializeField] Vector3 botBallSpawnWorld = new Vector3(-0.69f, 2.13f, -1.953f);
    [SerializeField] Quaternion botBallSpawnWorldRotation = Quaternion.identity;
    [Tooltip("Ручная цель бота (опционально). Если включено «приоритет Aim», объект Aim в кольце игрока перекрывает это поле.")]
    [SerializeField] Transform botAimTarget;
    [Tooltip("Если в кольце игрока есть дочерний объект с именем Aim — бот всегда целится в него (даже если в Bot Aim Target назначен Ring_collider и т.п.).")]
    [SerializeField] bool preferAimChildInPlayerHoop = true;
    [Tooltip("Если botAimTarget не задан и Aim не найден, бот ищет эту точку от корня кольца игрока (путь через Transform.Find).")]
    [SerializeField] string fallbackAimTargetPathInHoop = "BUSKET2/Aim";

    [Header("Бот")]
    [Tooltip("Вероятность 0–100: бросок по направлению на кольцо игрока; иначе — промах в сторону.")]
    [Range(0f, 100f)]
    [SerializeField] float botAccuracyPercent = 55f;
    [Tooltip("Мин. секунд после возврата мяча бота на точку броска до следующего броска.")]
    [SerializeField] float botReactionDelayMinSeconds = 1f;
    [Tooltip("Макс. секунд после возврата мяча бота на точку броска (вместе с минимумом задаёт окно).")]
    [SerializeField] float botReactionDelayMaxSeconds = 1.8f;
    [SerializeField] float normalizedPullMin = 0.72f;
    [SerializeField] float normalizedPullMax = 0.95f;
    [Tooltip("Горизонтальный промах в метрах от точки Aim (влево/вправо по локальной оси кольца).")]
    [SerializeField] float missLateralOffsetMin = 0.18f;
    [SerializeField] float missLateralOffsetMax = 0.45f;
    [Tooltip("Продольный промах в метрах от точки Aim (вперед/назад вдоль оси кольца).")]
    [SerializeField] float missForwardOffsetMin = 0.05f;
    [SerializeField] float missForwardOffsetMax = 0.22f;
    [Tooltip("Вертикальный промах в метрах от точки Aim.")]
    [SerializeField] float missVerticalOffsetMin = -0.06f;
    [SerializeField] float missVerticalOffsetMax = 0.12f;
    [Tooltip("Смещение точки прицеливания бота по Y относительно центра объекта кольца игрока.")]
    [SerializeField] float perfectAimTargetYOffset = -0.15f;
    [Tooltip("Высота пика траектории над центром кольца игрока для идеального броска.")]
    [SerializeField] float perfectArcApexHeight = 3.4f;
    [Tooltip("Доп. высота пика на каждый метр горизонтальной дистанции до цели (перелёт передней дужки/щитка).")]
    [SerializeField] float perfectApexExtraPerMeter = 0.55f;
    [Tooltip("Доп. смещение точки прицеливания вдоль направления кольца игрока.")]
    [SerializeField] float perfectAimForwardOffset = 0.08f;
    [Tooltip("Ограничение вертикальной скорости для страховки (увеличьте, если дуга кажется плоской).")]
    [SerializeField] float perfectMaxUpVelocity = 22f;

    [Header("События")]
    [SerializeField] UnityEvent onPlayerWon;
    [SerializeField] UnityEvent onBotWon;

    BallSpawner _spawner;
    Coroutine _botThrowRoutine;
    bool _matchEnded;
    SlingshotShooter _ballSubscribedForThrown;

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
        SnapBotBallToFixedSpawn();
        ScheduleBotThrowAfterReactionDelay();
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

        if (_matchEnded)
        {
            ball.IsInputLocked = true;
            return;
        }

        // Игрок всегда может бросать сразу после появления его мяча.
        ball.IsInputLocked = false;
    }

    void HandleBallThrown(SlingshotShooter thrownBall)
    {
        // Событие только для мяча игрока: бот бросает по своему циклу, не после хода игрока.
        if (_matchEnded || thrownBall == null)
            return;

        if (botShooter != null && ReferenceEquals(thrownBall, botShooter))
            return;
    }

    void ScheduleBotThrowAfterBallReturned()
    {
        if (_matchEnded)
            return;

        if (_botThrowRoutine != null)
            return;

        _botThrowRoutine = StartCoroutine(WaitForBotBallReturnThenScheduleThrow());
    }

    void ScheduleBotThrowAfterReactionDelay()
    {
        if (_matchEnded)
            return;

        StopBotRoutineIfAny();
        _botThrowRoutine = StartCoroutine(BotThrowAfterDelayRoutine());
    }

    IEnumerator WaitForBotBallReturnThenScheduleThrow()
    {
        SlingshotShooter shooter = ResolveBotShooterIfNeeded();
        if (shooter == null)
        {
            _botThrowRoutine = null;
            yield break;
        }

        if (!IsBotBallInFlight(shooter))
        {
            _botThrowRoutine = null;
            if (!_matchEnded)
                ScheduleBotThrowAfterReactionDelay();
            yield break;
        }

        bool returned = false;
        void OnReturned(SlingshotShooter ball)
        {
            if (ReferenceEquals(ball, shooter))
                returned = true;
        }

        shooter.OnReturnedToThrowPosition += OnReturned;
        while (!returned && !_matchEnded)
            yield return null;

        shooter.OnReturnedToThrowPosition -= OnReturned;
        _botThrowRoutine = null;

        if (_matchEnded)
            yield break;

        SnapBotBallToFixedSpawn(shooter);
        ScheduleBotThrowAfterReactionDelay();
    }

    static bool IsBotBallInFlight(SlingshotShooter shooter)
    {
        if (shooter == null || !shooter.TryGetComponent(out Rigidbody rb))
            return false;

        return !rb.isKinematic;
    }

    IEnumerator BotThrowAfterDelayRoutine()
    {
        float minDelay = Mathf.Min(botReactionDelayMinSeconds, botReactionDelayMaxSeconds);
        float maxDelay = Mathf.Max(botReactionDelayMinSeconds, botReactionDelayMaxSeconds);
        float reactionDelay = maxDelay > minDelay ? UnityEngine.Random.Range(minDelay, maxDelay) : minDelay;
        if (reactionDelay > 0f)
            yield return new WaitForSeconds(reactionDelay);

        if (_matchEnded)
        {
            _botThrowRoutine = null;
            yield break;
        }

        // Сбрасываем до броска: OnThrown вызывается синхронно внутри TryLaunchScripted,
        // иначе ScheduleBotThrowAfterBallReturned видит занятую корутину и не ждёт возврат мяча.
        _botThrowRoutine = null;
        ExecuteBotThrow();
    }

    void ExecuteBotThrow()
    {
        SlingshotShooter shooter = ResolveBotShooterIfNeeded();
        if (shooter == null)
            return;

        shooter.IsInputLocked = true;
        SnapBotBallToFixedSpawn(shooter);
        shooter.PrepareForThrow();

        bool rollHit = UnityEngine.Random.Range(0f, 100f) < botAccuracyPercent;
        Vector3 hoopPos = ResolveBotAimPoint();

        Vector3 force;
        if (rollHit && TryCalculatePerfectThrowForce(shooter, hoopPos, out Vector3 perfectForce))
        {
            force = perfectForce;
        }
        else
        {
            Vector3 missTarget = CalculateMissTargetPoint(hoopPos);
            if (!TryCalculatePerfectThrowForce(shooter, missTarget, out force))
            {
                Vector3 from = shooter.transform.position;
                Vector3 planar = missTarget - from;
                planar.y = 0f;
                float pull = UnityEngine.Random.Range(normalizedPullMin, normalizedPullMax);
                force = shooter.ComputeThrowForceForPlanarDirection(planar, pull);
            }
        }

        if (shooter.TryLaunchScripted(force, PvPTeam.Bot))
            ScheduleBotThrowAfterBallReturned();

        TryUnlockCurrentThrowableForPlayer();
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

    void TryUnlockCurrentThrowableForPlayer()
    {
        if (_matchEnded || _spawner == null || _spawner.CurrentThrowableBall == null)
            return;

        _spawner.CurrentThrowableBall.IsInputLocked = false;
    }

    /// <summary>
    /// Ставит мяч бота строго в заданную мировую точку (перед PrepareForThrow и при старте сцены).
    /// </summary>
    void SnapBotBallToFixedSpawn(SlingshotShooter shooter = null)
    {
        SlingshotShooter target = shooter != null ? shooter : botShooter;
        if (target == null)
            return;

        target.transform.SetPositionAndRotation(botBallSpawnWorld, botBallSpawnWorldRotation);
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

    bool TryCalculatePerfectThrowForce(SlingshotShooter shooter, Vector3 target, out Vector3 force)
    {
        force = Vector3.zero;
        if (shooter == null || !shooter.TryGetComponent(out Rigidbody rb))
            return false;

        Vector3 origin = shooter.transform.position;
        Vector3 targetPoint = target;
        float gravity = Mathf.Abs(Physics.gravity.y);
        if (gravity < 0.001f)
            return false;

        Vector3 toTarget = targetPoint - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float horizontalDist = toTargetXZ.magnitude;

        // Явная дуга: старт -> пик выше кольца -> центр кольца.
        float arcBase = Mathf.Max(0.6f, perfectArcApexHeight) + perfectApexExtraPerMeter * horizontalDist;
        float desiredApexY = targetPoint.y + arcBase;
        desiredApexY = Mathf.Max(desiredApexY, origin.y + 0.6f);

        float upDistance = desiredApexY - origin.y;
        if (upDistance <= 0f)
            return false;

        float upVelocity = Mathf.Sqrt(2f * gravity * upDistance);
        if (upVelocity > perfectMaxUpVelocity)
            upVelocity = perfectMaxUpVelocity;

        // После ограничения vy реальный пик ниже desiredApexY — иначе timeDown и скорость по XZ неверны.
        float actualApexY = origin.y + (upVelocity * upVelocity) / (2f * gravity);
        float downDistance = actualApexY - targetPoint.y;
        if (downDistance <= 0f)
            return false;

        float timeUp = upVelocity / gravity;
        float timeDown = Mathf.Sqrt(2f * downDistance / gravity);
        float totalTime = timeUp + timeDown;
        if (totalTime <= 0.05f)
            return false;

        Vector3 velocityXZ = toTargetXZ / totalTime;
        Vector3 launchVelocity = velocityXZ + Vector3.up * upVelocity;

        force = launchVelocity * Mathf.Max(rb.mass, 0.001f);
        return true;
    }

    Vector3 CalculateMissTargetPoint(Vector3 baseTarget)
    {
        if (playerDefendedHoop == null)
            return baseTarget;

        Transform hoop = playerDefendedHoop.transform;
        Vector3 lateralDir = hoop.right.sqrMagnitude > 0.0001f ? hoop.right.normalized : Vector3.right;
        Vector3 forwardDir = hoop.forward.sqrMagnitude > 0.0001f ? hoop.forward.normalized : Vector3.forward;

        float lateral = UnityEngine.Random.Range(missLateralOffsetMin, missLateralOffsetMax);
        if (UnityEngine.Random.value < 0.5f)
            lateral = -lateral;

        float forward = UnityEngine.Random.Range(missForwardOffsetMin, missForwardOffsetMax);
        if (UnityEngine.Random.value < 0.5f)
            forward = -forward;

        float vertical = UnityEngine.Random.Range(missVerticalOffsetMin, missVerticalOffsetMax);

        return baseTarget + lateralDir * lateral + forwardDir * forward + Vector3.up * vertical;
    }

    Vector3 ResolveBotAimPoint()
    {
        if (playerDefendedHoop == null)
            return Vector3.zero;

        Transform hoop = playerDefendedHoop.transform;

        // Сериализованное поле Bot Aim Target не сбрасывается при смене дефолтов в коде: если в сцене остался Ring_collider,
        // всё равно предпочитаем вручную расставленный Aim под кольцом игрока.
        if (preferAimChildInPlayerHoop)
        {
            Transform aim = FindChildRecursive(hoop, "Aim");
            if (aim != null)
                return aim.position;
        }

        if (botAimTarget == null)
            botAimTarget = FindChildByPath(hoop, fallbackAimTargetPathInHoop);

        if (botAimTarget != null)
            return botAimTarget.position;

        Transform entry = FindChildRecursive(hoop, "EntryTrigger");
        Transform exit = FindChildRecursive(hoop, "ExitTrigger");

        if (entry != null && exit != null)
            return (entry.position + exit.position) * 0.5f + hoop.forward * perfectAimForwardOffset;

        return hoop.position + Vector3.up * perfectAimTargetYOffset + hoop.forward * perfectAimForwardOffset;
    }

    static Transform FindChildByPath(Transform root, string relativePath)
    {
        if (root == null || string.IsNullOrWhiteSpace(relativePath))
            return null;

        string[] parts = relativePath.Split('/');
        Transform current = root;
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (string.IsNullOrEmpty(part))
                continue;

            current = current.Find(part);
            if (current == null)
                return null;
        }

        return current;
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
