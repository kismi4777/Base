using System.Collections;
using UnityEngine;

/// <summary>
/// После первого кадра убирает совпадение индексов спавна двух колец (если порядок Start дал коллизию).
/// Повесьте на любой объект в сцене PvP и укажите оба HoopSpawner (у каждого в инспекторе уже задан spawnPeer).
/// </summary>
public sealed class PvPHoopSpawnOverlapFixer : MonoBehaviour
{
    [SerializeField] HoopSpawner primarySpawner;
    [SerializeField] HoopSpawner secondarySpawner;

    IEnumerator Start()
    {
        yield return null;

        if (primarySpawner == null || secondarySpawner == null)
            yield break;

        if (primarySpawner.CurrentSpawnIndex != secondarySpawner.CurrentSpawnIndex)
            yield break;

        secondarySpawner.InstantRelocateIfSameIndexAsPeer();
    }
}
