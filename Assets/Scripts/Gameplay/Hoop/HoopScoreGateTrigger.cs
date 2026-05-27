using UnityEngine;

/// <summary>
/// Триггер ворот кольца: Entry помечает вход мяча, Exit подтверждает гол и снимает здоровье.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HoopScoreGateTrigger : MonoBehaviour
{
    public enum GateType
    {
        Entry = 0,
        Exit = 1
    }

    [SerializeField] HoopHealth hoopHealth;
    [SerializeField] Rigidbody targetBall;
    [SerializeField] GateType gateType = GateType.Entry;
    [SerializeField] bool requireDownwardVelocity;
    [SerializeField] float minDownwardVelocity = 0.02f;

    void Awake()
    {
        if (hoopHealth == null)
            hoopHealth = GetComponentInParent<HoopHealth>();
    }

    void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Process(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Резерв на случай пропущенного OnTriggerEnter.
        Process(other);
    }

    void Process(Collider other)
    {
        if (hoopHealth == null || other == null)
            return;

        Rigidbody ball = other.attachedRigidbody;
        if (ball == null)
            return;

        if (targetBall != null && ball != targetBall)
            return;

        if (targetBall == null && !ball.TryGetComponent(out BallBouncePhysics _))
            return;

        if (requireDownwardVelocity && ball.linearVelocity.y > -minDownwardVelocity)
            return;

        if (gateType == GateType.Entry)
            hoopHealth.RegisterEntry(ball);
        else
            hoopHealth.RegisterExit(ball);
    }
}

