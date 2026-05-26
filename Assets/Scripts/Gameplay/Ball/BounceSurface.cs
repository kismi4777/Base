using UnityEngine;

/// <summary>Маркер поверхности для расчёта отскока мяча.</summary>
public class BounceSurface : MonoBehaviour
{
    public BounceSurfaceType surfaceType = BounceSurfaceType.Floor;
}

public enum BounceSurfaceType
{
    Floor,
    Rim,
    Backboard,
    Generic
}
