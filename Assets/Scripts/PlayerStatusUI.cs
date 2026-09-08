using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("리벨")]
    [SerializeField] private RibelHealth ribelHealth;

    [Header("HP")]
    [SerializeField] private Image healthFill;

    [Header("마나")]
    [SerializeField] private Image manaFill;

    [Header("게이지 부드러움")]
    [Tooltip("값이 클수록 게이지가 더 빠르게 따라갑니다.")]
    [SerializeField] private float healthSmoothSpeed = 6f;

    [Tooltip("값이 클수록 게이지가 더 빠르게 따라갑니다.")]
    [SerializeField] private float manaSmoothSpeed = 6f;

    private float targetHealthRatio = 1f;
    private float targetManaRatio = 1f;

    private void Start()
    {
        InitializeGaugeValues();
    }

    private void Update()
    {
        UpdateTargetValues();
        UpdateGaugeAnimation();
    }

    // =========================================================
    // 시작할 때 현재 값으로 즉시 맞춤
    // =========================================================

    private void InitializeGaugeValues()
    {
        if (ribelHealth != null &&
            healthFill != null &&
            ribelHealth.MaxHealth > 0f)
        {
            targetHealthRatio =
                Mathf.Clamp01(
                    ribelHealth.CurrentHealth /
                    ribelHealth.MaxHealth
                );

            healthFill.fillAmount =
                targetHealthRatio;
        }

        if (manaFill != null &&
            SummonManager.Instance != null &&
            SummonManager.Instance.MaxMana > 0f)
        {
            targetManaRatio =
                Mathf.Clamp01(
                    SummonManager.Instance.CurrentMana /
                    SummonManager.Instance.MaxMana
                );

            manaFill.fillAmount =
                targetManaRatio;
        }
    }

    // =========================================================
    // 실제 값 읽기
    // =========================================================

    private void UpdateTargetValues()
    {
        if (ribelHealth != null &&
            ribelHealth.MaxHealth > 0f)
        {
            targetHealthRatio =
                Mathf.Clamp01(
                    ribelHealth.CurrentHealth /
                    ribelHealth.MaxHealth
                );
        }

        if (SummonManager.Instance != null &&
            SummonManager.Instance.MaxMana > 0f)
        {
            targetManaRatio =
                Mathf.Clamp01(
                    SummonManager.Instance.CurrentMana /
                    SummonManager.Instance.MaxMana
                );
        }
    }

    // =========================================================
    // 부드럽게 실제 게이지 이동
    // =========================================================

    private void UpdateGaugeAnimation()
    {
        if (healthFill != null)
        {
            healthFill.fillAmount =
                Mathf.Lerp(
                    healthFill.fillAmount,
                    targetHealthRatio,
                    1f - Mathf.Exp(
                        -healthSmoothSpeed * Time.deltaTime
                    )
                );
        }

        if (manaFill != null)
        {
            manaFill.fillAmount =
                Mathf.Lerp(
                    manaFill.fillAmount,
                    targetManaRatio,
                    1f - Mathf.Exp(
                        -manaSmoothSpeed * Time.deltaTime
                    )
                );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        healthSmoothSpeed =
            Mathf.Max(0.1f, healthSmoothSpeed);

        manaSmoothSpeed =
            Mathf.Max(0.1f, manaSmoothSpeed);
    }
#endif
}