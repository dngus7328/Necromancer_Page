using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SummonSlotVisual : MonoBehaviour
{
    // =========================================================
    // 카드 프리팹 표시
    // =========================================================

    [Header("카드 프리팹 표시")]

    [Tooltip("카드 프리팹이 생성될 위치")]
    [SerializeField]
    private RectTransform cardVisualRoot;

    [Tooltip("SummonData가 없는 빈 슬롯에 표시할 기본 카드 프리팹")]
    [SerializeField]
    private GameObject defaultSlotVisualPrefab;

    // =========================================================
    // 공통 UI
    // =========================================================

    [Header("공통 UI")]

    [Tooltip("기존 CooldownFill")]
    [SerializeField]
    private Image cooldownFill;

    [Tooltip("코스트 표시용 Text (TMP)")]
    [SerializeField]
    private TMP_Text costText;

    // =========================================================
    // 선택 연출
    // =========================================================

    [Header("선택 연출")]

    [Tooltip("선택됐을 때 위로 올라가는 거리")]
    [SerializeField]
    private float selectedMoveY = 12f;

    [Tooltip("선택됐을 때 확대 배율")]
    [SerializeField]
    private float selectedScale = 1.05f;

    [Tooltip("선택/해제 이동 속도")]
    [SerializeField]
    private float transitionSpeed = 12f;

    // =========================================================
    // 소환 후 복귀 연출
    // =========================================================

    [Header("소환 후 복귀")]

    [Tooltip("소환 직후 순간적으로 줄어드는 크기")]
    [SerializeField]
    private float summonReturnScale = 0.88f;

    [Tooltip("원래 크기로 돌아오는 시간")]
    [SerializeField]
    private float summonReturnDuration = 0.16f;

    // =========================================================
    // Runtime
    // =========================================================

    private RectTransform rectTransform;

    private Vector2 originalAnchoredPosition;

    private Vector3 originalScale;

    private GameObject currentCardVisual;

    private GameObject currentVisualPrefab;

    private bool hasSummonData;

    private bool isSelected;

    private Coroutine summonReturnCoroutine;

    // =========================================================
    // Property
    // =========================================================

    public bool HasSummonData =>
        hasSummonData;

    public Image CooldownFill =>
        cooldownFill;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            originalAnchoredPosition =
                rectTransform.anchoredPosition;
        }

        originalScale =
            transform.localScale;

        FindReferencesAutomatically();
    }

    private void Update()
    {
        UpdateSelectionVisual();
    }

    // =========================================================
    // 자동 참조
    // =========================================================

    private void FindReferencesAutomatically()
    {
        if (cardVisualRoot == null)
        {
            Transform found =
                transform.Find("CardVisualRoot");

            if (found != null)
            {
                cardVisualRoot =
                    found as RectTransform;
            }
        }

        if (cooldownFill == null)
        {
            Transform found =
                transform.Find("CooldownFill");

            if (found != null)
            {
                cooldownFill =
                    found.GetComponent<Image>();
            }
        }

        if (costText == null)
        {
            Transform found =
                transform.Find("Text (TMP)");

            if (found != null)
            {
                costText =
                    found.GetComponent<TMP_Text>();
            }
        }
    }

    // =========================================================
    // SummonData 적용
    // =========================================================

    public void SetData(
        SummonData data)
    {
        FindReferencesAutomatically();

        hasSummonData =
            data != null;

        GameObject wantedPrefab;

        if (data != null &&
            data.SlotVisualPrefab != null)
        {
            wantedPrefab =
                data.SlotVisualPrefab;
        }
        else
        {
            wantedPrefab =
                defaultSlotVisualPrefab;
        }

        SetCardVisual(
            wantedPrefab
        );

        // -----------------------------------------------------
        // 코스트
        // -----------------------------------------------------

        if (costText != null)
        {
            if (data != null)
            {
                costText.text =
    Mathf.RoundToInt(
        data.ManaCost
    ).ToString();

                costText.gameObject.SetActive(
                    true
                );
            }
            else
            {
                costText.text =
                    "";

                costText.gameObject.SetActive(
                    false
                );
            }
        }

        // -----------------------------------------------------
        // 쿨다운
        // -----------------------------------------------------

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount =
                0f;

            cooldownFill.gameObject.SetActive(
                data != null
            );
        }

        // -----------------------------------------------------
        // 빈 슬롯이면 선택 해제
        // -----------------------------------------------------

        if (data == null)
        {
            SetSelected(
                false
            );
        }
    }

    // =========================================================
    // 카드 프리팹 생성 / 교체
    // =========================================================

    private void SetCardVisual(
        GameObject prefab)
    {
        // 같은 프리팹이 이미 떠 있으면 다시 만들 필요 없음
        if (currentVisualPrefab ==
            prefab &&
            currentCardVisual != null)
        {
            return;
        }

        // 기존 카드 제거
        if (currentCardVisual != null)
        {
            Destroy(
                currentCardVisual
            );

            currentCardVisual =
                null;
        }

        currentVisualPrefab =
            prefab;

        if (prefab == null ||
            cardVisualRoot == null)
        {
            return;
        }

        // -----------------------------------------------------
        // 프리팹 생성
        //
        // false = 프리팹의 로컬 Transform 값을 최대한 유지
        // -----------------------------------------------------

        currentCardVisual =
            Instantiate(
                prefab,
                cardVisualRoot,
                false
            );

        RectTransform visualRect =
            currentCardVisual
                .GetComponent<RectTransform>();

        if (visualRect != null)
        {
            // =================================================
            // 중요
            //
            // 프리팹의:
            // Width
            // Height
            // Anchor
            // Pivot
            //
            // 전부 건드리지 않음.
            //
            // 위치만 CardVisualRoot 중심으로 옮김.
            // =================================================

            visualRect.anchoredPosition =
                Vector2.zero;

            visualRect.localScale =
                Vector3.one;

            visualRect.localRotation =
                Quaternion.identity;
        }
        else
        {
            // UI RectTransform이 아닌 경우 안전 처리
            Transform visualTransform =
                currentCardVisual.transform;

            visualTransform.localPosition =
                Vector3.zero;

            visualTransform.localScale =
                Vector3.one;

            visualTransform.localRotation =
                Quaternion.identity;
        }

        // -----------------------------------------------------
        // 카드 프리팹이 입력을 가리지 않도록 처리
        // -----------------------------------------------------

        Graphic[] graphics =
            currentCardVisual
                .GetComponentsInChildren<Graphic>(
                    true
                );

        for (int i = 0;
             i < graphics.Length;
             i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].raycastTarget =
                    false;
            }
        }
    }

    // =========================================================
    // 선택
    // =========================================================

    public void SetSelected(
        bool selected)
    {
        // 데이터 없는 슬롯은 선택 불가
        if (!hasSummonData)
        {
            isSelected =
                false;

            return;
        }

        isSelected =
            selected;
    }

    private void UpdateSelectionVisual()
    {
        if (rectTransform == null)
        {
            return;
        }

        // -----------------------------------------------------
        // 위치
        // -----------------------------------------------------

        Vector2 targetPosition =
            originalAnchoredPosition;

        if (isSelected)
        {
            targetPosition.y +=
                selectedMoveY;
        }

        rectTransform.anchoredPosition =
            Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetPosition,
                transitionSpeed *
                Time.unscaledDeltaTime
            );

        // -----------------------------------------------------
        // 크기
        // -----------------------------------------------------

        Vector3 targetScale =
            isSelected
                ? originalScale *
                  selectedScale
                : originalScale;

        if (summonReturnCoroutine ==
            null)
        {
            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    targetScale,
                    transitionSpeed *
                    Time.unscaledDeltaTime
                );
        }
    }

    // =========================================================
    // 소환 후 복귀
    // =========================================================

    public void PlaySummonReturn()
    {
        if (!hasSummonData ||
            !gameObject.activeInHierarchy)
        {
            return;
        }

        if (summonReturnCoroutine !=
            null)
        {
            StopCoroutine(
                summonReturnCoroutine
            );
        }

        summonReturnCoroutine =
            StartCoroutine(
                SummonReturnRoutine()
            );
    }

    private IEnumerator SummonReturnRoutine()
    {
        Vector3 targetScale =
            isSelected
                ? originalScale *
                  selectedScale
                : originalScale;

        Vector3 startScale =
            targetScale *
            summonReturnScale;

        transform.localScale =
            startScale;

        float duration =
            Mathf.Max(
                0.01f,
                summonReturnDuration
            );

        float timer =
            0f;

        while (timer <
               duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    3f
                );

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    eased
                );

            yield return null;
        }

        transform.localScale =
            targetScale;

        summonReturnCoroutine =
            null;
    }

    // =========================================================
    // 즉시 초기화
    // =========================================================

    public void ResetVisualImmediate()
    {
        isSelected =
            false;

        if (summonReturnCoroutine !=
            null)
        {
            StopCoroutine(
                summonReturnCoroutine
            );

            summonReturnCoroutine =
                null;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition =
                originalAnchoredPosition;
        }

        transform.localScale =
            originalScale;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        selectedMoveY =
            Mathf.Max(
                0f,
                selectedMoveY
            );

        selectedScale =
            Mathf.Max(
                0.1f,
                selectedScale
            );

        transitionSpeed =
            Mathf.Max(
                0.1f,
                transitionSpeed
            );

        summonReturnScale =
            Mathf.Max(
                0.1f,
                summonReturnScale
            );

        summonReturnDuration =
            Mathf.Max(
                0.01f,
                summonReturnDuration
            );
    }

#endif
}