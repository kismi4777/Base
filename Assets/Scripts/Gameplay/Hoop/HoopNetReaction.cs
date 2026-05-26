using UnityEngine;

/// <summary>
/// Визуальная реакция сетки при чистом прохождении мяча через кольцо.
/// Скрипт не меняет физику мяча, только анимирует Transform сетки.
/// </summary>
public class HoopNetReaction : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField] Rigidbody targetBall;
    [Tooltip("Центр кольца. Обычно Transform объекта Hoop.")]
    [SerializeField] Transform hoopAnchor;
    [Tooltip("Transform визуальной сетки (например BUSKET).")]
    [SerializeField] Transform netVisual;
    [SerializeField] Vector3 centerOffset = new(0f, -0.15f, 0f);

    [Header("Условия срабатывания")]
    [SerializeField] float triggerRadius = 0.26f;
    [SerializeField] float minDownwardSpeed = 0.75f;
    [SerializeField] float retriggerCooldown = 0.2f;

    [Header("Анимация сетки")]
    [SerializeField] float pullDownDistance = 0.2f;
    [SerializeField] float swayDistance = 0.08f;
    [SerializeField] float oscillationFrequency = 6f;
    [SerializeField] float damping = 5.5f;
    [SerializeField] float maxAnimDuration = 0.65f;
    [SerializeField] float maxSpeedForFullImpact = 14f;

    [Header("Лёгкая реакция от касания кольца")]
    [SerializeField] float rimHitImpactScale = 0.5f;
    [SerializeField] float rimMinImpactSpeed = 0f;
    [SerializeField] float rimHitCooldown = 0.015f;
    [SerializeField] float rimMinRadius = 0f;
    [SerializeField] float rimMaxRadius = 1.25f;
    [Tooltip("Попадания по этим объектам не вызывают тряску сетки.")]
    [SerializeField] Transform[] excludedRimHitRoots;

    Rigidbody _ball;
    Vector3 _previousBallPosition;
    bool _hasPreviousBallPosition;
    float _lastTriggerTime = -10f;
    float _lastRimTriggerTime = -10f;

    Transform _visualParent;
    Vector3 _baseLocalPosition;
    Quaternion _baseLocalRotation;

    float _animTime;
    float _impact;
    Vector2 _swayDirection;
    bool _isAnimating;

    void Awake()
    {
        BindBallIfNeeded();

        if (hoopAnchor == null)
            hoopAnchor = transform;

        if (netVisual == null)
            netVisual = transform;

        _ball = targetBall;
        _visualParent = netVisual.parent != null ? netVisual.parent : transform;
        _baseLocalPosition = netVisual.localPosition;
        _baseLocalRotation = netVisual.localRotation;
    }

    void FixedUpdate()
    {
        if (_ball == null)
        {
            BindBallIfNeeded();
            _ball = targetBall;
            _hasPreviousBallPosition = false;
            return;
        }

        if (_ball.isKinematic)
        {
            _hasPreviousBallPosition = false;
            return;
        }

        Vector3 currentPosition = _ball.position;
        if (_hasPreviousBallPosition)
            TryReactOnCrossing(_previousBallPosition, currentPosition, _ball.linearVelocity);

        _previousBallPosition = currentPosition;
        _hasPreviousBallPosition = true;
    }

    void Update()
    {
        if (!_isAnimating || netVisual == null || _visualParent == null)
            return;

        _animTime += Time.deltaTime;
        float progress = _animTime;
        float decay = Mathf.Exp(-damping * progress);
        float phase = progress * oscillationFrequency * Mathf.PI * 2f;
        float pull = Mathf.Max(0f, Mathf.Sin(phase));
        float swayWave = Mathf.Sin(phase + Mathf.PI * 0.5f);

        float verticalMeters = pullDownDistance * _impact * pull * decay;
        float sideMeters = swayDistance * _impact * swayWave * decay;

        Vector3 swayWorld =
            (hoopAnchor.right * _swayDirection.x + hoopAnchor.forward * _swayDirection.y) * sideMeters;

        Vector3 localOffset =
            _visualParent.InverseTransformVector(Vector3.down * verticalMeters) +
            _visualParent.InverseTransformVector(swayWorld);

        netVisual.localPosition = _baseLocalPosition + localOffset;

        float tilt = sideMeters * 90f;
        netVisual.localRotation = _baseLocalRotation *
                                  Quaternion.Euler(tilt * _swayDirection.y, 0f, -tilt * _swayDirection.x);

        if (_animTime >= maxAnimDuration || decay < 0.01f)
            StopAnimation();
    }

    public void OnBallLaunched(Rigidbody ball)
    {
        if (targetBall != null && ball != targetBall)
            return;

        targetBall = ball;
        _ball = ball;
        _hasPreviousBallPosition = false;
    }

    public void ResetShot()
    {
        _hasPreviousBallPosition = false;
        StopAnimation();
    }

    public void OnRimHit(Collider hitCollider, Vector3 impactPoint, Vector3 relativeVelocity, float impactSpeed)
    {
        if (Time.time - _lastRimTriggerTime < rimHitCooldown)
            return;

        if (impactSpeed < rimMinImpactSpeed)
            return;

        if (IsExcludedRimHit(hitCollider))
            return;

        Vector3 center = GetHoopCenter();
        float distanceToCenter = HorizontalDistance(impactPoint, center);
        bool hasRadiusLimits = rimMaxRadius > rimMinRadius + 0.0001f;
        if (hasRadiusLimits && (distanceToCenter < rimMinRadius || distanceToCenter > rimMaxRadius))
            return;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(relativeVelocity, Vector3.up);
        Vector2 sway;
        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            sway = new Vector2(horizontalVelocity.x, horizontalVelocity.z).normalized;
        }
        else
        {
            Vector3 fromCenter = impactPoint - center;
            Vector2 fromCenter2D = new(fromCenter.x, fromCenter.z);
            sway = fromCenter2D.sqrMagnitude > 0.0001f ? fromCenter2D.normalized : Vector2.right;
        }

        float normalizedImpact = Mathf.Clamp01(impactSpeed / Mathf.Max(0.001f, maxSpeedForFullImpact));
        float rimImpact = Mathf.Lerp(0.18f, 0.55f, normalizedImpact) * rimHitImpactScale;
        StartAnimation(rimImpact, sway, true);
        _lastRimTriggerTime = Time.time;
    }

    bool IsExcludedRimHit(Collider hitCollider)
    {
        if (hitCollider == null || excludedRimHitRoots == null || excludedRimHitRoots.Length == 0)
            return false;

        Transform current = hitCollider.transform;
        for (int i = 0; i < excludedRimHitRoots.Length; i++)
        {
            Transform excludedRoot = excludedRimHitRoots[i];
            if (excludedRoot == null)
                continue;

            if (current == excludedRoot || current.IsChildOf(excludedRoot))
                return true;
        }

        return false;
    }

    void TryReactOnCrossing(Vector3 previousPosition, Vector3 currentPosition, Vector3 currentVelocity)
    {
        if (Time.time - _lastTriggerTime < retriggerCooldown)
            return;

        Vector3 center = GetHoopCenter();

        if (previousPosition.y <= center.y || currentPosition.y > center.y)
            return;

        if (currentVelocity.y > -minDownwardSpeed)
            return;

        float yDelta = previousPosition.y - currentPosition.y;
        if (Mathf.Abs(yDelta) < 0.0001f)
            return;

        float t = (previousPosition.y - center.y) / yDelta;
        t = Mathf.Clamp01(t);
        Vector3 crossingPoint = Vector3.Lerp(previousPosition, currentPosition, t);

        float distance = HorizontalDistance(crossingPoint, center);
        if (distance > triggerRadius)
            return;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        Vector2 sway = new(horizontalVelocity.x, horizontalVelocity.z);
        if (sway.sqrMagnitude < 0.0001f)
            sway = Random.insideUnitCircle.normalized;
        else
            sway.Normalize();

        float speed = currentVelocity.magnitude;
        float normalizedImpact = Mathf.Clamp01(speed / Mathf.Max(0.001f, maxSpeedForFullImpact));
        StartAnimation(Mathf.Lerp(0.45f, 1f, normalizedImpact), sway, false);
        _lastTriggerTime = Time.time;
    }

    void StartAnimation(float impact, Vector2 swayDirection, bool additive)
    {
        float clampedImpact = Mathf.Clamp01(impact);
        Vector2 normalizedDirection = swayDirection.sqrMagnitude > 0f ? swayDirection.normalized : Vector2.right;

        if (additive && _isAnimating)
        {
            _impact = Mathf.Clamp01(_impact + clampedImpact * 0.5f);
            _swayDirection = Vector2.Lerp(_swayDirection, normalizedDirection, 0.35f).normalized;
            _animTime *= 0.35f;
            return;
        }

        _impact = clampedImpact;
        _swayDirection = normalizedDirection;
        _animTime = 0f;
        _isAnimating = true;
    }

    void StopAnimation()
    {
        _isAnimating = false;
        _animTime = 0f;

        if (netVisual == null)
            return;

        netVisual.localPosition = _baseLocalPosition;
        netVisual.localRotation = _baseLocalRotation;
    }

    void BindBallIfNeeded()
    {
        if (targetBall != null)
            return;

        SlingshotShooter shooter = FindFirstObjectByType<SlingshotShooter>();
        if (shooter != null && shooter.TryGetComponent(out Rigidbody rb))
            targetBall = rb;
    }

    Vector3 GetHoopCenter()
    {
        Transform anchor = hoopAnchor != null ? hoopAnchor : transform;
        return anchor.position + centerOffset;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    void OnDisable()
    {
        StopAnimation();
    }
}
