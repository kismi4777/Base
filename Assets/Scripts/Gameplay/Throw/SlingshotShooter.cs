using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Бросок через натяжение по экрану: вектор от точки касания к текущей позиции пальца/мыши.
/// Сила и направление задаются pullVector в экранных координатах.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SlingshotShooter : MonoBehaviour
{
    [Header("Настройки броска")]
    public float powerMultiplier = 20f;
    public float maxPullDistance = 0.5f;
    [Tooltip("Минимальная длина натяжения (нормализованная к высоте экрана)")]
    public float minPullMagnitude = 0.02f;
    [Header("Дуга броска")]
    [Tooltip("Угол взлёта при слабом натяжении (градусы)")]
    [Range(20f, 80f)] public float minLaunchElevationDeg = 38f;
    [Tooltip("Угол взлёта при полном натяжении — чем выше, тем круче дуга")]
    [Range(20f, 85f)] public float maxLaunchElevationDeg = 64f;
    [Tooltip("Доп. подъём вверх на единицу импульса вперёд (высота пика дуги на дальних бросках)")]
    [Min(0f)] public float arcHeightPerForward = 0.32f;
    [Tooltip("Общая «высота» дуги: умножает только вертикальную силу (горизонталь без изменений). >1 — круче баскетбольная дуга, <1 — площе.")]
    [Range(0.35f, 2.5f)] public float arcLoftMultiplier = 1f;
    [Tooltip("Минимальная доля «вперёд» при боковом натяжении")]
    [Range(0.05f, 0.6f)] public float minForwardFraction = 0.2f;
    [Tooltip("Сила обратного закрута (backspin) при броске")]
    public float backspinTorque = 3f;

    [Header("Связи")]
    [SerializeField] TrajectoryPredictor trajectory;

    Rigidbody _rb;
    Vector2 _startScreenPos;
    Vector2 _currentScreenPos;
    bool _isAiming;
    int _activePointerId = -1;
    Vector3 _initialPosition;
    Quaternion _initialRotation;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        if (trajectory == null)
            trajectory = GetComponentInChildren<TrajectoryPredictor>();
    }

    void Update()
    {
        if (TryBeginAim())
            return;

        if (_isAiming)
            UpdateAiming();

        if (Input.GetKeyDown(KeyCode.R))
            ResetBall();
    }

    bool TryBeginAim()
    {
        if (_isAiming)
            return false;

        if (!TryGetPointerDown(out int pointerId, out Vector2 screenPos))
            return false;

        if (IsPointerOverUi())
            return false;

        _activePointerId = pointerId;
        _startScreenPos = screenPos;
        _currentScreenPos = screenPos;
        _isAiming = true;
        return true;
    }

    void UpdateAiming()
    {
        if (!TryGetPointerPosition(_activePointerId, out Vector2 screenPos))
        {
            EndAimAndShoot();
            return;
        }

        if (IsOutsideScreen(screenPos))
        {
            EndAimAndShoot();
            return;
        }

        _currentScreenPos = screenPos;

        Vector3 forceVector = CalculateShootForce();
        Vector3 plannedVelocity = forceVector / Mathf.Max(_rb.mass, 0.001f);

        if (trajectory != null)
        {
            if (plannedVelocity.sqrMagnitude > 0.0001f)
                trajectory.ShowTrajectory(transform.position, plannedVelocity);
            else
                trajectory.HideTrajectory();
        }

        if (IsPointerReleased(_activePointerId))
            EndAimAndShoot();
    }

    void EndAimAndShoot()
    {
        _isAiming = false;
        _activePointerId = -1;
        Shoot();
        trajectory?.HideTrajectory();
    }

    Vector3 CalculateShootForce()
    {
        Vector2 pullVector = (_startScreenPos - _currentScreenPos) / Screen.height;
        pullVector = Vector2.ClampMagnitude(pullVector, maxPullDistance);

        float pullMagnitude = pullVector.magnitude;
        if (pullMagnitude < minPullMagnitude)
            return Vector3.zero;

        // Сила броска — от полной длины натяжения, а не только вертикали экрана.
        float normalizedPower = pullMagnitude / Mathf.Max(maxPullDistance, 0.001f);
        float elevationRad = Mathf.Lerp(minLaunchElevationDeg, maxLaunchElevationDeg, normalizedPower) * Mathf.Deg2Rad;
        float totalImpulse = pullMagnitude * powerMultiplier;

        float horizontalImpulse = totalImpulse * Mathf.Cos(elevationRad);
        float verticalImpulse = totalImpulse * Mathf.Sin(elevationRad);

        Vector2 pullDir = pullVector / pullMagnitude;
        float forwardWeight = Mathf.Max(pullDir.y, minForwardFraction);
        Vector2 xzDirection = new Vector2(pullDir.x, forwardWeight).normalized;

        float verticalForce =
            (verticalImpulse + horizontalImpulse * xzDirection.y * arcHeightPerForward) * arcLoftMultiplier;

        Vector3 force = new(
            xzDirection.x * horizontalImpulse,
            verticalForce,
            xzDirection.y * horizontalImpulse);

        if (force.z < 0f)
            force.z = 0f;
        if (force.y < 0f)
            force.y = 0f;

        return force;
    }

    void Shoot()
    {
        Vector3 force = CalculateShootForce();
        if (force == Vector3.zero)
            return;

        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.AddForce(force, ForceMode.Impulse);
        ApplyBackspin(force);

        Vector3 launchVelocity = force / Mathf.Max(_rb.mass, 0.001f);
        NotifyHoopMagnets(launchVelocity);
    }

    void NotifyHoopMagnets(Vector3 launchVelocity)
    {
        HoopRimMagnet[] magnets = FindObjectsByType<HoopRimMagnet>(FindObjectsSortMode.None);
        for (int i = 0; i < magnets.Length; i++)
            magnets[i].OnBallLaunched(_rb, launchVelocity);
    }

    void ApplyBackspin(Vector3 throwForce)
    {
        Vector3 horizontal = new(throwForce.x, 0f, throwForce.z);
        if (horizontal.sqrMagnitude < 0.0001f)
            return;

        // Ось перпендикулярна направлению броска; минус — backspin (верх мяча назад по полёту).
        Vector3 spinAxis = Vector3.Cross(Vector3.up, horizontal.normalized);
        _rb.AddTorque(-spinAxis * backspinTorque, ForceMode.Impulse);
    }

    public void ResetBall()
    {
        _isAiming = false;
        _activePointerId = -1;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        trajectory?.HideTrajectory();

        HoopRimMagnet[] magnets = FindObjectsByType<HoopRimMagnet>(FindObjectsSortMode.None);
        for (int i = 0; i < magnets.Length; i++)
            magnets[i].ResetShot();
    }

    static bool IsOutsideScreen(Vector2 screenPos) =>
        screenPos.x < 0f || screenPos.y < 0f ||
        screenPos.x > Screen.width || screenPos.y > Screen.height;

    static bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    static bool TryGetPointerDown(out int pointerId, out Vector2 screenPos)
    {
        pointerId = -1;
        screenPos = default;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                pointerId = touch.fingerId;
                screenPos = touch.position;
                return true;
            }

            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }

        return false;
    }

    static bool TryGetPointerPosition(int pointerId, out Vector2 screenPos)
    {
        screenPos = default;

        if (pointerId >= 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != pointerId)
                    continue;

                screenPos = touch.position;
                return true;
            }

            return false;
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }

        return false;
    }

    static bool IsPointerReleased(int pointerId)
    {
        if (pointerId >= 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != pointerId)
                    continue;

                return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }

            return true;
        }

        return Input.GetMouseButtonUp(0);
    }
}
