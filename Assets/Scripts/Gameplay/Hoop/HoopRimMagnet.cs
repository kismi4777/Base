using UnityEngine;

/// <summary>
/// Ассист кольца: при броске считает, куда мяч пересечёт плоскость щели по баллистике.
/// Если это «чуть мимо» — с заданным шансом на всём полёте подгоняет скорость к попаданию в центр.
/// </summary>
public class HoopRimMagnet : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField] Rigidbody targetBall;
    [Tooltip("Опорная точка кольца (обычно Hoop). Центр щели = её позиция + centerOffset (в метрах мира).")]
    [SerializeField] Transform hoopAnchor;
    [SerializeField] Vector3 centerOffset = Vector3.zero;

    [Header("Шанс и сила")]
    [Range(0f, 1f)] [SerializeField] float assistChance = 0.75f;
    [Tooltip("1 = мгновенно подставлять нужную скорость, 0.2 = мягко.")]
    [Range(0.05f, 1f)] [SerializeField] float velocityBlend = 0.65f;

    [Header("Коридор «чуть мимо»")]
    [SerializeField] float minHorizontalMiss = 0.05f;
    [SerializeField] float maxHorizontalMiss = 0.55f;
    [Tooltip("Насколько выше/ниже плоскости кольца может быть точка пересечения.")]
    [SerializeField] float maxPlaneMiss = 0.35f;

    [Header("Отладка")]
    [SerializeField] bool drawGizmos = true;

    bool _shotEvaluated;
    bool _assistActive;
    bool _hasAimPoint;
    Vector3 _aimPoint;

    void Awake() => BindBallIfNeeded();

    void BindBallIfNeeded()
    {
        if (targetBall != null)
            return;

        SlingshotShooter shooter = FindFirstObjectByType<SlingshotShooter>();
        if (shooter != null && shooter.TryGetComponent(out Rigidbody rb))
            targetBall = rb;
    }

    /// <summary>Вызывается из SlingshotShooter в момент броска.</summary>
    public void OnBallLaunched(Rigidbody ball, Vector3 launchVelocity)
    {
        if (ball != targetBall)
            return;

        ResetShot();
        EvaluateShot(ball.position, launchVelocity);
    }

    void FixedUpdate()
    {
        if (targetBall == null)
        {
            BindBallIfNeeded();
            return;
        }

        Rigidbody ball = targetBall;

        if (ball.isKinematic || ball.linearVelocity.sqrMagnitude < 0.04f)
        {
            ResetShot();
            return;
        }

        Vector3 center = GetHoopCenter();

        // Запасной путь: если бросок не пришёл через OnBallLaunched
        if (!_shotEvaluated)
            EvaluateShot(ball.position, ball.linearVelocity);

        if (_assistActive)
            SteerTowardHoop(ball, center);
    }

    void EvaluateShot(Vector3 startPos, Vector3 startVelocity)
    {
        Vector3 center = GetHoopCenter();

        if (!TryPredictPlaneCrossing(startPos, startVelocity, center.y, out Vector3 crossing, out _))
            return;

        _hasAimPoint = true;
        _aimPoint = crossing;

        float horizontalMiss = HorizontalDistance(crossing, center);
        if (horizontalMiss < minHorizontalMiss || horizontalMiss > maxHorizontalMiss)
            return;

        if (Mathf.Abs(crossing.y - center.y) > maxPlaneMiss)
            return;

        _shotEvaluated = true;
        _assistActive = Random.value <= assistChance;
    }

    void SteerTowardHoop(Rigidbody ball, Vector3 center)
    {
        if (!TryGetDescentTimeToPlane(ball.position.y, ball.linearVelocity.y, center.y, out float timeToHoop))
            return;

        if (timeToHoop < 0.03f || timeToHoop > 5f)
            return;

        Vector3 gravity = Physics.gravity;
        Vector3 desiredVelocity = (center - ball.position - 0.5f * gravity * timeToHoop * timeToHoop) / timeToHoop;
        ball.linearVelocity = Vector3.Lerp(ball.linearVelocity, desiredVelocity, velocityBlend);

        // Прошли щель — выключаем ассист на этом броске
        if (ball.position.y < center.y - 0.12f && HorizontalDistance(ball.position, center) < maxHorizontalMiss)
            _assistActive = false;
    }

    Vector3 GetHoopCenter()
    {
        Transform anchor = hoopAnchor != null ? hoopAnchor : transform;
        return anchor.position + centerOffset;
    }

    public void ResetShot()
    {
        _shotEvaluated = false;
        _assistActive = false;
        _hasAimPoint = false;
    }

    public static bool TryPredictPlaneCrossing(
        Vector3 startPos,
        Vector3 startVelocity,
        float planeY,
        out Vector3 crossing,
        out float time)
    {
        crossing = startPos;
        time = 0f;

        if (!TryGetDescentTimeToPlane(startPos.y, startVelocity.y, planeY, out time))
            return false;

        crossing = startPos + startVelocity * time + 0.5f * Physics.gravity * time * time;
        return true;
    }

    static bool TryGetDescentTimeToPlane(float startY, float velocityY, float planeY, out float time)
    {
        time = 0f;
        float gravityY = Physics.gravity.y;
        float a = 0.5f * gravityY;
        float b = velocityY;
        float c = startY - planeY;
        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0f)
            return false;

        float sqrtD = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtD) / (2f * a);
        float t2 = (-b + sqrtD) / (2f * a);

        // Ближайшее пересечение плоскости на спуске (не «пробой» снизу вверх)
        float bestTime = float.PositiveInfinity;
        foreach (float candidate in new[] { t1, t2 })
        {
            if (candidate <= 0.02f)
                continue;

            float verticalSpeedAtCrossing = velocityY + gravityY * candidate;
            if (verticalSpeedAtCrossing >= -0.01f)
                continue;

            if (candidate < bestTime)
                bestTime = candidate;
        }

        if (!float.IsFinite(bestTime))
            return false;

        time = bestTime;
        return true;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Vector3 center = GetHoopCenter();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, 0.06f);

        Gizmos.color = new Color(0.3f, 1f, 0.35f, 1f);
        DrawRing(center, minHorizontalMiss);
        Gizmos.color = new Color(1f, 0.55f, 0.15f, 1f);
        DrawRing(center, maxHorizontalMiss);

        if (_hasAimPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_aimPoint, 0.05f);
            Gizmos.DrawLine(center, _aimPoint);
        }
    }

    static void DrawRing(Vector3 center, float radius, int segments = 48)
    {
        if (radius <= 0f)
            return;

        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
