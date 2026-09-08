using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private EnemyUnitBase enemy;

    [Header("체력바")]
    [SerializeField] private Transform fill;

    [Tooltip("체력이 가득 찼을 때 Fill의 X 크기")]
    [SerializeField] private float fullWidth = 0.8f;

    private Vector3 originalFillScale;
    private Vector3 originalFillPosition;

    private void Awake()
    {
        if (enemy == null)
        {
            enemy =
                GetComponentInParent<EnemyUnitBase>();
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
        if (enemy == null || fill == null)
        {
            return;
        }

        float ratio = enemy.HealthRatio;

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