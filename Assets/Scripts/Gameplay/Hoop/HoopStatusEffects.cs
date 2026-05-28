using System.Collections;
using UnityEngine;

/// <summary>Периодические эффекты на кольце: горение, яд, антилечение.</summary>
public sealed class HoopStatusEffects : MonoBehaviour
{
    [SerializeField] HoopHealth hoopHealth;
    [Tooltip("Опционально: визуал огня на кольце (ищется автоматически по имени Fire).")]
    [SerializeField] GameObject burnVisual;

    Coroutine _burnRoutine;
    Coroutine _poisonRoutine;
    float _antiHealUntil;

    public bool HasAntiHeal => Time.time < _antiHealUntil;
    public bool IsBurning => _burnRoutine != null;

    void Awake()
    {
        if (hoopHealth == null)
            hoopHealth = GetComponent<HoopHealth>();

        if (burnVisual == null)
            burnVisual = FindBurnVisualRoot();
    }

    public void ApplyAntiHeal(float durationSeconds)
    {
        if (durationSeconds <= 0f)
            return;

        _antiHealUntil = Mathf.Max(_antiHealUntil, Time.time + durationSeconds);
    }

    public void ApplyBurn(int damagePerTick, float tickInterval, float durationSeconds)
    {
        if (damagePerTick <= 0 || durationSeconds <= 0f || hoopHealth == null)
            return;

        if (_burnRoutine != null)
            StopCoroutine(_burnRoutine);

        SetBurnVisualActive(true);
        _burnRoutine = StartCoroutine(BurnRoutine(damagePerTick, tickInterval, durationSeconds));
    }

    public void ApplyPoison(int damagePerTick, float tickInterval, float durationSeconds)
    {
        if (damagePerTick <= 0 || durationSeconds <= 0f || hoopHealth == null)
            return;

        if (_poisonRoutine != null)
            StopCoroutine(_poisonRoutine);

        _poisonRoutine = StartCoroutine(PoisonRoutine(damagePerTick, tickInterval, durationSeconds));
    }

    IEnumerator BurnRoutine(int damagePerTick, float tickInterval, float durationSeconds)
    {
        float endTime = Time.time + durationSeconds;
        float interval = Mathf.Max(0.05f, tickInterval);
        var wait = new WaitForSeconds(interval);

        while (Time.time < endTime && hoopHealth != null)
        {
            hoopHealth.ApplyPeriodicDamage(damagePerTick);

            if (Time.time >= endTime)
                break;

            yield return wait;
        }

        SetBurnVisualActive(false);
        _burnRoutine = null;
    }

    IEnumerator PoisonRoutine(int damagePerTick, float tickInterval, float durationSeconds)
    {
        float endTime = Time.time + durationSeconds;
        var wait = new WaitForSeconds(Mathf.Max(0.05f, tickInterval));

        while (Time.time < endTime && hoopHealth != null)
        {
            hoopHealth.ApplyPeriodicDamage(damagePerTick);

            if (Time.time >= endTime)
                break;

            yield return wait;
        }

        _poisonRoutine = null;
    }

    GameObject FindBurnVisualRoot()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == transform)
                continue;

            string name = child.name;
            if (name.IndexOf("Fire", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return child.gameObject;
        }

        return null;
    }

    void SetBurnVisualActive(bool active)
    {
        if (burnVisual != null)
            burnVisual.SetActive(active);
    }
}
