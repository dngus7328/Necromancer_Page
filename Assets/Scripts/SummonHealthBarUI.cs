using UnityEngine;

public class SummonHealthBar : MonoBehaviour
{
    [Header("대상")]
    [SerializeField]
    private SummonUnitBase summon;

    [Header("체력바")]
    [SerializeField]
    private Transform fill;

    [Tooltip("체력이 가득 찼을 때 Fill의 X 기준 폭")]
    [SerializeField]
    private float fullWidth = 0.8f;

    [Header("보호막 바")]
    [Tooltip("비워두면 보호막 바를 표시하지 않습니다.")]
    [SerializeField]
    private Transform shieldFill;

    private Vector3 originalFillScale;
    private Vector3 originalFillPosition;

    private Vector3 originalShieldScale;
    private Vector3 originalShieldPosition;

    private void Awake()
    {
        if (summon == null)
        {
            summon =
                GetComponentInParent<SummonUnitBase>();
        }

        if (fill != null)
        {
            originalFillScale =
                fill.localScale;

            originalFillPosition =
                fill.localPosition;
        }

        if (shieldFill != null)
        {
            originalShieldScale =
                shieldFill.localScale;

            originalShieldPosition =
                shieldFill.localPosition;

            SetBarRatio(
                shieldFill,
                originalShieldScale,
                originalShieldPosition,
                0f
            );
        }
    }

    private void LateUpdate()
    {
        UpdateHealthBar();
        UpdateShieldBar();
    }

    private void UpdateHealthBar()
    {
        if (summon == null ||
            fill == null)
        {
            return;
        }

        if (summon.MaxHealth <= 0f)
        {
            SetBarRatio(
                fill,
                originalFillScale,
                originalFillPosition,
                0f
            );

            return;
        }

        float ratio =
            Mathf.Clamp01(
                summon.CurrentHealth /
                summon.MaxHealth
            );

        SetBarRatio(
            fill,
            originalFillScale,
            originalFillPosition,
            ratio
        );
    }

    private void UpdateShieldBar()
    {
        if (summon == null ||
            shieldFill == null)
        {
            return;
        }

        SetBarRatio(
            shieldFill,
            originalShieldScale,
            originalShieldPosition,
            summon.ShieldRatio
        );
    }

    private void SetBarRatio(
        Transform target,
        Vector3 originalScale,
        Vector3 originalPosition,
        float ratio)
    {
        if (target == null)
        {
            return;
        }

        ratio =
            Mathf.Clamp01(
                ratio
            );

        Vector3 newScale =
            originalScale;

        newScale.x =
            originalScale.x *
            ratio;

        target.localScale =
            newScale;

        float lostWidth =
            fullWidth *
            (1f - ratio);

        Vector3 newPosition =
            originalPosition;

        newPosition.x =
            originalPosition.x -
            (lostWidth * 0.5f);

        target.localPosition =
            newPosition;
    }
}
