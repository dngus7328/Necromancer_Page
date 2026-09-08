using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SummonSlotVisual : MonoBehaviour
{
    [Header("슬롯 번호")]
    [Range(0, 7)]
    [SerializeField] private int slotIndex;

    [Header("카드 UI")]
    [Tooltip("실제로 위로 솟아오를 카드 부모")]
    [SerializeField] private RectTransform cardRoot;

    [Tooltip("카드 일러스트")]
    [SerializeField] private Image cardImage;

    [Tooltip("카드가 없을 때 보이는 기존 빈 슬롯")]
    [SerializeField] private GameObject emptyVisual;

    [Tooltip("카드 뒤에서 빛나는 이미지")]
    [SerializeField] private Image glowImage;

    [Header("선택 연출")]
    [Tooltip("선택 시 위로 올라가는 거리")]
    [SerializeField] private float riseDistance = 14f;

    [Tooltip("선택 시 확대")]
    [SerializeField] private float selectedScale = 1.06f;

    [Tooltip("올라가고 내려오는 속도")]
    [SerializeField] private float moveSpeed = 14f;

    [Header("Glow")]
    [SerializeField] private float glowMinAlpha = 0.18f;
    [SerializeField] private float glowMaxAlpha = 0.6f;
    [SerializeField] private float glowPulseSpeed = 2.5f;

    private Vector2 originalPosition;
    private Vector3 originalScale;

    private bool hasCard;
    private bool isSelected;

    private bool initialized;

    private Coroutine moveRoutine;

    public int SlotIndex => slotIndex;
    public bool HasCard => hasCard;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();

        // 다시 활성화될 때 현재 선택 상태에 맞춰 즉시 적용
        ApplySelectionInstant();
    }

    private void Update()
    {
        UpdateGlow();
    }

    // =========================================================
    // 초기화
    // =========================================================

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (cardRoot == null)
        {
            cardRoot = GetComponent<RectTransform>();
        }

        if (cardRoot != null)
        {
            originalPosition =
                cardRoot.anchoredPosition;

            originalScale =
                cardRoot.localScale;
        }

        hasCard =
            cardImage != null &&
            cardImage.sprite != null;

        ApplyCardState();

        SetGlowAlpha(0f);

        initialized = true;
    }

    // =========================================================
    // 카드 지정
    // =========================================================

    public void SetCard(Sprite cardSprite)
    {
        Initialize();

        if (cardImage != null)
        {
            cardImage.sprite =
                cardSprite;
        }

        hasCard =
            cardSprite != null;

        ApplyCardState();

        if (!hasCard)
        {
            SetSelected(false);
        }
    }

    // =========================================================
    // 카드 있음 / 없음
    // =========================================================

    private void ApplyCardState()
    {
        if (cardImage != null)
        {
            cardImage.enabled =
                hasCard;
        }

        if (emptyVisual != null)
        {
            emptyVisual.SetActive(
                !hasCard
            );
        }
    }

    // =========================================================
    // 선택
    // =========================================================

    public void SetSelected(bool selected)
    {
        // 비활성 오브젝트에서 StartCoroutine을 호출하면
        // Unity 오류가 발생하므로 상태만 저장
        if (!gameObject.activeInHierarchy)
        {
            isSelected = selected;
            return;
        }

        Initialize();

        if (!hasCard)
        {
            selected = false;
        }

        isSelected =
            selected;

        if (cardRoot == null)
        {
            return;
        }

        Vector2 targetPosition =
            originalPosition;

        Vector3 targetScale =
            originalScale;

        if (isSelected)
        {
            targetPosition.y +=
                riseDistance;

            targetScale =
                originalScale *
                selectedScale;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(
                moveRoutine
            );
        }

        moveRoutine =
            StartCoroutine(
                AnimateCard(
                    targetPosition,
                    targetScale
                )
            );

        if (!isSelected)
        {
            SetGlowAlpha(0f);
        }
    }

    // =========================================================
    // 활성화 순간 상태 적용
    // =========================================================

    private void ApplySelectionInstant()
    {
        if (!initialized ||
            cardRoot == null)
        {
            return;
        }

        Vector2 position =
            originalPosition;

        Vector3 scale =
            originalScale;

        if (isSelected &&
            hasCard)
        {
            position.y +=
                riseDistance;

            scale =
                originalScale *
                selectedScale;
        }

        cardRoot.anchoredPosition =
            position;

        cardRoot.localScale =
            scale;

        if (!isSelected)
        {
            SetGlowAlpha(0f);
        }
    }

    // =========================================================
    // 소환 성공
    // =========================================================

    public void PlaySummonReturn()
    {
        SetSelected(false);
    }

    // =========================================================
    // 선택 이동
    // =========================================================

    private IEnumerator AnimateCard(
        Vector2 targetPosition,
        Vector3 targetScale)
    {
        if (cardRoot == null)
        {
            yield break;
        }

        while (true)
        {
            cardRoot.anchoredPosition =
                Vector2.Lerp(
                    cardRoot.anchoredPosition,
                    targetPosition,
                    1f - Mathf.Exp(
                        -moveSpeed *
                        Time.unscaledDeltaTime
                    )
                );

            cardRoot.localScale =
                Vector3.Lerp(
                    cardRoot.localScale,
                    targetScale,
                    1f - Mathf.Exp(
                        -moveSpeed *
                        Time.unscaledDeltaTime
                    )
                );

            bool positionFinished =
                Vector2.Distance(
                    cardRoot.anchoredPosition,
                    targetPosition
                ) < 0.1f;

            bool scaleFinished =
                Vector3.Distance(
                    cardRoot.localScale,
                    targetScale
                ) < 0.001f;

            if (positionFinished &&
                scaleFinished)
            {
                cardRoot.anchoredPosition =
                    targetPosition;

                cardRoot.localScale =
                    targetScale;

                break;
            }

            yield return null;
        }

        moveRoutine = null;
    }

    // =========================================================
    // Glow
    // =========================================================

    private void UpdateGlow()
    {
        if (!isSelected ||
            !hasCard ||
            glowImage == null)
        {
            return;
        }

        float pulse =
            Mathf.Sin(
                Time.unscaledTime *
                glowPulseSpeed *
                Mathf.PI *
                2f
            );

        pulse =
            (pulse + 1f) *
            0.5f;

        float alpha =
            Mathf.Lerp(
                glowMinAlpha,
                glowMaxAlpha,
                pulse
            );

        SetGlowAlpha(alpha);
    }

    private void SetGlowAlpha(float alpha)
    {
        if (glowImage == null)
        {
            return;
        }

        Color color =
            glowImage.color;

        color.a =
            alpha;

        glowImage.color =
            color;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        riseDistance =
            Mathf.Max(
                0f,
                riseDistance
            );

        selectedScale =
            Mathf.Max(
                1f,
                selectedScale
            );

        moveSpeed =
            Mathf.Max(
                0.1f,
                moveSpeed
            );

        glowMinAlpha =
            Mathf.Clamp01(
                glowMinAlpha
            );

        glowMaxAlpha =
            Mathf.Clamp01(
                glowMaxAlpha
            );

        if (glowMaxAlpha <
            glowMinAlpha)
        {
            glowMaxAlpha =
                glowMinAlpha;
        }

        glowPulseSpeed =
            Mathf.Max(
                0.1f,
                glowPulseSpeed
            );
    }
#endif
}