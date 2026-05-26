using System.Collections.Generic;
using UnityEngine;

/// <summary>Показывает траекторию маркерами-плоскостями вдоль пути полёта.</summary>
public class TrajectoryPredictor : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField] GameObject markerPrefab;
    [SerializeField] Material markerMaterial;
    [SerializeField] float markerSize = 0.25f;
    [SerializeField] int steps = 20;
    [SerializeField] float timeStep = 0.08f;
    [SerializeField] LayerMask collisionMask;
    [SerializeField] bool stopOnCollision = true;
    [Header("Затухание")]
    [SerializeField] [Range(0f, 1f)] float startAlpha = 0.9f;
    [SerializeField] [Range(0f, 1f)] float endAlpha = 0.05f;
    [SerializeField] AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    readonly List<Transform> _markers = new();
    readonly List<Renderer> _markerRenderers = new();
    MaterialPropertyBlock _propertyBlock;
    int _activeCount;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        _propertyBlock = new MaterialPropertyBlock();

        BuildPool();
        HideTrajectory();
    }

    void BuildPool()
    {
        ClearPool();

        for (int i = 0; i < steps; i++)
        {
            Transform marker = CreateMarker().transform;
            _markers.Add(marker);
            _markerRenderers.Add(marker.GetComponent<Renderer>());
        }
    }

    Transform CreateMarker()
    {
        GameObject go;

        if (markerPrefab != null)
        {
            go = Instantiate(markerPrefab, transform);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.transform.SetParent(transform, false);

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            if (markerMaterial != null)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = markerMaterial;
            }
        }

        go.name = $"TrajectoryMarker_{_markers.Count}";

        // Стандартный Plane в Unity — 10×10 единиц.
        const float defaultPlaneSize = 10f;
        float planeScale = markerSize / defaultPlaneSize;
        Vector3 parentScale = transform.lossyScale;
        go.transform.localScale = new Vector3(
            planeScale / Mathf.Max(parentScale.x, 0.001f),
            planeScale / Mathf.Max(parentScale.y, 0.001f),
            planeScale / Mathf.Max(parentScale.z, 0.001f));

        TrajectoryMarker marker = go.GetComponent<TrajectoryMarker>();
        if (marker == null)
            marker = go.AddComponent<TrajectoryMarker>();

        marker.BindCamera(targetCamera);
        go.SetActive(false);
        return go.transform;
    }

    void ClearPool()
    {
        for (int i = _markers.Count - 1; i >= 0; i--)
        {
            if (_markers[i] != null)
                Destroy(_markers[i].gameObject);
        }

        _markers.Clear();
        _markerRenderers.Clear();
        _activeCount = 0;
    }

    public void ShowTrajectory(Vector3 startPosition, Vector3 initialVelocity)
    {
        if (_markers.Count == 0)
            BuildPool();

        Vector3 position = startPosition;
        Vector3 velocity = initialVelocity;
        _activeCount = 0;

        for (int i = 0; i < steps && i < _markers.Count; i++)
        {
            Transform marker = _markers[i];
            marker.position = position;
            marker.gameObject.SetActive(true);
            _activeCount++;

            Vector3 nextVelocity = velocity + Physics.gravity * timeStep;
            Vector3 nextPosition = position + velocity * timeStep;

            if (stopOnCollision && Physics.Linecast(position, nextPosition, out RaycastHit hit, collisionMask))
            {
                marker.position = hit.point;
                break;
            }

            velocity = nextVelocity;
            position = nextPosition;
        }

        UpdateMarkersAlpha();
        HideExcessMarkers();
    }

    public void HideTrajectory()
    {
        _activeCount = 0;
        HideExcessMarkers();
    }

    void UpdateMarkersAlpha()
    {
        if (_activeCount <= 0)
            return;

        int denominator = Mathf.Max(1, _activeCount - 1);
        for (int i = 0; i < _activeCount; i++)
        {
            Renderer markerRenderer = _markerRenderers[i];
            if (markerRenderer == null)
                continue;

            float t = i / (float)denominator;
            float curveValue = Mathf.Clamp01(alphaCurve.Evaluate(t));
            float alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            SetRendererAlpha(markerRenderer, alpha);
        }
    }

    void SetRendererAlpha(Renderer markerRenderer, float alpha)
    {
        if (_propertyBlock == null || markerRenderer == null)
            return;

        markerRenderer.GetPropertyBlock(_propertyBlock);

        if (markerRenderer.sharedMaterial != null)
        {
            Color baseColor = markerRenderer.sharedMaterial.HasProperty("_BaseColor")
                ? markerRenderer.sharedMaterial.GetColor("_BaseColor")
                : Color.white;
            baseColor.a = alpha;
            _propertyBlock.SetColor("_BaseColor", baseColor);

            if (markerRenderer.sharedMaterial.HasProperty("_Color"))
            {
                Color color = markerRenderer.sharedMaterial.GetColor("_Color");
                color.a = alpha;
                _propertyBlock.SetColor("_Color", color);
            }
        }

        markerRenderer.SetPropertyBlock(_propertyBlock);
    }

    void HideExcessMarkers()
    {
        for (int i = 0; i < _markers.Count; i++)
            _markers[i].gameObject.SetActive(i < _activeCount);
    }

    void OnDestroy()
    {
        ClearPool();
    }
}
