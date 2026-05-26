using UnityEngine;

/// <summary>
/// Управляет только скоростью движения мяча вдоль баллистической траектории,
/// не изменяя саму геометрию кривой полёта.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallFlightSpeedController : MonoBehaviour
{
    [Header("Общее масштабирование скорости")]
    [SerializeField] bool useSpeedControl = true;
    [SerializeField] [Min(0.05f)] float baseSpeedMultiplier = 1f;
    [SerializeField] AnimationCurve speedOverFlight = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Замедление в верхней точке")]
    [SerializeField] bool slowDownAtApex = true;
    [SerializeField] [Range(0f, 0.95f)] float apexSpeedDrop = 0.35f;
    [SerializeField] [Range(0.02f, 0.6f)] float apexWindowNormalized = 0.2f;
    [SerializeField] [Min(0.05f)] float minApexSpeedFactor = 0.1f;

    Rigidbody _rb;
    bool _isControllingFlight;
    bool _hadGravityBeforeControl;
    Vector3 _launchVelocity;
    float _virtualTime;
    float _estimatedFlightDuration;
    float _apexTime;
    float _apexSigma;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void BeginControl(Vector3 launchVelocity)
    {
        CancelControl();

        if (!useSpeedControl || _rb == null)
            return;

        _launchVelocity = launchVelocity;
        _virtualTime = 0f;

        float gravityMagnitude = Mathf.Max(Mathf.Abs(Physics.gravity.y), 0.0001f);
        _apexTime = Mathf.Max(launchVelocity.y / gravityMagnitude, 0f);
        _estimatedFlightDuration = Mathf.Max(_apexTime * 2f, 0.1f);
        _apexSigma = Mathf.Max(_apexTime * apexWindowNormalized, 0.02f);

        _hadGravityBeforeControl = _rb.useGravity;
        _rb.useGravity = false;
        _isControllingFlight = true;
    }

    public void CancelControl()
    {
        if (!_isControllingFlight || _rb == null)
            return;

        _isControllingFlight = false;
        _rb.useGravity = _hadGravityBeforeControl;
    }

    void FixedUpdate()
    {
        if (!_isControllingFlight || _rb == null)
            return;

        if (_rb.isKinematic)
        {
            CancelControl();
            return;
        }

        float speedFactor = Mathf.Max(ComputeSpeedFactor(), 0.01f);
        _virtualTime += Time.fixedDeltaTime * speedFactor;

        Vector3 ballisticVelocity = _launchVelocity + Physics.gravity * _virtualTime;
        _rb.linearVelocity = ballisticVelocity * speedFactor;
    }

    void OnCollisionEnter(Collision _)
    {
        // После первого контакта возвращаемся к обычной физике,
        // чтобы существующая логика отскоков работала без изменений.
        CancelControl();
    }

    float ComputeSpeedFactor()
    {
        float normalizedTime = Mathf.Clamp01(_virtualTime / Mathf.Max(_estimatedFlightDuration, 0.001f));
        float profileFactor = Mathf.Max(speedOverFlight.Evaluate(normalizedTime), 0f);
        float speedFactor = baseSpeedMultiplier * profileFactor;

        if (!slowDownAtApex || _apexTime <= 0f || apexSpeedDrop <= 0f)
            return speedFactor;

        float sigma = Mathf.Max(_apexSigma, 0.001f);
        float apexDistance = _virtualTime - _apexTime;
        float gaussian = Mathf.Exp(-(apexDistance * apexDistance) / (2f * sigma * sigma));
        float apexFactor = 1f - apexSpeedDrop * gaussian;
        apexFactor = Mathf.Clamp(apexFactor, minApexSpeedFactor, 1f);
        return speedFactor * apexFactor;
    }
}
