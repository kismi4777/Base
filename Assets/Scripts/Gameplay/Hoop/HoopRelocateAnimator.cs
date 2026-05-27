using System.Collections;
using UnityEngine;

/// <summary>
/// Воспроизведение клипов death/spawn. Позиция фиксируется вручную — в клипах зашиты старые координаты.
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

    public IEnumerator PlayDeathRoutine(Vector3 pinWorldPosition)
    {
        yield return PlayStateRoutine(DeathHash, deathClipLength, pinWorldPosition);
    }

    public IEnumerator PlaySpawnRoutine(Vector3 pinWorldPosition)
    {
        yield return PlayStateRoutine(SpawnHash, spawnClipLength, pinWorldPosition);
    }

    IEnumerator PlayStateRoutine(int stateHash, float fallbackLength, Vector3 pinWorldPosition)
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(fallbackLength);
            yield break;
        }

        PinWorldPosition(pinWorldPosition);
        animator.Play(stateHash, 0, 0f);
        yield return null;

        float elapsed = 0f;
        float maxWait = Mathf.Max(fallbackLength * 2f, 0.1f);

        while (elapsed < maxWait)
        {
            PinWorldPosition(pinWorldPosition);

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == stateHash && info.normalizedTime >= 0.99f && !animator.IsInTransition(0))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        PinWorldPosition(pinWorldPosition);
    }

    void PinWorldPosition(Vector3 pinWorldPosition)
    {
        transform.position = pinWorldPosition;
    }
}
