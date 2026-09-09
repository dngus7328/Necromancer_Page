using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortSprite : MonoBehaviour
{
    [Header("정렬")]
    [Tooltip("Y 위치를 Sorting Order로 변환할 배율")]
    [SerializeField] private int sortingPrecision = 100;

    [Tooltip("같은 위치에서 추가로 더할 값")]
    [SerializeField] private int sortingOffset = 0;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingOrder =
            Mathf.RoundToInt(
                -transform.position.y *
                sortingPrecision
            ) +
            sortingOffset;
    }
}