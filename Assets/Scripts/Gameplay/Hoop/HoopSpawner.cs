using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Держит одно кольцо (тот же объект/префаб) и переносит его в случайную точку спавна после гола, сохраняя текущее HP.
/// </summary>
public class HoopSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] GameObject hoopPrefab;
    [SerializeField] float relocateDelay = 0.35f;
    [SerializeField] bool randomizeOnStart = true;
    [SerializeField] bool removeExtraHoopsOnStart = true;
    [SerializeField] bool avoidSameSpawnPoint = true;

    readonly List<Transform> _spawnPoints = new();
    GameObject _currentHoop;
    int _currentSpawnIndex = -1;
    Coroutine _relocateRoutine;

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
            PlaceHoopAtSpawnPoint(_currentHoop.transform, PickRandomSpawnIndex(-1));

        SubscribeToHoopHealth(_currentHoop);
    }

    void OnDestroy()
    {
        UnsubscribeFromHoopHealth(_currentHoop);
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

        if (hoopPrefab == null)
        {
            Debug.LogError("HoopSpawner: не назначен префаб кольца.", this);
            return;
        }

        int spawnIndex = PickRandomSpawnIndex(-1);
        Transform spawnPoint = _spawnPoints[spawnIndex];
        _currentHoop = Instantiate(hoopPrefab, spawnPoint.position, spawnPoint.rotation);
        _currentSpawnIndex = spawnIndex;
    }

    void SubscribeToHoopHealth(GameObject hoop)
    {
        if (hoop == null || !hoop.TryGetComponent(out HoopHealth hoopHealth))
        {
            Debug.LogError("HoopSpawner: у кольца отсутствует компонент HoopHealth.", hoop);
            return;
        }

        hoopHealth.Scored -= HandleHoopScored;
        hoopHealth.Scored += HandleHoopScored;
    }

    void UnsubscribeFromHoopHealth(GameObject hoop)
    {
        if (hoop == null || !hoop.TryGetComponent(out HoopHealth hoopHealth))
            return;

        hoopHealth.Scored -= HandleHoopScored;
    }

    void HandleHoopScored(HoopHealth hoopHealth)
    {
        if (hoopHealth == null || _spawnPoints.Count == 0)
            return;

        if (_relocateRoutine != null)
            StopCoroutine(_relocateRoutine);

        _relocateRoutine = StartCoroutine(RelocateAfterScoreRoutine(hoopHealth));
    }

    IEnumerator RelocateAfterScoreRoutine(HoopHealth hoopHealth)
    {
        GameObject hoopObject = hoopHealth.gameObject;
        hoopObject.SetActive(false);

        yield return new WaitForSeconds(relocateDelay);

        int nextIndex = PickRandomSpawnIndex(avoidSameSpawnPoint ? _currentSpawnIndex : -1);
        PlaceHoopAtSpawnPoint(hoopObject.transform, nextIndex);

        hoopHealth.ClearShotTrackingForRelocate();
        if (hoopObject.TryGetComponent(out HoopNetReaction netReaction))
            netReaction.ResetShot();

        hoopObject.SetActive(true);
        _relocateRoutine = null;
    }

    void PlaceHoopAtSpawnPoint(Transform hoopTransform, int spawnIndex)
    {
        Transform spawnPoint = _spawnPoints[spawnIndex];
        hoopTransform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        _currentSpawnIndex = spawnIndex;
    }

    int PickRandomSpawnIndex(int excludedIndex)
    {
        if (_spawnPoints.Count == 1)
            return 0;

        if (!avoidSameSpawnPoint || excludedIndex < 0 || _spawnPoints.Count <= 1)
            return Random.Range(0, _spawnPoints.Count);

        int index = Random.Range(0, _spawnPoints.Count - 1);
        if (index >= excludedIndex)
            index++;

        return index;
    }
}
