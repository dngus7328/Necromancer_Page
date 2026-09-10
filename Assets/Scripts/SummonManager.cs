using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance { get; private set; }

    [System.Serializable]
    public class SummonSlot
    {
        [Header("소환수")]
        public GameObject summonPrefab;

        [Header("배치 프리뷰")]
        public GameObject previewPrefab;

        [Header("소환진")]
        [Tooltip("소환할 때 바닥에 표시할 소환진 PNG")]
        public Sprite summonCircleSprite;

        [Header("비용")]
        public float manaCost = 20f;
        public int capacityCost = 1;

        [Header("쿨타임")]
        [Tooltip("이 소환수를 다시 사용할 수 있을 때까지의 시간")]
        public float cooldown = 8f;
    }

    [Header("소환 슬롯 1~8")]
    [SerializeField]
    private SummonSlot[] summonSlots =
        new SummonSlot[8];

    [Header("슬롯 UI")]
    [SerializeField]
    private SummonSlotVisual[] slotVisuals =
        new SummonSlotVisual[8];

    [Header("쿨타임 UI")]
    [Tooltip("각 슬롯의 CooldownFill Image를 1~8 순서대로 연결")]
    [SerializeField]
    private Image[] cooldownFills =
        new Image[8];

    [Header("마나")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float currentMana = 100f;

    [Header("소환 용량")]
    [SerializeField] private int maxCapacity = 10;
    [SerializeField] private int currentCapacity = 0;

    [Header("리벨")]
    [SerializeField] private Transform ribel;

    [Header("소환 범위")]
    [SerializeField] private GameObject summonRangeVisual;
    [SerializeField] private SpriteRenderer summonRangeRenderer;
    [SerializeField] private float fallbackSummonRange = 9f;

    [Header("배치")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask blockedLayers;
    [SerializeField] private float placementCheckRadius = 0.35f;

    [Header("프리뷰 색")]
    [SerializeField]
    private Color validPreviewColor =
        new Color(
            0.65f,
            0.4f,
            1f,
            0.65f
        );

    [SerializeField]
    private Color invalidPreviewColor =
        new Color(
            1f,
            0.25f,
            0.25f,
            0.65f
        );

    [Header("배치 취소 X")]
    [SerializeField] private GameObject placementCancelButton;

    [Header("소환진 연출")]
    [SerializeField] private float circleStartScale = 0.65f;
    [SerializeField] private float circlePeakScale = 1.15f;
    [SerializeField] private float circleEndScale = 1.30f;

    [SerializeField] private float circleBurstTime = 0.07f;
    [SerializeField] private float circleHoldTime = 0.05f;
    [SerializeField] private float circleFadeTime = 0.22f;

    [Tooltip("소환진 Sorting Order")]
    [SerializeField] private int circleSortingOrder = 50;

    [Header("소환수 등장 연출")]
    [SerializeField] private float summonAppearTime = 0.22f;
    [SerializeField] private float summonStartScale = 0.82f;
    [SerializeField] private float summonRiseDistance = 0.12f;

    private int selectedSlotIndex = -1;

    private bool isPlacementMode;

    private GameObject currentPreview;

    private SpriteRenderer[] previewRenderers;

    private Vector2 currentPlacementPosition;

    private bool currentPlacementValid;

    private bool shouldBlockRibelMoveThisFrame;

    // =========================================================
    // 슬롯별 현재 남은 쿨타임
    // =========================================================

    private float[] remainingCooldowns =
        new float[8];

    public bool IsPlacementMode =>
        isPlacementMode;

    public bool ShouldBlockRibelMoveThisFrame =>
        shouldBlockRibelMoveThisFrame;

    public float CurrentMana =>
        currentMana;

    public float MaxMana =>
        maxMana;

    public int CurrentCapacity =>
        currentCapacity;

    public int MaxCapacity =>
        maxCapacity;

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        FindRibel();

        if (summonRangeRenderer == null &&
            summonRangeVisual != null)
        {
            summonRangeRenderer =
                summonRangeVisual
                    .GetComponentInChildren<SpriteRenderer>(
                        true
                    );
        }

        currentMana =
            Mathf.Clamp(
                currentMana,
                0f,
                maxMana
            );

        currentCapacity =
            Mathf.Clamp(
                currentCapacity,
                0,
                maxCapacity
            );

        if (remainingCooldowns == null ||
            remainingCooldowns.Length != 8)
        {
            remainingCooldowns =
                new float[8];
        }
    }

    private void Start()
    {
        if (placementCancelButton != null)
        {
            placementCancelButton.SetActive(
                false
            );
        }

        if (summonRangeVisual != null)
        {
            summonRangeVisual.SetActive(
                false
            );
        }

        ClearSlotSelectionVisual();

        // 게임 시작 시 모든 쿨타임은 준비 완료 상태
        for (int i = 0;
             i < remainingCooldowns.Length;
             i++)
        {
            remainingCooldowns[i] =
                0f;
        }

        UpdateCooldownUI();
    }

    private void Update()
    {
        shouldBlockRibelMoveThisFrame =
            false;

        UpdateCooldowns();

        HandleSlotInput();

        if (!isPlacementMode)
        {
            return;
        }

        UpdatePlacementPreview();
        HandlePlacementInput();
    }

    // =========================================================
    // 쿨타임
    // =========================================================

    private void UpdateCooldowns()
    {
        if (remainingCooldowns == null)
        {
            return;
        }

        for (int i = 0;
             i < remainingCooldowns.Length;
             i++)
        {
            if (remainingCooldowns[i] <= 0f)
            {
                remainingCooldowns[i] =
                    0f;

                continue;
            }

            remainingCooldowns[i] -=
                Time.deltaTime;

            if (remainingCooldowns[i] < 0f)
            {
                remainingCooldowns[i] =
                    0f;
            }
        }

        UpdateCooldownUI();
    }

    private void UpdateCooldownUI()
    {
        if (cooldownFills == null)
        {
            return;
        }

        for (int i = 0;
             i < cooldownFills.Length;
             i++)
        {
            Image fill =
                cooldownFills[i];

            if (fill == null)
            {
                continue;
            }

            if (i >= summonSlots.Length ||
                summonSlots[i] == null)
            {
                fill.fillAmount =
                    0f;

                continue;
            }

            float totalCooldown =
                Mathf.Max(
                    0.01f,
                    summonSlots[i].cooldown
                );

            float remaining =
                0f;

            if (i < remainingCooldowns.Length)
            {
                remaining =
                    remainingCooldowns[i];
            }

            // 소환 직후 = 1
            // 쿨타임 완료 = 0
            fill.fillAmount =
                Mathf.Clamp01(
                    remaining /
                    totalCooldown
                );
        }
    }

    private bool IsSlotOnCooldown(
        int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >=
            remainingCooldowns.Length)
        {
            return false;
        }

        return remainingCooldowns[slotIndex] >
            0f;
    }

    private void StartCooldown(
        int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >=
            remainingCooldowns.Length ||
            slotIndex >=
            summonSlots.Length)
        {
            return;
        }

        SummonSlot slot =
            summonSlots[slotIndex];

        if (slot == null)
        {
            return;
        }

        remainingCooldowns[slotIndex] =
            Mathf.Max(
                0f,
                slot.cooldown
            );

        UpdateCooldownUI();
    }

    // =========================================================
    // 리벨
    // =========================================================

    private void FindRibel()
    {
        if (ribel != null)
        {
            return;
        }

        GameObject ribelObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (ribelObject != null)
        {
            ribel =
                ribelObject.transform;
        }
    }

    // =========================================================
    // 숫자키
    // =========================================================

    private void HandleSlotInput()
    {
        for (int i = 0;
             i < 8;
             i++)
        {
            KeyCode key =
                (KeyCode)(
                    (int)KeyCode.Alpha1 +
                    i
                );

            if (!Input.GetKeyDown(key))
            {
                continue;
            }

            SelectSlot(i);
            break;
        }
    }

    // =========================================================
    // 슬롯 선택
    // =========================================================

    private void SelectSlot(
        int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >= summonSlots.Length)
        {
            return;
        }

        SummonSlot slot =
            summonSlots[slotIndex];

        if (slot == null ||
            slot.summonPrefab == null)
        {
            return;
        }

        // =====================================================
        // 쿨타임 중에는 해당 슬롯 사용 불가
        // =====================================================

        if (IsSlotOnCooldown(
                slotIndex))
        {
            return;
        }

        if (selectedSlotIndex ==
            slotIndex)
        {
            if (!isPlacementMode)
            {
                EnterPlacementMode();
            }

            return;
        }

        selectedSlotIndex =
            slotIndex;

        UpdateSlotSelectionVisual();

        if (isPlacementMode)
        {
            DestroyPreview();
            CreatePreview();
        }
    }

    private void UpdateSlotSelectionVisual()
    {
        if (slotVisuals == null)
        {
            return;
        }

        for (int i = 0;
             i < slotVisuals.Length;
             i++)
        {
            if (slotVisuals[i] == null)
            {
                continue;
            }

            slotVisuals[i].SetSelected(
                i ==
                selectedSlotIndex
            );
        }
    }

    private void ClearSlotSelectionVisual()
    {
        if (slotVisuals == null)
        {
            return;
        }

        for (int i = 0;
             i < slotVisuals.Length;
             i++)
        {
            if (slotVisuals[i] == null)
            {
                continue;
            }

            slotVisuals[i].SetSelected(
                false
            );
        }
    }

    // =========================================================
    // 배치 모드
    // =========================================================

    private void EnterPlacementMode()
    {
        if (selectedSlotIndex < 0 ||
            selectedSlotIndex >=
            summonSlots.Length)
        {
            return;
        }

        // 쿨타임 중이라면 배치 모드 진입 금지
        if (IsSlotOnCooldown(
                selectedSlotIndex))
        {
            return;
        }

        isPlacementMode =
            true;

        if (summonRangeVisual != null)
        {
            summonRangeVisual.SetActive(
                true
            );
        }

        CreatePreview();

        if (placementCancelButton != null)
        {
            placementCancelButton.SetActive(
                true
            );
        }
    }

    // =========================================================
    // 프리뷰
    // =========================================================

    private void CreatePreview()
    {
        DestroyPreview();

        if (selectedSlotIndex < 0 ||
            selectedSlotIndex >=
            summonSlots.Length)
        {
            return;
        }

        SummonSlot slot =
            summonSlots[
                selectedSlotIndex
            ];

        if (slot == null ||
            slot.previewPrefab == null)
        {
            return;
        }

        currentPreview =
            Instantiate(
                slot.previewPrefab,
                Vector3.zero,
                Quaternion.identity
            );

        previewRenderers =
            currentPreview
                .GetComponentsInChildren<SpriteRenderer>();
    }

    private void UpdatePlacementPreview()
    {
        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;

            if (mainCamera == null)
            {
                return;
            }
        }

        FindRibel();

        if (ribel == null)
        {
            return;
        }

        Vector3 mouseWorld3 =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Vector2 mouseWorld =
            new Vector2(
                mouseWorld3.x,
                mouseWorld3.y
            );

        currentPlacementPosition =
            ClampPositionInsideSummonRange(
                mouseWorld
            );

        if (currentPreview != null)
        {
            currentPreview.transform.position =
                currentPlacementPosition;
        }

        currentPlacementValid =
            CheckPlacementValid(
                currentPlacementPosition
            );

        UpdatePreviewColor();
    }

    private float GetActualSummonRange()
    {
        if (summonRangeRenderer != null)
        {
            Bounds bounds =
                summonRangeRenderer.bounds;

            return Mathf.Min(
                bounds.extents.x,
                bounds.extents.y
            );
        }

        return fallbackSummonRange;
    }

    private Vector2 ClampPositionInsideSummonRange(
        Vector2 targetPosition)
    {
        Vector2 center =
            ribel.position;

        Vector2 direction =
            targetPosition -
            center;

        float range =
            GetActualSummonRange();

        Vector2 clampedOffset =
            Vector2.ClampMagnitude(
                direction,
                range
            );

        return center +
            clampedOffset;
    }

    private bool CheckPlacementValid(
        Vector2 position)
    {
        if (selectedSlotIndex < 0 ||
            selectedSlotIndex >=
            summonSlots.Length)
        {
            return false;
        }

        // 쿨타임이면 배치 불가
        if (IsSlotOnCooldown(
                selectedSlotIndex))
        {
            return false;
        }

        SummonSlot slot =
            summonSlots[
                selectedSlotIndex
            ];

        if (slot == null)
        {
            return false;
        }

        if (currentMana <
            slot.manaCost)
        {
            return false;
        }

        if (currentCapacity +
            slot.capacityCost >
            maxCapacity)
        {
            return false;
        }

        Collider2D blocked =
            Physics2D.OverlapCircle(
                position,
                placementCheckRadius,
                blockedLayers
            );

        return blocked ==
            null;
    }

    private void UpdatePreviewColor()
    {
        if (previewRenderers == null)
        {
            return;
        }

        Color color =
            currentPlacementValid
                ? validPreviewColor
                : invalidPreviewColor;

        for (int i = 0;
             i < previewRenderers.Length;
             i++)
        {
            if (previewRenderers[i] != null)
            {
                previewRenderers[i].color =
                    color;
            }
        }
    }

    // =========================================================
    // 배치 입력
    // =========================================================

    private void HandlePlacementInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            shouldBlockRibelMoveThisFrame =
                true;

            CancelPlacement();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            shouldBlockRibelMoveThisFrame =
                true;

            if (!currentPlacementValid)
            {
                return;
            }

            PlaceSummon();
        }
    }

    // =========================================================
    // 실제 소환
    // =========================================================

    private void PlaceSummon()
    {
        if (selectedSlotIndex < 0 ||
            selectedSlotIndex >=
            summonSlots.Length)
        {
            return;
        }

        int usedSlotIndex =
            selectedSlotIndex;

        SummonSlot slot =
            summonSlots[
                usedSlotIndex
            ];

        if (slot == null ||
            slot.summonPrefab == null)
        {
            return;
        }

        if (IsSlotOnCooldown(
                usedSlotIndex))
        {
            return;
        }

        if (!CheckPlacementValid(
                currentPlacementPosition))
        {
            return;
        }

        // =====================================================
        // 소환진
        // =====================================================

        if (slot.summonCircleSprite != null)
        {
            CreateSummonCircle(
                slot.summonCircleSprite,
                currentPlacementPosition
            );
        }

        // =====================================================
        // 소환수 생성
        // =====================================================

        GameObject summonObject =
            Instantiate(
                slot.summonPrefab,
                currentPlacementPosition,
                Quaternion.identity
            );

        // =====================================================
        // 소환수 등장 연출
        // =====================================================

        StartCoroutine(
            PlaySummonAppearance(
                summonObject
            )
        );

        SummonUnitBase summonUnit =
            summonObject
                .GetComponent<SummonUnitBase>();

        if (summonUnit == null)
        {
            summonUnit =
                summonObject
                    .GetComponentInChildren<SummonUnitBase>(
                        true
                    );
        }

        if (summonUnit != null)
        {
            summonUnit
                .BeginSummonAppearance();
        }

        // =====================================================
        // 리벨 소환 모션
        // =====================================================

        if (ribel != null)
        {
            RibelController controller =
                ribel
                    .GetComponent<RibelController>();

            if (controller != null)
            {
                controller
                    .PlaySummonMotion();
            }
        }

        // =====================================================
        // 비용 처리
        // =====================================================

        currentMana -=
            slot.manaCost;

        currentCapacity +=
            slot.capacityCost;

        currentMana =
            Mathf.Clamp(
                currentMana,
                0f,
                maxMana
            );

        currentCapacity =
            Mathf.Clamp(
                currentCapacity,
                0,
                maxCapacity
            );

        // =====================================================
        // ★ 쿨타임 시작
        // =====================================================

        StartCooldown(
            usedSlotIndex
        );

        if (slotVisuals != null &&
            usedSlotIndex <
            slotVisuals.Length &&
            slotVisuals[usedSlotIndex] !=
            null)
        {
            slotVisuals[
                usedSlotIndex
            ].PlaySummonReturn();
        }

        selectedSlotIndex =
            -1;

        ExitPlacementMode();
    }

    // =========================================================
    // 소환진 자동 생성
    // =========================================================

    private void CreateSummonCircle(
        Sprite sprite,
        Vector2 position)
    {
        GameObject circleObject =
            new GameObject(
                "SummonCircle"
            );

        circleObject.transform.position =
            position;

        SpriteRenderer renderer =
            circleObject.AddComponent<SpriteRenderer>();

        renderer.sprite =
            sprite;

        renderer.color =
            Color.white;

        renderer.sortingOrder =
            circleSortingOrder;

        StartCoroutine(
            PlaySummonCircle(
                circleObject,
                renderer
            )
        );
    }

    private IEnumerator PlaySummonCircle(
        GameObject circleObject,
        SpriteRenderer renderer)
    {
        Transform t =
            circleObject.transform;

        Vector3 baseScale =
            Vector3.one;

        t.localScale =
            baseScale *
            circleStartScale;

        Color baseColor =
            renderer.color;

        baseColor.a =
            1f;

        renderer.color =
            baseColor;

        float timer =
            0f;

        while (timer <
               circleBurstTime)
        {
            timer +=
                Time.deltaTime;

            float p =
                Mathf.Clamp01(
                    timer /
                    circleBurstTime
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - p,
                    3f
                );

            float scale =
                Mathf.Lerp(
                    circleStartScale,
                    circlePeakScale,
                    eased
                );

            t.localScale =
                baseScale *
                scale;

            yield return null;
        }

        t.localScale =
            baseScale *
            circlePeakScale;

        if (circleHoldTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    circleHoldTime
                );
        }

        timer =
            0f;

        while (timer <
               circleFadeTime)
        {
            timer +=
                Time.deltaTime;

            float p =
                Mathf.Clamp01(
                    timer /
                    circleFadeTime
                );

            float scale =
                Mathf.Lerp(
                    circlePeakScale,
                    circleEndScale,
                    p
                );

            t.localScale =
                baseScale *
                scale;

            Color color =
                baseColor;

            color.a =
                Mathf.Lerp(
                    1f,
                    0f,
                    p
                );

            renderer.color =
                color;

            yield return null;
        }

        Destroy(
            circleObject
        );
    }

    // =========================================================
    // 소환수 자동 등장 연출
    // =========================================================

    private IEnumerator PlaySummonAppearance(
        GameObject summonObject)
    {
        if (summonObject == null)
        {
            yield break;
        }

        SpriteRenderer[] renderers =
            summonObject
                .GetComponentsInChildren<SpriteRenderer>(
                    true
                );

        if (renderers == null ||
            renderers.Length == 0)
        {
            yield break;
        }

        Color[] originalColors =
            new Color[
                renderers.Length
            ];

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            originalColors[i] =
                renderers[i].color;

            Color transparent =
                originalColors[i];

            transparent.a =
                0f;

            renderers[i].color =
                transparent;
        }

        Transform summonTransform =
            summonObject.transform;

        Vector3 originalScale =
            summonTransform.localScale;

        Vector3 originalPosition =
            summonTransform.position;

        summonTransform.localScale =
            originalScale *
            summonStartScale;

        summonTransform.position =
            originalPosition +
            Vector3.down *
            summonRiseDistance;

        float timer =
            0f;

        float duration =
            Mathf.Max(
                0.01f,
                summonAppearTime
            );

        while (timer <
               duration)
        {
            if (summonObject == null)
            {
                yield break;
            }

            timer +=
                Time.deltaTime;

            float p =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - p,
                    3f
                );

            summonTransform.localScale =
                Vector3.Lerp(
                    originalScale *
                    summonStartScale,
                    originalScale,
                    eased
                );

            summonTransform.position =
                Vector3.Lerp(
                    originalPosition +
                    Vector3.down *
                    summonRiseDistance,
                    originalPosition,
                    eased
                );

            for (int i = 0;
                 i < renderers.Length;
                 i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                Color color =
                    originalColors[i];

                color.a =
                    Mathf.Lerp(
                        0f,
                        originalColors[i].a,
                        eased
                    );

                renderers[i].color =
                    color;
            }

            yield return null;
        }

        if (summonObject == null)
        {
            yield break;
        }

        summonTransform.localScale =
            originalScale;

        summonTransform.position =
            originalPosition;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color =
                    originalColors[i];
            }
        }
    }

    // =========================================================
    // 취소
    // =========================================================

    public void CancelPlacement()
    {
        if (!isPlacementMode)
        {
            return;
        }

        shouldBlockRibelMoveThisFrame =
            true;

        ExitPlacementMode();
    }

    private void ExitPlacementMode()
    {
        isPlacementMode =
            false;

        DestroyPreview();

        if (summonRangeVisual != null)
        {
            summonRangeVisual.SetActive(
                false
            );
        }

        if (placementCancelButton != null)
        {
            placementCancelButton.SetActive(
                false
            );
        }
    }

    private void DestroyPreview()
    {
        if (currentPreview != null)
        {
            Destroy(
                currentPreview
            );

            currentPreview =
                null;
        }

        previewRenderers =
            null;
    }

    // =========================================================
    // 마나
    // =========================================================

    public void AddMana(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentMana +=
            amount;

        if (currentMana >
            maxMana)
        {
            currentMana =
                maxMana;
        }
    }

    // =========================================================
    // 용량
    // =========================================================

    public void ReleaseCapacity(
        int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentCapacity -=
            amount;

        if (currentCapacity < 0)
        {
            currentCapacity =
                0;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxMana =
            Mathf.Max(
                1f,
                maxMana
            );

        currentMana =
            Mathf.Clamp(
                currentMana,
                0f,
                maxMana
            );

        maxCapacity =
            Mathf.Max(
                1,
                maxCapacity
            );

        currentCapacity =
            Mathf.Clamp(
                currentCapacity,
                0,
                maxCapacity
            );

        fallbackSummonRange =
            Mathf.Max(
                0.1f,
                fallbackSummonRange
            );

        placementCheckRadius =
            Mathf.Max(
                0.05f,
                placementCheckRadius
            );

        circleStartScale =
            Mathf.Max(
                0.01f,
                circleStartScale
            );

        circlePeakScale =
            Mathf.Max(
                circleStartScale,
                circlePeakScale
            );

        circleEndScale =
            Mathf.Max(
                circlePeakScale,
                circleEndScale
            );

        circleBurstTime =
            Mathf.Max(
                0.01f,
                circleBurstTime
            );

        circleHoldTime =
            Mathf.Max(
                0f,
                circleHoldTime
            );

        circleFadeTime =
            Mathf.Max(
                0.01f,
                circleFadeTime
            );

        summonAppearTime =
            Mathf.Max(
                0.01f,
                summonAppearTime
            );

        summonRiseDistance =
            Mathf.Max(
                0f,
                summonRiseDistance
            );

        if (summonSlots == null ||
            summonSlots.Length != 8)
        {
            System.Array.Resize(
                ref summonSlots,
                8
            );
        }

        if (slotVisuals == null ||
            slotVisuals.Length != 8)
        {
            System.Array.Resize(
                ref slotVisuals,
                8
            );
        }

        if (cooldownFills == null ||
            cooldownFills.Length != 8)
        {
            System.Array.Resize(
                ref cooldownFills,
                8
            );
        }

        if (remainingCooldowns == null ||
            remainingCooldowns.Length != 8)
        {
            System.Array.Resize(
                ref remainingCooldowns,
                8
            );
        }

        for (int i = 0;
             i < summonSlots.Length;
             i++)
        {
            if (summonSlots[i] != null)
            {
                summonSlots[i].cooldown =
                    Mathf.Max(
                        0f,
                        summonSlots[i].cooldown
                    );
            }
        }
    }
#endif
}