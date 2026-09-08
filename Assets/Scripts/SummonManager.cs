using UnityEngine;

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

        [Header("비용")]
        public float manaCost = 20f;
        public int capacityCost = 1;
    }

    [Header("소환 슬롯 1~8")]
    [SerializeField]
    private SummonSlot[] summonSlots =
        new SummonSlot[8];

    [Header("슬롯 UI")]
    [Tooltip("SummonSlot_01 ~ 08의 SummonSlotVisual")]
    [SerializeField]
    private SummonSlotVisual[] slotVisuals =
        new SummonSlotVisual[8];

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
        new Color(0.65f, 0.4f, 1f, 0.65f);

    [SerializeField]
    private Color invalidPreviewColor =
        new Color(1f, 0.25f, 0.25f, 0.65f);

    [Header("배치 취소 X")]
    [SerializeField] private GameObject placementCancelButton;

    private int selectedSlotIndex = -1;

    private bool isPlacementMode;

    private GameObject currentPreview;

    private SpriteRenderer[] previewRenderers;

    private Vector2 currentPlacementPosition;

    private bool currentPlacementValid;

    private bool shouldBlockRibelMoveThisFrame;

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
                    .GetComponentInChildren<SpriteRenderer>(true);
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
    }

    private void Start()
    {
        if (placementCancelButton != null)
        {
            placementCancelButton.SetActive(false);
        }

        if (summonRangeVisual != null)
        {
            summonRangeVisual.SetActive(false);
        }

        ClearSlotSelectionVisual();
    }

    private void Update()
    {
        shouldBlockRibelMoveThisFrame =
            false;

        HandleSlotInput();

        if (!isPlacementMode)
        {
            return;
        }

        UpdatePlacementPreview();
        HandlePlacementInput();
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
        for (int i = 0; i < 8; i++)
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

    private void SelectSlot(int slotIndex)
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
        // 같은 슬롯 두 번째 입력
        // =====================================================

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

        // ★ 선택 피드백 갱신
        UpdateSlotSelectionVisual();

        if (isPlacementMode)
        {
            DestroyPreview();
            CreatePreview();
        }
    }

    // =========================================================
    // 슬롯 선택 UI
    // =========================================================

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
                i == selectedSlotIndex
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

        isPlacementMode = true;

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
    // 프리뷰 생성
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

    // =========================================================
    // 프리뷰 이동
    // =========================================================

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
            currentPreview
                .transform
                .position =
                currentPlacementPosition;
        }

        currentPlacementValid =
            CheckPlacementValid(
                currentPlacementPosition
            );

        UpdatePreviewColor();
    }

    // =========================================================
    // 실제 소환 범위
    // =========================================================

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

    // =========================================================
    // 프리뷰 범위 제한
    // =========================================================

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

    // =========================================================
    // 배치 가능 여부
    // =========================================================

    private bool CheckPlacementValid(
        Vector2 position)
    {
        if (selectedSlotIndex < 0 ||
            selectedSlotIndex >=
            summonSlots.Length)
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

        if (blocked != null)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // 프리뷰 색
    // =========================================================

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
            if (previewRenderers[i] == null)
            {
                continue;
            }

            previewRenderers[i].color =
                color;
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
    // 소환
    // =========================================================

    private void PlaceSummon()
    {
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
            slot.summonPrefab == null)
        {
            return;
        }

        if (!CheckPlacementValid(
                currentPlacementPosition))
        {
            return;
        }

        Instantiate(
            slot.summonPrefab,
            currentPlacementPosition,
            Quaternion.identity
        );

        // 리벨 소환 모션
        if (ribel != null)
        {
            RibelController controller =
                ribel.GetComponent<RibelController>();

            if (controller != null)
            {
                controller
                    .PlaySummonMotion();
            }
        }

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
        // ★ 소환 성공 시 카드가 아래로 복귀
        // =====================================================

        if (slotVisuals != null &&
            selectedSlotIndex <
            slotVisuals.Length &&
            slotVisuals[selectedSlotIndex] !=
            null)
        {
            slotVisuals[
                selectedSlotIndex
            ].PlaySummonReturn();
        }

        // 선택 자체도 해제
        selectedSlotIndex = -1;

        ExitPlacementMode();
    }

    // =========================================================
    // 배치 취소
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

    // =========================================================
    // 배치 종료
    // =========================================================

    private void ExitPlacementMode()
    {
        isPlacementMode = false;

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

    // =========================================================
    // 프리뷰 삭제
    // =========================================================

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

    public void AddMana(float amount)
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
            currentCapacity = 0;
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
    }
#endif
}