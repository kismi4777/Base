using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Показывает цифру урона над кольцом при успешном попадании мяча.
/// </summary>
public class HoopDamagePopup : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField] HoopHealth hoopHealth;
    [SerializeField] GameObject popupRoot;
    [SerializeField] TMP_Text damageLabel;
    [SerializeField] Animator damageAnimator;

    [Header("Анимация")]
    [SerializeField] string animationStateName = "New Animation";
    [SerializeField] float hideAfterSeconds = 0.6f;
    [SerializeField] string damageFormat = "{0}";

    Coroutine _hideRoutine;

    void Awake()
    {
        if (hoopHealth == null)
            hoopHealth = GetComponent<HoopHealth>();

        ResolvePopupReferences();

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (hoopHealth == null)
            return;

        hoopHealth.Scored -= HandleScored;
        hoopHealth.Scored += HandleScored;
        hoopHealth.PeriodicDamageApplied -= HandlePeriodicDamage;
        hoopHealth.PeriodicDamageApplied += HandlePeriodicDamage;
    }

    void OnDisable()
    {
        if (hoopHealth != null)
        {
            hoopHealth.Scored -= HandleScored;
            hoopHealth.PeriodicDamageApplied -= HandlePeriodicDamage;
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
    }

    void HandleScored(HoopHealth health)
    {
        if (health == null)
            return;

        Show(health.LastAppliedDamage);
    }

    void HandlePeriodicDamage(HoopHealth health, int damage)
    {
        if (health == null || damage <= 0)
            return;

        Show(damage);
    }

    void Show(int damage)
    {
        if (damageLabel == null || popupRoot == null)
            return;

        damageLabel.text = string.Format(damageFormat, damage);
        popupRoot.SetActive(true);

        if (damageAnimator != null)
            damageAnimator.Play(animationStateName, 0, 0f);

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideAfterSeconds);
        popupRoot.SetActive(false);
        _hideRoutine = null;
    }

    void ResolvePopupReferences()
    {
        if (popupRoot == null)
        {
            Transform textTransform = transform.Find("Canvas/Text (TMP)");
            if (textTransform != null)
                popupRoot = textTransform.gameObject;
        }

        if (damageLabel == null && popupRoot != null)
            damageLabel = popupRoot.GetComponent<TMP_Text>();

        if (damageAnimator == null && popupRoot != null)
            damageAnimator = popupRoot.GetComponent<Animator>();
    }
}
