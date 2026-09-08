using UnityEngine;

public class SummonHealthBar : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private SummonUnitBase summon;

    [Header("체력바")]
    [SerializeField] private Transform fill;

    [Tooltip("체력이 가득 찼을 때 Fill의 X 크기")]
    [SerializeField] private float fullWidth = 0.8f;

    private Vector3 originalFillScale;
    private Vector3 originalFillPosition;

    private void Awake()
    {
        if (summon == null)
        {
            summon =
                GetComponentInParent<SummonUnitBase>();
        }

        if (fill != null)
        {
            originalFillScale = fill.localScale;
            originalFillPosition = fill.localPosition;
        }
    }

    private void LateUpdate()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (summon == null || fill == null)
        {
            return;
        }

        if (summon.MaxHealth <= 0f)
        {
            return;
        }

        float ratio =
            Mathf.Clamp01(
                summon.CurrentHealth /
                summon.MaxHealth
            );

        Vector3 newScale =
            originalFillScale;

        newScale.x =
            originalFillScale.x * ratio;

        fill.localScale = newScale;

        // 가운데 Pivot이어도
        // 오른쪽 부분만 줄어든 것처럼 보이도록 위치 보정
        float lostWidth =
            fullWidth * (1f - ratio);

        Vector3 newPosition =
            originalFillPosition;

        newPosition.x =
            originalFillPosition.x -
            (lostWidth * 0.5f);

        fill.localPosition =
            newPosition;
    }
}