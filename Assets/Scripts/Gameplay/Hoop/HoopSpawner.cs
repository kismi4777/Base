using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спавнит кольцо в случайной точке из дочерних точек спавна.
/// При уничтожении кольца (через HoopHealth) создаёт новое после задержки.
/// </summary>
public class HoopSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] GameObject hoopPrefab;
    [SerializeField] float respawnDelay = 1f;
    [SerializeField] bool randomizeOnStart = true;
    [SerializeField] bool removeExtraHoopsOnStart = true;

    readonly List<Transform> _spawnPoints = new();
    GameObject _currentHoop;
    Coroutine _respawnRoutine;

    void Awake()
    {
        CacheSpawnPoints();
    }

    void Start()
    {
        if (_spawnPoints.Count == 0)
        {
            Debug.LogError("HoopSpawner: не найдены точки спавна. Добавьте дочерние объекты-точки.", this);
            return;
        }

        AttachOrSpawnInitialHoop();
        if (_currentHoop == null)
            return;

        if (randomizeOnStart)
            MoveToRandomSpawnPoint(_currentHoop.transform);

        SubscribeToHoopHealth(_currentHoop);
    }

    void CacheSpawnPoints()
    {
        _spawnPoints.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            _spawnPoints.Add(child);
        }
    }

    void AttachOrSpawnInitialHoop()
    {
        HoopHealth[] hoopHealths = FindObjectsByType<HoopHealth>(FindObjectsSortMode.None);
        if (hoopHealths.Length > 0)
        {
            _currentHoop = hoopHealths[0].gameObject;
            if (removeExtraHoopsOnStart)
            {
                for (int i = 1; i < hoopHealths.Length; i++)
                {
                    if (hoopHealths[i] != null)
                        Destroy(hoopHealths[i].gameObject);
                }
            }
            return;
        }

        SpawnNewHoop();
    }

    void SpawnNewHoop()
    {
        if (hoopPrefab == null)
        {
            Debug.LogError("HoopSpawner: не назначен префаб кольца.", this);
            return;
        }

        Transform spawnPoint = GetRandomSpawnPoint();
        _currentHoop = Instantiate(hoopPrefab, spawnPoint.position, spawnPoint.rotation);
        SubscribeToHoopHealth(_currentHoop);
    }

    Transform GetRandomSpawnPoint()
    {
        int index = Random.Range(0, _spawnPoints.Count);
        return _spawnPoints[index];
    }

    void MoveToRandomSpawnPoint(Transform target)
    {
        Transform spawnPoint = GetRandomSpawnPoint();
        target.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    void SubscribeToHoopHealth(GameObject hoop)
    {
        if (hoop == null)
            return;

        if (!hoop.TryGetComponent(out HoopHealth hoopHealth))
        {
            Debug.LogError("HoopSpawner: у кольца отсутствует компонент HoopHealth.", hoop);
            return;
        }

        hoopHealth.HealthDepleted -= HandleHoopDepleted;
        hoopHealth.HealthDepleted += HandleHoopDepleted;
    }

    void HandleHoopDepleted(HoopHealth depletedHoop)
    {
        if (depletedHoop != null)
            Destroy(depletedHoop.gameObject);

        if (_respawnRoutine != null)
            StopCoroutine(_respawnRoutine);

        _respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        _respawnRoutine = null;
        SpawnNewHoop();
    }
}
