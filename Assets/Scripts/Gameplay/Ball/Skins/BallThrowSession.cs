using UnityEngine;

/// <summary>Состояние текущего броска для способностей скина.</summary>
[DisallowMultipleComponent]
public sealed class BallThrowSession : MonoBehaviour
{
    float _throwStartTime = -1f;
    Vector3 _lastFlightSamplePosition;
    float _flightDistanceMeters;
    bool _throwStarted;
    bool _throwFinished;
    bool _scoredThisThrow;
    bool _bouncedOffShield;
    bool _hitRimOrShieldWithoutScore;

    public float FlightTimeSeconds =>
        _throwStartTime < 0f ? 0f : Mathf.Max(0f, Time.time - _throwStartTime);

    /// <summary>Горизонтальное расстояние, пройденное мячом с начала броска (для Дракона).</summary>
    public float FlightDistanceMeters => _flightDistanceMeters;

    public bool ScoredThisThrow => _scoredThisThrow;
    public bool BouncedOffShield => _bouncedOffShield;
    public bool HitRimOrShieldWithoutScore => _hitRimOrShieldWithoutScore;
    public bool ThrowStarted => _throwStarted;
    public bool ThrowFinished => _throwFinished;

    public void BeginThrow()
    {
        _throwStartTime = Time.time;
        _lastFlightSamplePosition = transform.position;
        _flightDistanceMeters = 0f;
        _throwStarted = true;
        _throwFinished = false;
        _scoredThisThrow = false;
        _bouncedOffShield = false;
        _hitRimOrShieldWithoutScore = false;
    }

    void Update()
    {
        if (!_throwStarted || _throwFinished)
            return;

        Vector3 position = transform.position;
        Vector3 delta = position - _lastFlightSamplePosition;
        delta.y = 0f;
        _flightDistanceMeters += delta.magnitude;
        _lastFlightSamplePosition = position;
    }

    public void MarkShieldBounce() => _bouncedOffShield = true;

    public void MarkRimOrShieldHitWithoutScore() => _hitRimOrShieldWithoutScore = true;

    public void MarkScored()
    {
        _scoredThisThrow = true;
        _throwFinished = true;
    }

    /// <summary>
    /// Возвращает true только если реально завершён начатый бросок без гола.
    /// Защищает серию способностей от сброса на PrepareForThrow у нового мяча.
    /// </summary>
    public bool TryFinishWithoutScore()
    {
        if (!_throwStarted || _throwFinished)
            return false;

        _throwFinished = true;
        return true;
    }

    public void ResetBetweenThrows()
    {
        _throwStartTime = -1f;
        _flightDistanceMeters = 0f;
        _throwStarted = false;
        _throwFinished = false;
        _scoredThisThrow = false;
        _bouncedOffShield = false;
        _hitRimOrShieldWithoutScore = false;
    }
}
