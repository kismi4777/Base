using UnityEngine;

/// <summary>
/// Убирает мяч с поля при касании пола во время полёта.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallFloorDespawn : MonoBehaviour
{
    [Tooltip("Если true — мяч возвращается в BallSpawner (для мяча игрока). Для Ball_Bot выключить.")]
    [SerializeField] bool despawnViaBallSpawner = true;

    Rigidbody _rb;
    SlingshotShooter _shooter;
    BallSpawner _spawner;
    bool _despawned;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _shooter = GetComponent<SlingshotShooter>();

        if (BallSpawner.Instance != null)
            _spawner = BallSpawner.Instance;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_despawned || _rb.isKinematic)
            return;

        if (!IsFloorCollision(collision))
            return;

        _despawned = true;
        if (despawnViaBallSpawner)
        {
            ResolveSpawner()?.ReleaseBall(_shooter);
            return;
        }

        // Для отдельного мяча бота: не отдаём в пул игрока, просто возвращаем на старт.
        _shooter?.ResetBall();
        _despawned = false;
    }

    BallSpawner ResolveSpawner()
    {
        if (_spawner != null)
            return _spawner;

        _spawner = BallSpawner.Instance;
        return _spawner;
    }

    static bool IsFloorCollision(Collision collision)
    {
        if (collision.contactCount <= 0)
            return false;

        Collider otherCollider = collision.collider;
        BounceSurface surface = otherCollider.GetComponentInParent<BounceSurface>();
        if (surface != null)
            return surface.surfaceType == BounceSurfaceType.Floor;

        Vector3 normal = collision.GetContact(0).normal;
        return Mathf.Abs(Vector3.Dot(normal.normalized, Vector3.up)) > 0.55f;
    }

    public void ResetForReuse()
    {
        _despawned = false;
    }
}
