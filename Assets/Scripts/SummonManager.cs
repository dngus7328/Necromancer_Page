using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance { get; private set; }

    [System.Serializable]
    public class SummonSlot
    {
        [Header("소환수 데이터")]
        public SummonData summonData;
    }

    [Header("전투 슬롯 1~8")]
    [SerializeField]
    private SummonSlot[] summonSlots =
        new SummonSlot[8];

    [Header("슬롯 비주얼")]
    [SerializeField]
    private SummonSlotVisual[] slotVisuals =
        new SummonSlotVisual[8];

    [Header("쿨타임 UI")]
    [SerializeField]
    private Image[] cooldownFills =
        new Image[8];

    [Header("마나")]
    [SerializeField]
    private float maxMana = 100f;

    [SerializeField]
    private float currentMana = 100f;

    [Header("소환 용량")]
    [SerializeField]
    private int maxCapacity = 10;

    [SerializeField]
    private int currentCapacity = 0;

    [Header("리벨")]
    [SerializeField]
    private Transform ribel;

    [Header("소환 가능 사거리")]
    [SerializeField]
    private GameObject summonRangeVisual;

    [SerializeField]
    private SpriteRenderer summonRangeRenderer;

    [SerializeField]
    private float fallbackSummonRange = 9f;

    [Header("사거리 원 보이는 테두리 보정")]
    [Tooltip(
        "큰 소환 가능 사거리 이미지에서 실제로 보이는 원의 비율입니다."
    )]
    [Range(0.5f, 1f)]
    [SerializeField]
    private float summonRangeVisibleRadiusRatio = 1f;

    [Header("배치")]
    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private LayerMask blockedLayers;

    [SerializeField]
    private float placementCheckRadius = 0.35f;

    [Header("배치 프리뷰 원")]
    [SerializeField]
    private Sprite placementPreviewCircleSprite;

    [SerializeField]
    private Color validPreviewCircleColor =
        new Color(
            0.45f,
            0.9f,
            1f,
            0.8f
        );

    [SerializeField]
    private Color invalidPreviewCircleColor =
        new Color(
            1f,
            0.25f,
            0.25f,
            0.8f
        );

    [Header("프리뷰 원 보이는 테두리 보정")]
    [Tooltip(
        "프리뷰 원 PNG에서 실제로 보이는 원의 비율입니다."
    )]
    [Range(0.5f, 1f)]
    [SerializeField]
    private float previewCircleVisibleRadiusRatio = 0.85f;

    [Header("프리뷰 소환수")]
    [SerializeField]
    private Color previewUnitColor =
        new Color(
            0.75f,
            0.6f,
            1f,
            0.6f
        );

    [Header("프리뷰 렌더")]
    [SerializeField]
    private string previewSortingLayer =
        "Effect";

    [SerializeField]
    private int previewCircleOrder = 35;

    [SerializeField]
    private int previewUnitOrder = 40;

    [Header("배치 취소")]
    [SerializeField]
    private GameObject placementCancelButton;

    [Header("소환진 연출")]
    [SerializeField]
    private float circleStartScale = 0.65f;

    [SerializeField]
    private float circlePeakScale = 1.15f;

    [SerializeField]
    private float circleEndScale = 1.30f;

    [SerializeField]
    private float circleBurstTime = 0.07f;

    [SerializeField]
    private float circleHoldTime = 0.05f;

    [SerializeField]
    private float circleFadeTime = 0.22f;

    [Header("소환진 렌더")]
    [SerializeField]
    private string summonCircleSortingLayer =
        "Effect";

    [SerializeField]
    private int circleSortingOrder = 50;

    // =========================================================
    // Runtime
    // =========================================================

    private int selectedSlotIndex = -1;

    private bool isPlacementMode;

    private GameObject currentPreviewRoot;

    private SpriteRenderer previewCircleRenderer;

    private readonly List<SpriteRenderer>
        previewMemberRenderers =
            new List<SpriteRenderer>();

    private Vector2 currentPlacementPosition;

    private bool currentPlacementValid;

    private bool shouldBlockRibelMoveThisFrame;

    private float[] remainingCooldowns =
        new float[8];

    private const float CooldownReadyThreshold =
        0.05f;

    // =========================================================
    // 외부 확인용
    // =========================================================

    public bool IsPlacementMode =>
        isPlacementMode;

    /// <summary>
    /// 배치 모드에서는 좌클릭을 전부 소환 조작이 사용한다.
    /// 소환 가능한 위치인지 여부와 상관없이 리벨 이동에는 전달하지 않는다.
    /// </summary>
    public bool IsConsumingWorldClick =>
        isPlacementMode;

    // 기존 코드와의 호환을 위해 유지
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
            mainCamera =
                Camera.main;
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

        if (remainingCooldowns == null ||
            remainingCooldowns.Length != 8)
        {
            remainingCooldowns =
                new float[8];
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

        for (int i = 0;
             i < remainingCooldowns.Length;
             i++)
        {
            remainingCooldowns[i] =
                0f;
        }

        RefreshAllSlotVisuals();

        ClearSlotSelectionVisual();

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
    // 슬롯 데이터
    // =========================================================

    private SummonData GetSlotData(
        int index)
    {
        if (summonSlots == null ||
            index < 0 ||
            index >= summonSlots.Length)
        {
            return null;
        }

        if (summonSlots[index] == null)
        {
            return null;
        }

        return summonSlots[index].summonData;
    }

    // =========================================================
    // 슬롯 비주얼
    // =========================================================

    private void RefreshAllSlotVisuals()
    {
        if (slotVisuals == null)
        {
            return;
        }

        for (int i = 0;
             i < slotVisuals.Length;
             i++)
        {
            RefreshSlotVisual(i);
        }
    }

    private void RefreshSlotVisual(
        int index)
    {
        if (slotVisuals == null ||
            index < 0 ||
            index >= slotVisuals.Length)
        {
            return;
        }

        if (slotVisuals[index] == null)
        {
            return;
        }

        slotVisuals[index].SetData(
            GetSlotData(index)
        );
    }

    // =========================================================
    // 쿨타임
    // =========================================================

    private void UpdateCooldowns()
    {
        for (int i = 0;
             i < remainingCooldowns.Length;
             i++)
        {
            if (remainingCooldowns[i] <=
                CooldownReadyThreshold)
            {
                remainingCooldowns[i] =
                    0f;

                continue;
            }

            remainingCooldowns[i] -=
                Time.deltaTime;

            if (remainingCooldowns[i] <=
                CooldownReadyThreshold)
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

            SummonData data =
                GetSlotData(i);

            if (data == null)
            {
                fill.fillAmount =
                    0f;

                continue;
            }

            float remaining =
                remainingCooldowns[i];

            if (remaining <=
                CooldownReadyThreshold)
            {
                fill.fillAmount =
                    0f;

                continue;
            }

            float cooldown =
                Mathf.Max(
                    0.01f,
                    GetActualCooldown(
                        i
                    )
                );

            fill.fillAmount =
                Mathf.Clamp01(
                    remaining /
                    cooldown
                );
        }
    }

    private bool IsSlotOnCooldown(
        int index)
    {
        if (index < 0 ||
            index >= remainingCooldowns.Length)
        {
            return false;
        }

        return remainingCooldowns[index] >
            CooldownReadyThreshold;
    }

    private void StartCooldown(
        int index)
    {
        SummonData data =
            GetSlotData(index);

        if (data == null ||
            index < 0 ||
            index >= remainingCooldowns.Length)
        {
            return;
        }

        remainingCooldowns[index] =
            GetActualCooldown(
                index
            );

        UpdateCooldownUI();
    }

    // =========================================================
    // 숫자키 1~8
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

            if (Input.GetKeyDown(key))
            {
                SelectSlot(i);
                return;
            }
        }
    }

    private bool TryShowSummonFailureMessage(
        int slotIndex,
        SummonData data,
        bool includePositionCheck)
    {
        if (data == null)
        {
            return false;
        }

        if (IsSlotOnCooldown(
                slotIndex
            ))
        {
            ShowSystemMessage(
                SystemMessageUI.MessageType.Cooldown
            );

            return true;
        }

        if (currentMana <
            GetActualManaCost(
                slotIndex
            ))
        {
            ShowSystemMessage(
                SystemMessageUI.MessageType.NotEnoughMana
            );

            return true;
        }

        if (currentCapacity +
            data.CapacityCost >
            maxCapacity)
        {
            ShowSystemMessage(
                SystemMessageUI.MessageType.CapacityFull
            );

            return true;
        }

        if (includePositionCheck)
        {
            Collider2D blocked =
                Physics2D.OverlapCircle(
                    currentPlacementPosition,
                    placementCheckRadius,
                    blockedLayers
                );

            if (blocked != null)
            {
                ShowSystemMessage(
                    SystemMessageUI.MessageType.InvalidPosition
                );

                return true;
            }
        }

        return false;
    }

    private void ShowSystemMessage(
        SystemMessageUI.MessageType type)
    {
        if (SystemMessageUI.Instance == null)
        {
            return;
        }

        SystemMessageUI.Instance.Show(
            type
        );
    }

    private void SelectSlot(
        int index)
    {
        SummonData data =
            GetSlotData(index);

        // 데이터 없는 슬롯은 조작 불가
        if (data == null)
        {
            return;
        }

        // 실제 소환 프리팹 없는 데이터도 조작 불가
        if (data.SummonPrefab == null)
        {
            Debug.LogWarning(
                $"[SummonManager] 슬롯 {index + 1}의 " +
                $"{data.SummonName} 데이터에 Summon Prefab이 없습니다."
            );

            return;
        }

        if (TryShowSummonFailureMessage(
                index,
                data,
                false
            ))
        {
            return;
        }

        // 같은 슬롯을 두 번째로 누르면 배치 모드
        if (selectedSlotIndex == index)
        {
            if (!isPlacementMode)
            {
                EnterPlacementMode();
            }

            return;
        }

        selectedSlotIndex =
            index;

        UpdateSlotSelectionVisual();

        if (isPlacementMode)
        {
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
            if (slotVisuals[i] != null)
            {
                slotVisuals[i].SetSelected(
                    false
                );
            }
        }
    }

    // =========================================================
    // 배치 모드
    // =========================================================

    private void EnterPlacementMode()
    {
        SummonData data =
            GetSlotData(
                selectedSlotIndex
            );

        if (data == null)
        {
            return;
        }

        if (data.SummonPrefab == null)
        {
            return;
        }

        // =====================================================
        // 중요:
        // PreviewSprite가 없어도 배치/소환 가능.
        // 프리뷰 그림만 안 보일 뿐이다.
        // =====================================================

        isPlacementMode =
            true;

        if (summonRangeVisual != null)
        {
            summonRangeVisual.SetActive(
                true
            );
        }

        if (placementCancelButton != null)
        {
            placementCancelButton.SetActive(
                true
            );
        }

        CreatePreview();

        // 들어간 직후 현재 마우스 위치도 계산
        UpdatePlacementPreview();
    }

    // =========================================================
    // 프리뷰 생성
    // =========================================================

    private void CreatePreview()
    {
        DestroyPreview();

        SummonData data =
            GetSlotData(
                selectedSlotIndex
            );

        if (data == null)
        {
            return;
        }

        currentPreviewRoot =
            new GameObject(
                "Runtime_SummonPreview"
            );

        previewMemberRenderers.Clear();

        int count =
            Mathf.Max(
                1,
                data.SpawnCount
            );

        // =====================================================
        // PreviewSprite가 있을 때만 소환수 몸 프리뷰 생성
        // 없어도 소환진 프리뷰와 실제 배치는 계속 작동
        // =====================================================

        if (data.PreviewSprite != null)
        {
            for (int i = 0;
                 i < count;
                 i++)
            {
                GameObject member =
                    new GameObject(
                        "PreviewMember_" +
                        (i + 1)
                    );

                member.transform.SetParent(
                    currentPreviewRoot.transform,
                    false
                );

                member.transform.localPosition =
                    GetGroupPreviewOffset(
                        data.SummonBodySize,
                        count,
                        i
                    );

                SpriteRenderer renderer =
                    member.AddComponent<SpriteRenderer>();

                renderer.sprite =
                    data.PreviewSprite;

                renderer.color =
                    previewUnitColor;

                renderer.sortingLayerName =
                    previewSortingLayer;

                renderer.sortingOrder =
                    previewUnitOrder;

                previewMemberRenderers.Add(
                    renderer
                );
            }
        }

        // 프리뷰 원은 별도로 생성
        if (placementPreviewCircleSprite != null)
        {
            GameObject circle =
                new GameObject(
                    "PreviewCircle"
                );

            circle.transform.SetParent(
                currentPreviewRoot.transform,
                false
            );

            circle.transform.localPosition =
                Vector3.zero;

            circle.transform.localScale =
                Vector3.one *
                data.PreviewCircleScale;

            previewCircleRenderer =
                circle.AddComponent<SpriteRenderer>();

            previewCircleRenderer.sprite =
                placementPreviewCircleSprite;

            previewCircleRenderer.color =
                validPreviewCircleColor;

            previewCircleRenderer.sortingLayerName =
                previewSortingLayer;

            previewCircleRenderer.sortingOrder =
                previewCircleOrder;
        }
    }

    private Vector2 GetGroupPreviewOffset(
        SummonBodySize bodySize,
        int count,
        int index)
    {
        if (count <= 1)
        {
            return Vector2.zero;
        }

        float radius =
            GetSpawnSpreadRadius(
                bodySize,
                count
            );

        float angle =
            (
                360f /
                count
            ) *
            index +
            22.5f;

        float rad =
            angle *
            Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(rad),
            Mathf.Sin(rad)
        ) *
        radius;
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
        }

        FindRibel();

        if (mainCamera == null ||
            ribel == null)
        {
            currentPlacementValid =
                false;

            return;
        }

        Vector3 mouse =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Vector2 mouseWorld =
            new Vector2(
                mouse.x,
                mouse.y
            );

        currentPlacementPosition =
            ClampPositionInsideSummonRange(
                mouseWorld
            );

        if (currentPreviewRoot != null)
        {
            currentPreviewRoot
                .transform.position =
                currentPlacementPosition;
        }

        currentPlacementValid =
            CheckPlacementValid(
                currentPlacementPosition
            );

        if (previewCircleRenderer != null)
        {
            previewCircleRenderer.color =
                currentPlacementValid
                    ? validPreviewCircleColor
                    : invalidPreviewCircleColor;
        }
    }

    // =========================================================
    // 실제 소환 가능 사거리
    // =========================================================

    private float GetActualSummonRange()
    {
        if (summonRangeRenderer != null)
        {
            Bounds bounds =
                summonRangeRenderer.bounds;

            float spriteRadius =
                Mathf.Min(
                    bounds.extents.x,
                    bounds.extents.y
                );

            return spriteRadius *
                summonRangeVisibleRadiusRatio;
        }

        return fallbackSummonRange;
    }

    // =========================================================
    // 현재 프리뷰 원 반지름
    // =========================================================

    private float GetCurrentPreviewRadius()
    {
        if (previewCircleRenderer != null &&
            previewCircleRenderer.sprite != null)
        {
            Bounds bounds =
                previewCircleRenderer.bounds;

            float spriteRadius =
                Mathf.Min(
                    bounds.extents.x,
                    bounds.extents.y
                );

            return spriteRadius *
                previewCircleVisibleRadiusRatio;
        }

        if (currentPreviewRoot == null ||
            previewMemberRenderers.Count == 0)
        {
            return 0f;
        }

        Vector2 center =
            currentPreviewRoot.transform.position;

        float maxRadius =
            0f;

        for (int i = 0;
             i < previewMemberRenderers.Count;
             i++)
        {
            SpriteRenderer renderer =
                previewMemberRenderers[i];

            if (renderer == null ||
                renderer.sprite == null)
            {
                continue;
            }

            Bounds b =
                renderer.bounds;

            float horizontal =
                Mathf.Max(
                    Mathf.Abs(
                        b.min.x -
                        center.x
                    ),
                    Mathf.Abs(
                        b.max.x -
                        center.x
                    )
                );

            float vertical =
                Mathf.Max(
                    Mathf.Abs(
                        b.min.y -
                        center.y
                    ),
                    Mathf.Abs(
                        b.max.y -
                        center.y
                    )
                );

            float radius =
                Mathf.Max(
                    horizontal,
                    vertical
                );

            if (radius > maxRadius)
            {
                maxRadius =
                    radius;
            }
        }

        return maxRadius;
    }

    // =========================================================
    // 프리뷰가 사거리 안쪽에 완전히 들어오도록 제한
    // =========================================================

    private Vector2 ClampPositionInsideSummonRange(
        Vector2 target)
    {
        if (ribel == null)
        {
            return target;
        }

        Vector2 center =
            ribel.position;

        Vector2 offset =
            target -
            center;

        float outerRadius =
            GetActualSummonRange();

        float previewRadius =
            GetCurrentPreviewRadius();

        float centerMaxDistance =
            Mathf.Max(
                0f,
                outerRadius -
                previewRadius
            );

        offset =
            Vector2.ClampMagnitude(
                offset,
                centerMaxDistance
            );

        return center +
            offset;
    }

    // =========================================================
    // 배치 가능 여부
    // =========================================================

    private bool CheckPlacementValid(
        Vector2 position)
    {
        SummonData data =
            GetSlotData(
                selectedSlotIndex
            );

        if (data == null)
        {
            return false;
        }

        if (data.SummonPrefab == null)
        {
            return false;
        }

        if (IsSlotOnCooldown(
                selectedSlotIndex))
        {
            return false;
        }

        if (currentMana <
            GetActualManaCost(
                selectedSlotIndex
            ))
        {
            return false;
        }

        if (currentCapacity +
            data.CapacityCost >
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

        return blocked == null;
    }

    // =========================================================
    // 배치 입력
    // =========================================================

    private void HandlePlacementInput()
    {
        // 우클릭 = 배치 취소
        if (Input.GetMouseButtonDown(1))
        {
            shouldBlockRibelMoveThisFrame =
                true;

            CancelPlacement();

            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        // 배치 모드에서 발생한 좌클릭은
        // 성공/실패 여부와 무관하게 리벨 이동에 사용하지 않는다.
        shouldBlockRibelMoveThisFrame =
            true;

        // HUD 위를 클릭했다면 소환하지 않음
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 불가능한 위치면 아무 일도 하지 않음.
        // 배치 모드는 그대로 유지.
        if (!currentPlacementValid)
        {
            SummonData data =
                GetSlotData(
                    selectedSlotIndex
                );

            // 쿨타임 / 마나 / 용량 / 막힌 위치 중
            // 실제 실패 원인을 하나만 표시합니다.
            if (!TryShowSummonFailureMessage(
                    selectedSlotIndex,
                    data,
                    true
                ))
            {
                ShowSystemMessage(
                    SystemMessageUI.MessageType.InvalidPosition
                );
            }

            return;
        }

        PlaceSummon();
    }

    // =========================================================
    // 실제 소환
    // =========================================================

    private void PlaceSummon()
    {
        int slotIndex =
            selectedSlotIndex;

        SummonData data =
            GetSlotData(
                slotIndex
            );

        if (data == null)
        {
            return;
        }

        if (data.SummonPrefab == null)
        {
            Debug.LogWarning(
                $"[SummonManager] {data.SummonName}의 " +
                "Summon Prefab이 지정되지 않았습니다."
            );

            return;
        }

        if (TryShowSummonFailureMessage(
                slotIndex,
                data,
                true
            ))
        {
            return;
        }

        if (!CheckPlacementValid(
                currentPlacementPosition))
        {
            ShowSystemMessage(
                SystemMessageUI.MessageType.InvalidPosition
            );

            return;
        }

        int count =
            Mathf.Max(
                1,
                data.SpawnCount
            );

        SummonSpawnGroup group =
            new SummonSpawnGroup(
                data.SummonBodySize,
                count,
                data.CapacityCost
            );

        if (data.SummonCircleSprite != null)
        {
            CreateSummonCircle(
                data.SummonCircleSprite,
                currentPlacementPosition
            );
        }

        int successfullyCreatedCount =
            0;

        for (int i = 0;
             i < count;
             i++)
        {
            Vector2 spawnPosition =
                currentPlacementPosition +
                GetGroupPreviewOffset(
                    data.SummonBodySize,
                    count,
                    i
                );

            spawnPosition =
                ClampIndividualSpawnPosition(
                    spawnPosition
                );

            GameObject obj =
                Instantiate(
                    data.SummonPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

            if (obj == null)
            {
                continue;
            }

            SummonUnitBase unit =
                obj.GetComponent<SummonUnitBase>();

            if (unit == null)
            {
                unit =
                    obj.GetComponentInChildren<SummonUnitBase>(
                        true
                    );
            }

            if (unit != null)
            {
                unit.InitializeSummon(
                    group,
                    data
                );

                unit.BeginSummonAppearance();

                successfullyCreatedCount++;
            }
            else
            {
                Debug.LogWarning(
                    $"[SummonManager] {data.SummonName} 프리팹에서 " +
                    "SummonUnitBase를 찾지 못했습니다."
                );
            }
        }

        // 프리팹 자체 생성에 실패했다면
        // 마나/용량/쿨타임을 소비하지 않는다.
        if (successfullyCreatedCount <= 0)
        {
            Debug.LogWarning(
                $"[SummonManager] {data.SummonName} 소환에 실패했습니다."
            );

            return;
        }

        if (ribel != null)
        {
            RibelController controller =
                ribel.GetComponent<RibelController>();

            if (controller != null)
            {
                controller.PlaySummonMotion();
            }
        }

        currentMana -=
            GetActualManaCost(
                slotIndex
            );

        currentCapacity +=
            data.CapacityCost;

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

        StartCooldown(
            slotIndex
        );

        if (slotVisuals != null &&
            slotIndex >= 0 &&
            slotIndex < slotVisuals.Length &&
            slotVisuals[slotIndex] != null)
        {
            slotVisuals[
                slotIndex
            ].PlaySummonReturn();
        }

        selectedSlotIndex =
            -1;

        ExitPlacementMode();
    }

    // =========================================================
    // 개별 소환체 사거리 제한
    // =========================================================

    private Vector2 ClampIndividualSpawnPosition(
        Vector2 position)
    {
        if (ribel == null)
        {
            return position;
        }

        Vector2 center =
            ribel.position;

        Vector2 offset =
            position -
            center;

        float range =
            Mathf.Max(
                0f,
                GetActualSummonRange() -
                placementCheckRadius
            );

        offset =
            Vector2.ClampMagnitude(
                offset,
                range
            );

        return center +
            offset;
    }

    // =========================================================
    // 체급별 그룹 간격
    // =========================================================

    private float GetSpawnSpreadRadius(
        SummonBodySize bodySize,
        int count)
    {
        if (count <= 1)
        {
            return 0f;
        }

        switch (bodySize)
        {
            case SummonBodySize.Small:
                return count >= 6
                    ? 0.65f
                    : 0.5f;

            case SummonBodySize.Medium:
                return count >= 3
                    ? 0.75f
                    : 0.55f;

            case SummonBodySize.Large:
                return 0.85f;

            case SummonBodySize.Huge:
                return 1f;
        }

        return 0.5f;
    }

    // =========================================================
    // 실제 소환진
    // =========================================================

    private void CreateSummonCircle(
        Sprite sprite,
        Vector2 position)
    {
        GameObject circle =
            new GameObject(
                "SummonCircle"
            );

        circle.transform.position =
            position;

        SpriteRenderer renderer =
            circle.AddComponent<SpriteRenderer>();

        renderer.sprite =
            sprite;

        renderer.sortingLayerName =
            summonCircleSortingLayer;

        renderer.sortingOrder =
            circleSortingOrder;

        StartCoroutine(
            PlaySummonCircle(
                circle,
                renderer
            )
        );
    }

    private IEnumerator PlaySummonCircle(
        GameObject circle,
        SpriteRenderer renderer)
    {
        Transform t =
            circle.transform;

        Color baseColor =
            renderer.color;

        float timer =
            0f;

        t.localScale =
            Vector3.one *
            circleStartScale;

        while (timer <
               circleBurstTime)
        {
            timer +=
                Time.deltaTime;

            float p =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        circleBurstTime
                    )
                );

            t.localScale =
                Vector3.one *
                Mathf.Lerp(
                    circleStartScale,
                    circlePeakScale,
                    p
                );

            yield return null;
        }

        t.localScale =
            Vector3.one *
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
                    Mathf.Max(
                        0.01f,
                        circleFadeTime
                    )
                );

            t.localScale =
                Vector3.one *
                Mathf.Lerp(
                    circlePeakScale,
                    circleEndScale,
                    p
                );

            Color c =
                baseColor;

            c.a =
                1f - p;

            renderer.color =
                c;

            yield return null;
        }

        Destroy(circle);
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

        ClearSlotSelectionVisual();
    }

    private void DestroyPreview()
    {
        if (currentPreviewRoot != null)
        {
            Destroy(
                currentPreviewRoot
            );
        }

        currentPreviewRoot =
            null;

        previewCircleRenderer =
            null;

        previewMemberRenderers.Clear();
    }

    // =========================================================
    // 슬롯 데이터 교체
    // =========================================================

    public void SetSummonDataToSlot(
        int slotIndex,
        SummonData data)
    {
        if (summonSlots == null ||
            slotIndex < 0 ||
            slotIndex >= summonSlots.Length)
        {
            return;
        }

        if (summonSlots[slotIndex] == null)
        {
            summonSlots[slotIndex] =
                new SummonSlot();
        }

        summonSlots[
            slotIndex
        ].summonData =
            data;

        if (slotIndex <
            remainingCooldowns.Length)
        {
            remainingCooldowns[
                slotIndex
            ] =
                0f;
        }

        // 현재 선택된 슬롯을 비웠다면 선택/배치도 정리
        if (selectedSlotIndex ==
            slotIndex &&
            data == null)
        {
            selectedSlotIndex =
                -1;

            if (isPlacementMode)
            {
                ExitPlacementMode();
            }
        }

        RefreshSlotVisual(
            slotIndex
        );

        UpdateCooldownUI();
    }

    public float GetActualManaCost(
        int slotIndex)
    {
        SummonData data =
            GetSlotData(
                slotIndex
            );

        if (data == null)
        {
            return 0f;
        }

        return AugmentRuntimeEffects.GetManaCost(
            data.ManaCost
        );
    }

    public float GetActualCooldown(
        int slotIndex)
    {
        SummonData data =
            GetSlotData(
                slotIndex
            );

        if (data == null)
        {
            return 0f;
        }

        return AugmentRuntimeEffects.GetCooldown(
            data.Cooldown
        );
    }

    public void RefreshAugmentAffectedUI()
    {
        UpdateCooldownUI();
        RefreshAllSlotVisuals();
    }

    public SummonData GetSummonDataFromSlot(
        int slotIndex)
    {
        return GetSlotData(
            slotIndex
        );
    }

    // =========================================================
    // 마나 / 용량
    // =========================================================

    public void AddMana(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        float finalAmount =
            AugmentRuntimeEffects
                .GetManaRecoveryAmount(
                    amount
                );

        currentMana =
            Mathf.Clamp(
                currentMana +
                finalAmount,
                0f,
                maxMana
            );
    }

    public void ReleaseCapacity(
        int amount)
    {
        currentCapacity =
            Mathf.Max(
                0,
                currentCapacity -
                amount
            );
    }

    // =========================================================
    // 리벨 찾기
    // =========================================================

    private void FindRibel()
    {
        if (ribel != null)
        {
            return;
        }

        GameObject obj =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (obj != null)
        {
            ribel =
                obj.transform;
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
                0.01f,
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

        if (string.IsNullOrWhiteSpace(
                previewSortingLayer))
        {
            previewSortingLayer =
                "Effect";
        }

        if (string.IsNullOrWhiteSpace(
                summonCircleSortingLayer))
        {
            summonCircleSortingLayer =
                "Effect";
        }
    }

#endif
}