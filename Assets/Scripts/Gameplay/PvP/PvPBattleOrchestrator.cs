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
    [SerializeField] float botAimDelaySeconds = 0.45f;
    [SerializeField] float normalizedPullMin = 0.72f;
    [SerializeField] float normalizedPullMax = 0.95f;
    [SerializeField] float missYawDegreesMin = 18f;
    [SerializeField] float missYawDegreesMax = 42f;
    [SerializeField] float playerUnlockDelayAfterBotThrow = 1.15f;
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

        bool rollHit = UnityEngine.Random.Range(0f, 100f) < botAccuracyPercent;
        Vector3 hoopPos = ResolveBotAimPoint();

        Vector3 force;
        if (rollHit && TryCalculatePerfectThrowForce(shooter, hoopPos, out Vector3 perfectForce))
        {
            force = perfectForce;
        }
        else
        {
            Vector3 from = shooter.transform.position;
            Vector3 planar = hoopPos - from;
            planar.y = 0f;

            float yawDeg = UnityEngine.Random.Range(missYawDegreesMin, missYawDegreesMax);
            if (UnityEngine.Random.value < 0.5f)
                yawDeg = -yawDeg;

            planar = Quaternion.AngleAxis(yawDeg, Vector3.up) * planar;
            float pull = UnityEngine.Random.Range(normalizedPullMin, normalizedPullMax);
            force = shooter.ComputeThrowForceForPlanarDirection(planar, pull);
        }
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
