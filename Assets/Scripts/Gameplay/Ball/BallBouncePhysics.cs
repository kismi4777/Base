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

    public event Action<float, Vector3> Impact;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
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

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < settings.minImpactSpeed)
            return;

        ContactPoint contact = collision.GetContact(0);
        ApplyBounce(contact.normal, collision.collider);
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
}
