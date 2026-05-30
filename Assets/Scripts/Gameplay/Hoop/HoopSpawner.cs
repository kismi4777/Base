using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Держит одно кольцо и переносит его после гола: полоска HP → death → новая точка → spawn.
/// </summary>
public class HoopSpawner : MonoBehaviour
{
    const string SpawnPointNamePrefix = "Point_";

    [Header("Настройки спавна")]
    [SerializeField] GameObject hoopPrefab;
    [SerializeField] bool randomizeOnStart = true;
    [SerializeField] bool removeExtraHoopsOnStart = true;
    [SerializeField] bool avoidSameSpawnPoint = true;
    [SerializeField] bool playSpawnOnStart = false;

    [Header("Спавн рядом с кольцом соперника")]
    [Tooltip("Если задано, кольцо не появится на той же точке, что и у другого HoopSpawner.")]
    [SerializeField] HoopSpawner spawnPeer;
    [SerializeField] bool excludePeerSpawnPoint = true;
    [Tooltip("Не спавнить в той же вертикальной колонке (одинаковый X), что и кольцо соперника — нельзя оказаться над/под ним.")]
    [SerializeField] bool excludePeerSameColumn = true;
    [SerializeField] float columnWorldXTolerance = 0.25f;
    [SerializeField] float columnMinVerticalSeparation = 0.3f;

    [Header("PvP: чьё кольцо спавнит этот спавнер")]
    [SerializeField] bool configurePvPFilterOnSpawn = true;
    [SerializeField] bool usePvPTeamFilterOnSpawn = true;
    [SerializeField] PvPTeam defendedTeamOnSpawn = PvPTeam.Player;

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
            Debug.LogError("HoopSpawner: не найдены точки спавна (дочерние Point_*).", this);
            return;
        }

        AttachOrSpawnInitialHoop();
        if (_currentHoop == null)
            return;

        if (randomizeOnStart)
        {
            int spawnIndex = PickRandomSpawnIndex(-1);
            PlaceHoopAtSpawnPoint(_currentHoop.transform, spawnIndex);
        }
        else if (_currentSpawnIndex < 0)
        {
            _currentSpawnIndex = FindClosestSpawnIndex(_currentHoop.transform.position);
        }

        SubscribeToHoopHealth(_currentHoop);

        if (playSpawnOnStart && _currentHoop.TryGetComponent(out HoopRelocateAnimator relocateAnimator))
        {
            Vector3 pin = _spawnPoints[_currentSpawnIndex].position;
            StartCoroutine(relocateAnimator.PlaySpawnRoutine(pin));
        }
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
            if (!child.name.StartsWith(SpawnPointNamePrefix))
                continue;

            _spawnPoints.Add(child);
        }

        _spawnPoints.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    }

    void AttachOrSpawnInitialHoop()
    {
        if (hoopPrefab != null)
        {
            int spawnIndex = PickRandomSpawnIndex(-1);
            Transform spawnPoint = _spawnPoints[spawnIndex];
            _currentHoop = Instantiate(hoopPrefab, spawnPoint.position, spawnPoint.rotation);
            ConfigureSpawnedHoop(_currentHoop);
            _currentSpawnIndex = spawnIndex;
            return;
        }

        HoopHealth[] hoopHealths = FindObjectsByType<HoopHealth>(FindObjectsSortMode.None);
        if (hoopHealths.Length > 0)
        {
            _currentHoop = hoopHealths[0].gameObject;
            ConfigureSpawnedHoop(_currentHoop);
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

        Debug.LogError("HoopSpawner: не назначен префаб кольца (hoopPrefab).", this);
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
        hoopHealth.SetRelocateBusy(true);

        if (!hoopObject.TryGetComponent(out HoopRelocateAnimator relocateAnimator))
            relocateAnimator = hoopObject.AddComponent<HoopRelocateAnimator>();

        yield return hoopHealth.WaitForHealthBarAfterDamage();

        Vector3 deathPin = hoopObject.transform.position;
        yield return relocateAnimator.PlayDeathRoutine(deathPin);

        int nextIndex = PickRandomSpawnIndex(avoidSameSpawnPoint ? _currentSpawnIndex : -1);
        PlaceHoopAtSpawnPoint(hoopObject.transform, nextIndex);
        Vector3 spawnPin = _spawnPoints[nextIndex].position;

        hoopHealth.ClearShotTrackingForRelocate();
        if (hoopObject.TryGetComponent(out HoopNetReaction netReaction))
            netReaction.ResetShot();

        yield return relocateAnimator.PlaySpawnRoutine(spawnPin);

        hoopHealth.SetRelocateBusy(false);
        _relocateRoutine = null;
    }

    /// <summary>
    /// Индекс текущей точки спавна (для исключения совпадения с кольцом соперника).
    /// </summary>
    public int CurrentSpawnIndex => _currentSpawnIndex;

    /// <summary>
    /// Мгновенно переносит кольцо на другую точку, если совпала с точкой соперника (без анимации смерти/появления).
    /// </summary>
    public void InstantRelocateIfSameIndexAsPeer()
    {
        if (spawnPeer == null || _currentHoop == null || _spawnPoints.Count <= 1)
            return;

        if (!HasOverlappingPeerSpawn())
            return;

        int next = PickRandomSpawnIndex(-1);
        PlaceHoopAtSpawnPoint(_currentHoop.transform, next);
    }

    /// <summary>
    /// Совпадает индекс точки с соперником или та же вертикальная колонка (над/под чужим кольцом).
    /// </summary>
    public bool HasOverlappingPeerSpawn()
    {
        if (spawnPeer == null || _currentSpawnIndex < 0 || spawnPeer.CurrentSpawnIndex < 0)
            return false;

        if (excludePeerSpawnPoint && _currentSpawnIndex == spawnPeer.CurrentSpawnIndex)
            return true;

        if (!excludePeerSameColumn)
            return false;

        return IsVerticallyStackedWith(
            _spawnPoints[_currentSpawnIndex].position,
            spawnPeer.GetSpawnWorldPosition(spawnPeer.CurrentSpawnIndex));
    }

    void PlaceHoopAtSpawnPoint(Transform hoopTransform, int spawnIndex)
    {
        Transform spawnPoint = _spawnPoints[spawnIndex];
        hoopTransform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        _currentSpawnIndex = spawnIndex;
    }

    int PickRandomSpawnIndex(int excludedIndex)
    {
        int count = _spawnPoints.Count;
        if (count <= 1)
            return 0;

        int peerExcluded = spawnPeer != null && excludePeerSpawnPoint ? spawnPeer.CurrentSpawnIndex : -1;
        Vector3 peerWorldPosition = spawnPeer != null && excludePeerSameColumn && spawnPeer.CurrentSpawnIndex >= 0
            ? spawnPeer.GetSpawnWorldPosition(spawnPeer.CurrentSpawnIndex)
            : default;
        bool blockPeerColumn = spawnPeer != null && excludePeerSameColumn && spawnPeer.CurrentSpawnIndex >= 0;
        bool excludeSelfPrevious = avoidSameSpawnPoint && excludedIndex >= 0;

        int start = Random.Range(0, count);
        for (int offset = 0; offset < count; offset++)
        {
            int candidate = (start + offset) % count;
            if (excludeSelfPrevious && candidate == excludedIndex)
                continue;
            if (peerExcluded >= 0 && candidate == peerExcluded)
                continue;
            if (blockPeerColumn && IsVerticallyStackedWith(_spawnPoints[candidate].position, peerWorldPosition))
                continue;
            return candidate;
        }

        for (int i = 0; i < count; i++)
        {
            if (excludeSelfPrevious && i == excludedIndex)
                continue;
            if (peerExcluded >= 0 && i == peerExcluded)
                continue;
            if (blockPeerColumn && IsVerticallyStackedWith(_spawnPoints[i].position, peerWorldPosition))
                continue;
            return i;
        }

        return 0;
    }

    Vector3 GetSpawnWorldPosition(int spawnIndex)
    {
        return _spawnPoints[spawnIndex].position;
    }

    bool IsVerticallyStackedWith(Vector3 candidateWorld, Vector3 otherWorld)
    {
        if (Mathf.Abs(candidateWorld.x - otherWorld.x) > columnWorldXTolerance)
            return false;

        return Mathf.Abs(candidateWorld.y - otherWorld.y) >= columnMinVerticalSeparation;
    }

    int FindClosestSpawnIndex(Vector3 worldPosition)
    {
        int closestIndex = 0;
        float closestSqr = float.MaxValue;

        for (int i = 0; i < _spawnPoints.Count; i++)
        {
            float sqr = (_spawnPoints[i].position - worldPosition).sqrMagnitude;
            if (sqr >= closestSqr)
                continue;

            closestSqr = sqr;
            closestIndex = i;
        }

        return closestIndex;
    }

    void ConfigureSpawnedHoop(GameObject hoop)
    {
        if (!configurePvPFilterOnSpawn || hoop == null)
            return;

        if (!hoop.TryGetComponent(out HoopHealth hoopHealth))
            return;

        hoopHealth.ConfigurePvPTeamFilter(usePvPTeamFilterOnSpawn, defendedTeamOnSpawn);
    }
}
