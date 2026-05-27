using System.Collections;
using UnityEngine;

/// <summary>
/// Воспроизведение клипов death/spawn на Animator кольца.
/// </summary>
[RequireComponent(typeof(Animator))]
public class HoopRelocateAnimator : MonoBehaviour
{
    static readonly int DeathHash = Animator.StringToHash("death");
    static readonly int SpawnHash = Animator.StringToHash("spawn");

    [SerializeField] Animator animator;
    [SerializeField] float deathClipLength = 0.5f;
    [SerializeField] float spawnClipLength = 0.5f;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public IEnumerator PlayDeathRoutine()
    {
        yield return PlayStateRoutine(DeathHash, "death", deathClipLength);
    }

    public IEnumerator PlaySpawnRoutine()
    {
        yield return PlayStateRoutine(SpawnHash, "spawn", spawnClipLength);
    }

    IEnumerator PlayStateRoutine(int stateHash, string stateName, float fallbackLength)
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(fallbackLength);
            yield break;
        }

        animator.Play(stateHash, 0, 0f);
        yield return null;

        float elapsed = 0f;
        float maxWait = Mathf.Max(fallbackLength * 2f, 0.1f);

        while (elapsed < maxWait)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == stateHash && info.normalizedTime >= 0.99f && !animator.IsInTransition(0))
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
