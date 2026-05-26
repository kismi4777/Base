using UnityEngine;

/// <summary>Настройки отскока и реакции мяча на столкновения.</summary>
[CreateAssetMenu(fileName = "BallBounceSettings", menuName = "Gameplay/Ball Bounce Settings")]
public class BallBounceSettings : ScriptableObject
{
    [Header("Материал коллайдера")]
    public PhysicsMaterial ballPhysicMaterial;

    [Header("Коэффициент отскока по поверхностям")]
    [Range(0f, 1f)] public float floorRestitution = 0.68f;
    [Range(0f, 1f)] public float rimRestitution = 0.58f;
    [Range(0f, 1f)] public float backboardRestitution = 0.42f;
    [Range(0f, 1f)] public float defaultRestitution = 0.6f;

    [Header("Сопротивление полёта")]
    [Min(0f)] public float airDrag = 0.12f;
    [Min(0f)] public float angularDrag = 0.45f;
    [Range(0.5f, 1f)] public float energyRetentionPerHit = 0.96f;

    [Header("Пороги")]
    [Min(0f)] public float minImpactSpeed = 0.4f;
    [Min(0f)] public float minBounceNormalSpeed = 0.25f;
    [Min(0f)] public float maxBounceSpeed = 14f;
    [Min(0f)] public float sleepSpeedThreshold = 0.22f;

    [Header("Вращение при ударе")]
    [Min(0f)] public float spinFromImpactFactor = 0.1f;
}
