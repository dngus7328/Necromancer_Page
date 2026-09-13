using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SummonCapacityUI : MonoBehaviour
{
    // =========================================================
    // 용량 숫자
    // =========================================================

    [Header("소환 용량 숫자")]
    [Tooltip("용량을 표시할 TMP 텍스트")]
    [SerializeField]
    private TMP_Text capacityText;

    // =========================================================
    // 기본 색상
    // =========================================================

    [Header("색상")]
    [SerializeField]
    private Color normalColor =
        Color.white;

    [Tooltip("용량이 가득 찼을 때 숫자 색")]
    [SerializeField]
    private Color fullColor =
        new Color(
            1f,
            0.45f,
            0.45f,
            1f
        );

    [Tooltip("소환 불가 피드백 순간 색")]
    [SerializeField]
    private Color blockedFlashColor =
        new Color(
            1f,
            0.15f,
            0.15f,
            1f
        );

    // =========================================================
    // 피드백 대상
    // =========================================================

    [Header("가득 찼을 때 피드백")]

    [Tooltip(
        "흔들릴 UI의 RectTransform. " +
        "비워두면 이 오브젝트 자체를 사용합니다."
    )]
    [SerializeField]
    private RectTransform feedbackRoot;

    [Tooltip("좌우 흔들림 크기")]
    [SerializeField]
    private float shakeDistance = 8f;

    [Tooltip("흔들림 시간")]
    [SerializeField]
    private float shakeDuration = 0.20f;

    [Tooltip("흔들림 횟수")]
    [SerializeField]
    private int shakeCount = 5;

    [Tooltip("순간적으로 커지는 크기")]
    [SerializeField]
    private float punchScale = 1.08f;

    // =========================================================
    // Runtime
    // =========================================================

    private int lastCurrentCapacity = -1;
    private int lastMaxCapacity = -1;

    private Vector2 originalAnchoredPosition;
    private Vector3 originalScale;

    private Coroutine feedbackCoroutine;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        if (feedbackRoot == null)
        {
            feedbackRoot =
                transform as RectTransform;
        }

        if (feedbackRoot != null)
        {
            originalAnchoredPosition =
                feedbackRoot.anchoredPosition;

            originalScale =
                feedbackRoot.localScale;
        }
    }

    private void Start()
    {
        RefreshCapacityText(
            true
        );
    }

    private void Update()
    {
        RefreshCapacityText(
            false
        );

        CheckFullCapacityInput();
    }

    // =========================================================
    // 소환 불가 입력 확인
    // =========================================================

    private void CheckFullCapacityInput()
    {
        if (SummonManager.Instance == null)
        {
            return;
        }

        if (!SummonManager.Instance.IsPlacementMode)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        // HUD 자체를 누른 경우는 제외
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        int current =
            SummonManager.Instance.CurrentCapacity;

        int max =
            SummonManager.Instance.MaxCapacity;

        // 용량이 가득 찬 경우에만 피드백
        if (current < max)
        {
            return;
        }

        PlayCapacityBlockedFeedback();
    }

    // =========================================================
    // 텍스트 갱신
    // =========================================================

    private void RefreshCapacityText(
        bool forceRefresh)
    {
        if (capacityText == null)
        {
            return;
        }

        if (SummonManager.Instance == null)
        {
            capacityText.text =
                "- / -";

            return;
        }

        int current =
            SummonManager.Instance.CurrentCapacity;

        int max =
            SummonManager.Instance.MaxCapacity;

        if (!forceRefresh &&
            current == lastCurrentCapacity &&
            max == lastMaxCapacity)
        {
            return;
        }

        lastCurrentCapacity =
            current;

        lastMaxCapacity =
            max;

        capacityText.text =
            $"{current} / {max}";

        capacityText.color =
            current >= max
                ? fullColor
                : normalColor;
    }

    // =========================================================
    // 용량 부족 피드백
    // =========================================================

    public void PlayCapacityBlockedFeedback()
    {
        if (feedbackRoot == null)
        {
            return;
        }

        if (feedbackCoroutine != null)
        {
            StopCoroutine(
                feedbackCoroutine
            );
        }

        feedbackCoroutine =
            StartCoroutine(
                CapacityBlockedRoutine()
            );
    }

    private IEnumerator CapacityBlockedRoutine()
    {
        feedbackRoot.anchoredPosition =
            originalAnchoredPosition;

        feedbackRoot.localScale =
            originalScale;

        if (capacityText != null)
        {
            capacityText.color =
                blockedFlashColor;
        }

        float timer =
            0f;

        while (timer <
               shakeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        shakeDuration
                    )
                );

            float wave =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    shakeCount *
                    2f
                );

            float strength =
                1f - t;

            float offsetX =
                wave *
                shakeDistance *
                strength;

            feedbackRoot.anchoredPosition =
                originalAnchoredPosition +
                new Vector2(
                    offsetX,
                    0f
                );

            float scale =
                Mathf.Lerp(
                    punchScale,
                    1f,
                    t
                );

            feedbackRoot.localScale =
                originalScale *
                scale;

            yield return null;
        }

        // 완전 복귀
        feedbackRoot.anchoredPosition =
            originalAnchoredPosition;

        feedbackRoot.localScale =
            originalScale;

        if (capacityText != null)
        {
            int current =
                SummonManager.Instance != null
                    ? SummonManager.Instance.CurrentCapacity
                    : 0;

            int max =
                SummonManager.Instance != null
                    ? SummonManager.Instance.MaxCapacity
                    : 0;

            capacityText.color =
                current >= max
                    ? fullColor
                    : normalColor;
        }

        feedbackCoroutine =
            null;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        shakeDistance =
            Mathf.Max(
                0f,
                shakeDistance
            );

        shakeDuration =
            Mathf.Max(
                0.01f,
                shakeDuration
            );

        shakeCount =
            Mathf.Max(
                1,
                shakeCount
            );

        punchScale =
            Mathf.Max(
                1f,
                punchScale
            );
    }

#endif
}