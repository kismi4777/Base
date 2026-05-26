using UnityEngine;

/// <summary>Маркер траектории — плоскость всегда повёрнута лицом к камере.</summary>
public class TrajectoryMarker : MonoBehaviour
{
    Camera _camera;

    public void BindCamera(Camera camera)
    {
        _camera = camera;
    }

    void LateUpdate()
    {
        if (_camera == null)
            return;

        Vector3 toCamera = _camera.transform.position - transform.position;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        Vector3 normal = toCamera.normalized;

        // Стандартный Plane: нормаль грани — локальный +Y. Для LookRotation второй аргумент — желаемый up (ось Y).
        Vector3 right = Vector3.Cross(Vector3.up, normal);
        if (right.sqrMagnitude < 1e-6f)
            right = Vector3.Cross(Vector3.forward, normal);
        right.Normalize();

        Vector3 forward = Vector3.Cross(normal, right).normalized;
        transform.rotation = Quaternion.LookRotation(forward, normal);
    }
}
