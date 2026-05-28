using System;
using UnityEngine;

/// <summary>
/// Настраивает физику мяча и корректирует скорость при столкновении
/// (отскок, затухание, вращение от касания).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallBouncePhysics : MonoBehaviour
{
    [SerializeField] BallBounceSettings settings;

    Rigidbody _rb;
    Collider _collider;
    HoopNetReaction[] _hoopNetReactions;
    BallAbilityProcessor _abilityProcessor;
    BallThrowSession _throwSession;

    public event Action<float, Vector3> Impact;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _abilityProcessor = GetComponent<BallAbilityProcessor>();
        _throwSession = GetComponent<BallThrowSession>();
        CacheHoopNetReactions();
        ApplyRigidbodySettings();
        ApplyPhysicMaterial();
    }

    void OnValidate()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
        if (_collider == null)
            _collider = GetComponent<Collider>();

        ApplyRigidbodySettings();
        ApplyPhysicMaterial();
    }

    void ApplyRigidbodySettings()
    {
        if (_rb == null || settings == null)
            return;

        _rb.linearDamping = settings.airDrag;
        _rb.angularDamping = settings.angularDrag;
    }

    void ApplyPhysicMaterial()
    {
        if (_collider == null || settings == null || settings.ballPhysicMaterial == null)
            return;

        _collider.sharedMaterial = settings.ballPhysicMaterial;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (settings == null || _rb.isKinematic)
            return;

        if (collision.contactCount <= 0)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        ContactPoint contact = collision.GetContact(0);
        Collider otherCollider = collision.collider;

        // Реакцию сетки от кольца запускаем при любом касании, даже очень мягком.
        NotifyRimHit(otherCollider, contact.point, collision.relativeVelocity, impactSpeed);
        NotifyAbilityCollision(otherCollider, isRimHit: IsRimCollider(otherCollider));

        if (impactSpeed < settings.minImpactSpeed)
            return;

        ApplyBounce(contact.normal, otherCollider);
        ApplyImpactSpin(contact.normal, collision.relativeVelocity);
        DampMicroMotion();

        Impact?.Invoke(impactSpeed, contact.point);
    }

    void ApplyBounce(Vector3 normal, Collider otherCollider)
    {
        Vector3 velocity = _rb.linearVelocity;
        float normalSpeed = Vector3.Dot(velocity, normal);

        if (normalSpeed >= -settings.minBounceNormalSpeed)
            return;

        float restitution = ResolveRestitution(otherCollider, normal);
        Vector3 normalPart = normalSpeed * normal;
        Vector3 tangentPart = velocity - normalPart;

        float bouncedNormalSpeed = Mathf.Min(-normalSpeed * restitution, settings.maxBounceSpeed);
        Vector3 newVelocity = tangentPart + normal * bouncedNormalSpeed;
        newVelocity *= settings.energyRetentionPerHit;

        _rb.linearVelocity = newVelocity;
    }

    void ApplyImpactSpin(Vector3 normal, Vector3 relativeVelocity)
    {
        Vector3 tangentialHit = Vector3.ProjectOnPlane(-relativeVelocity, normal);
        if (tangentialHit.sqrMagnitude < 0.01f)
            return;

        Vector3 torqueAxis = Vector3.Cross(normal, tangentialHit.normalized);
        float spinImpulse = tangentialHit.magnitude * settings.spinFromImpactFactor;
        _rb.AddTorque(torqueAxis * spinImpulse, ForceMode.Impulse);
    }

    float ResolveRestitution(Collider otherCollider, Vector3 contactNormal)
    {
        if (otherCollider == null)
            return settings.defaultRestitution;

        BounceSurface surface = otherCollider.GetComponentInParent<BounceSurface>();
        if (surface != null)
        {
            return surface.surfaceType switch
            {
                BounceSurfaceType.Floor => settings.floorRestitution,
                BounceSurfaceType.Rim => settings.rimRestitution,
                BounceSurfaceType.Backboard => settings.backboardRestitution,
                _ => settings.defaultRestitution
            };
        }

        return ResolveRestitutionByNormal(contactNormal);
    }

    float ResolveRestitutionByNormal(Vector3 contactNormal)
    {
        float upAlignment = Mathf.Abs(Vector3.Dot(contactNormal.normalized, Vector3.up));
        if (upAlignment > 0.55f)
            return settings.floorRestitution;

        return settings.defaultRestitution;
    }

    void DampMicroMotion()
    {
        if (_rb.linearVelocity.sqrMagnitude > settings.sleepSpeedThreshold * settings.sleepSpeedThreshold)
            return;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    public void SetSettings(BallBounceSettings newSettings)
    {
        settings = newSettings;
        ApplyRigidbodySettings();
        ApplyPhysicMaterial();
    }

    void NotifyRimHit(Collider otherCollider, Vector3 impactPoint, Vector3 relativeVelocity, float impactSpeed)
    {
        if (otherCollider == null)
            return;

        BounceSurface surface = otherCollider.GetComponentInParent<BounceSurface>();
        bool isRim = surface != null && surface.surfaceType == BounceSurfaceType.Rim;
        if (!isRim)
        {
            // Фолбэк: некоторые коллайдеры кольца могут не иметь корректного маркера поверхности.
            string colliderName = otherCollider.name;
            string parentName = otherCollider.transform.parent != null ? otherCollider.transform.parent.name : string.Empty;
            isRim = colliderName.Contains("Ring") || parentName.Contains("Ring");
        }

        if (!isRim)
            return;

        if (_hoopNetReactions == null || _hoopNetReactions.Length == 0)
            CacheHoopNetReactions();

        for (int i = 0; i < _hoopNetReactions.Length; i++)
            _hoopNetReactions[i].OnRimHit(otherCollider, impactPoint, relativeVelocity, impactSpeed);
    }

    void CacheHoopNetReactions()
    {
        _hoopNetReactions = FindObjectsByType<HoopNetReaction>(FindObjectsSortMode.None);
    }

    void NotifyAbilityCollision(Collider otherCollider, bool isRimHit)
    {
        if (_abilityProcessor == null && _throwSession == null)
            return;

        bool isShield = IsShieldCollider(otherCollider);
        if (isShield)
            _abilityProcessor?.NotifyShieldCollision();

        if (_throwSession != null && !_throwSession.ScoredThisThrow && (isRimHit || isShield))
            _abilityProcessor?.NotifyRimOrShieldCollisionWithoutScore();
    }

    static bool IsShieldCollider(Collider otherCollider)
    {
        if (otherCollider == null)
            return false;

        Transform current = otherCollider.transform;
        while (current != null)
        {
            if (current.name.IndexOf("Shild", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            current = current.parent;
        }

        return false;
    }

    bool IsRimCollider(Collider otherCollider)
    {
        if (otherCollider == null)
            return false;

        BounceSurface surface = otherCollider.GetComponentInParent<BounceSurface>();
        if (surface != null && surface.surfaceType == BounceSurfaceType.Rim)
            return true;

        string colliderName = otherCollider.name;
        string parentName = otherCollider.transform.parent != null ? otherCollider.transform.parent.name : string.Empty;
        return colliderName.Contains("Ring") || parentName.Contains("Ring");
    }
}
