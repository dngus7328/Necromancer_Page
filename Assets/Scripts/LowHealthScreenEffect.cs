using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LowHealthScreenEffect : MonoBehaviour
{
    // =========================================================
    // 체력 기준
    // =========================================================

    [Header("체력 기준")]

    [Tooltip("이 체력 비율 이하부터 화면 경고 표시")]
    [Range(0.01f, 1f)]
    [SerializeField]
    private float warningThreshold = 0.30f;

    [Tooltip("이 체력 비율 이하부터 강한 위험 상태")]
    [Range(0.01f, 1f)]
    [SerializeField]
    private float criticalThreshold = 0.15f;

    // =========================================================
    // 투명도
    // =========================================================

    [Header("경고 강도")]

    [Tooltip("30% 근처일 때 기본 투명도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float normalAlpha = 0.16f;

    [Tooltip("체력이 거의 없을 때 최대 투명도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float criticalAlpha = 0.42f;

    // =========================================================
    // 맥박
    // =========================================================

    [Header("맥박")]

    [Tooltip("일반 위험 상태 맥박 속도")]
    [SerializeField]
    private float normalPulseSpeed = 2.2f;

    [Tooltip("치명적 상태 맥박 속도")]
    [SerializeField]
    private float criticalPulseSpeed = 4.2f;

    [Tooltip("맥박으로 추가되는 투명도")]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float pulseStrength = 0.10f;

    // =========================================================
    // 페이드
    // =========================================================

    [Header("등장 / 사라짐")]

    [Tooltip("체력 상태가 변할 때 투명도 변화 속도")]
    [SerializeField]
    private float fadeSpeed = 5f;

    // =========================================================
    // Runtime
    // =========================================================

    private Image overlayImage;

    private float currentHealthRatio = 1f;

    private float currentAlpha;

    private void Awake()
    {
        overlayImage =
            GetComponent<Image>();

        // 화면 전체를 덮지만 클릭은 통과
        overlayImage.raycastTarget =
            false;

        Color c =
            overlayImage.color;

        c.a =
            0f;

        overlayImage.color =
            c;
    }

    private void Update()
    {
        UpdateEffect();
    }

    // =========================================================
    // 체력 전달
    // =========================================================

    public void SetHealth(
        float currentHealth,
        float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            currentHealthRatio =
                0f;

            return;
        }

        currentHealthRatio =
            Mathf.Clamp01(
                currentHealth /
                maxHealth
            );
    }

    // =========================================================
    // 효과 계산
    // =========================================================

    private void UpdateEffect()
    {
        float targetAlpha =
            0f;

        // -----------------------------------------------------
        // 안전 상태
        // -----------------------------------------------------

        if (currentHealthRatio >
            warningThreshold)
        {
            targetAlpha =
                0f;
        }

        // -----------------------------------------------------
        // 위험 상태
        // -----------------------------------------------------

        else
        {
            float dangerT =
                Mathf.InverseLerp(
                    warningThreshold,
                    0f,
                    currentHealthRatio
                );

            float baseAlpha =
                Mathf.Lerp(
                    normalAlpha,
                    criticalAlpha,
                    dangerT
                );

            bool critical =
                currentHealthRatio <=
                criticalThreshold;

            float pulseSpeed =
                critical
                    ? criticalPulseSpeed
                    : normalPulseSpeed;

            float pulse =
                (
                    Mathf.Sin(
                        Time.unscaledTime *
                        pulseSpeed
                    ) +
                    1f
                ) *
                0.5f;

            float currentPulseStrength =
                critical
                    ? pulseStrength * 1.5f
                    : pulseStrength;

            targetAlpha =
                baseAlpha +
                pulse *
                currentPulseStrength;
        }

        // -----------------------------------------------------
        // 자연스럽게 등장 / 사라짐
        // -----------------------------------------------------

        currentAlpha =
            Mathf.MoveTowards(
                currentAlpha,
                targetAlpha,
                fadeSpeed *
                Time.unscaledDeltaTime
            );

        Color color =
            overlayImage.color;

        color.a =
            Mathf.Clamp01(
                currentAlpha
            );

        overlayImage.color =
            color;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        warningThreshold =
            Mathf.Clamp01(
                warningThreshold
            );

        criticalThreshold =
            Mathf.Clamp(
                criticalThreshold,
                0.01f,
                warningThreshold
            );

        normalPulseSpeed =
            Mathf.Max(
                0.01f,
                normalPulseSpeed
            );

        criticalPulseSpeed =
            Mathf.Max(
                0.01f,
                criticalPulseSpeed
            );

        fadeSpeed =
            Mathf.Max(
                0.01f,
                fadeSpeed
            );
    }

#endif
}