using UnityEngine;
using UnityEngine.EventSystems;

public class SummonCommandController : MonoBehaviour
{
    // =========================================================
    // Q 입력
    // =========================================================

    [Header("Q 입력")]
    [SerializeField]
    private float shortPressTime = 0.25f;

    [SerializeField]
    private float holdTime = 0.25f;

    // =========================================================
    // 리벨
    // =========================================================

    [Header("리벨")]
    [SerializeField]
    private Transform ribel;

    // =========================================================
    // 집결 위치 마커
    // =========================================================

    [Header("집결 위치 마커")]

    [Tooltip("집결 성공 위치에 표시할 월드 이펙트 프리팹")]
    [SerializeField]
    private GameObject rallyMarkerPrefab;

    [Tooltip("마커 위치 보정")]
    [SerializeField]
    private Vector3 rallyMarkerOffset =
        Vector3.zero;

    [Tooltip("새 집결 마커가 생길 때 이전 마커를 제거")]
    [SerializeField]
    private bool replacePreviousMarker =
        true;

    // =========================================================
    // Runtime
    // =========================================================

    private Camera mainCamera;

    private bool qPressed;

    private bool holdMode;

    private bool pointCommandUsed;

    private float qPressTime;

    private bool shouldBlockRibelMoveThisFrame;

    private GameObject currentRallyMarker;

    // =========================================================
    // 외부 확인
    // =========================================================

    public bool IsPointCommandMode =>
        qPressed &&
        holdMode;

    public bool ShouldBlockRibelMoveThisFrame =>
        shouldBlockRibelMoveThisFrame;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        mainCamera =
            Camera.main;

        FindRibel();
    }

    private void Update()
    {
        shouldBlockRibelMoveThisFrame =
            false;

        HandleQInput();
    }

    // =========================================================
    // Q 입력
    // =========================================================

    private void HandleQInput()
    {
        // -----------------------------------------------------
        // Q 누름 시작
        // -----------------------------------------------------

        if (Input.GetKeyDown(
                KeyCode.Q))
        {
            // 소환 배치 중에는 집결 입력 시작 안 함
            if (SummonManager.Instance != null &&
                SummonManager.Instance.IsPlacementMode)
            {
                return;
            }

            qPressed =
                true;

            holdMode =
                false;

            pointCommandUsed =
                false;

            qPressTime =
                0f;
        }

        // -----------------------------------------------------
        // Q 누르고 있는 중
        // -----------------------------------------------------

        if (qPressed &&
            Input.GetKey(
                KeyCode.Q))
        {
            qPressTime +=
                Time.deltaTime;

            // 일정 시간 이상 누르면 위치 집결 모드
            if (!holdMode &&
                qPressTime >=
                holdTime)
            {
                holdMode =
                    true;

                if (CommandRangeVisual.Instance != null)
                {
                    CommandRangeVisual.Instance
                        .ShowRallyRange();
                }
            }

            // -------------------------------------------------
            // Q 홀드 + 좌클릭
            // -------------------------------------------------

            if (holdMode &&
                Input.GetMouseButtonDown(0))
            {
                // 이 클릭은 리벨 이동에 사용하면 안 됨
                shouldBlockRibelMoveThisFrame =
                    true;

                TryRallyToMousePosition();
            }

            // -------------------------------------------------
            // Q 홀드 + 우클릭 = 취소
            // -------------------------------------------------

            if (holdMode &&
                Input.GetMouseButtonDown(1))
            {
                shouldBlockRibelMoveThisFrame =
                    true;

                EndQCommand();

                return;
            }
        }

        // -----------------------------------------------------
        // Q 뗌
        // -----------------------------------------------------

        if (qPressed &&
            Input.GetKeyUp(
                KeyCode.Q))
        {
            // 짧게 눌렀을 경우
            // 리벨 위치로 전원 집결
            if (!pointCommandUsed &&
                !holdMode &&
                qPressTime <=
                shortPressTime)
            {
                RallyAllToRibel();
            }

            EndQCommand();
        }
    }

    // =========================================================
    // 지정 위치 집결
    // =========================================================

    private void TryRallyToMousePosition()
    {
        // UI 위 클릭은 무시
        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject())
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;

            if (mainCamera == null)
            {
                return;
            }
        }

        Vector3 mouseWorld3 =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Vector2 point =
            new Vector2(
                mouseWorld3.x,
                mouseWorld3.y
            );

        // -----------------------------------------------------
        // 집결 가능 사거리 밖이면 무효
        //
        // 마커도 생성하지 않음
        // 리벨도 움직이지 않음
        // -----------------------------------------------------

        if (CommandRangeVisual.Instance != null &&
            !CommandRangeVisual.Instance
                .IsInsideRange(point))
        {
            return;
        }

        // -----------------------------------------------------
        // 실제 집결
        // -----------------------------------------------------

        RallyAllToPoint(
            point
        );

        // -----------------------------------------------------
        // 집결 성공 위치 표시
        // -----------------------------------------------------

        ShowRallyMarker(
            point
        );

        pointCommandUsed =
            true;
    }

    // =========================================================
    // Q 짧게 - 리벨에게 집결
    // =========================================================

    private void RallyAllToRibel()
    {
        FindRibel();

        if (ribel == null)
        {
            return;
        }

        SummonUnitBase[] summons =
            FindObjectsOfType<SummonUnitBase>();

        for (int i = 0;
             i < summons.Length;
             i++)
        {
            if (summons[i] == null)
            {
                continue;
            }

            summons[i]
                .RallyToRibel();
        }

        // 리벨 위치에도 집결 마커 표시
        ShowRallyMarker(
            ribel.position
        );
    }

    // =========================================================
    // 지정 위치 집결
    // =========================================================

    private void RallyAllToPoint(
        Vector2 point)
    {
        SummonUnitBase[] summons =
            FindObjectsOfType<SummonUnitBase>();

        for (int i = 0;
             i < summons.Length;
             i++)
        {
            if (summons[i] == null)
            {
                continue;
            }

            summons[i]
                .RallyToPoint(
                    point
                );
        }
    }

    // =========================================================
    // ★ 집결 마커 표시
    // =========================================================

    private void ShowRallyMarker(
        Vector2 worldPosition)
    {
        if (rallyMarkerPrefab == null)
        {
            Debug.LogWarning(
                "[SummonCommandController] " +
                "Rally Marker Prefab이 비어 있습니다.",
                gameObject
            );

            return;
        }

        // -----------------------------------------------------
        // 기존 마커 제거
        // -----------------------------------------------------

        if (replacePreviousMarker &&
            currentRallyMarker != null)
        {
            Destroy(
                currentRallyMarker
            );

            currentRallyMarker =
                null;
        }

        // -----------------------------------------------------
        // 생성 위치
        // -----------------------------------------------------

        Vector3 spawnPosition =
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f
            );

        spawnPosition +=
            rallyMarkerOffset;

        // -----------------------------------------------------
        // 실제 생성
        // -----------------------------------------------------

        currentRallyMarker =
            Instantiate(
                rallyMarkerPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (currentRallyMarker == null)
        {
            Debug.LogError(
                "[SummonCommandController] " +
                "집결 마커 생성에 실패했습니다.",
                gameObject
            );

            return;
        }

        // -----------------------------------------------------
        // SpriteRenderer가 하나라도 있는지 확인
        // -----------------------------------------------------

        SpriteRenderer[] renderers =
            currentRallyMarker
                .GetComponentsInChildren<SpriteRenderer>(
                    true
                );

        if (renderers.Length <= 0)
        {
            Debug.LogWarning(
                "[SummonCommandController] " +
                "집결 마커 프리팹 안에 SpriteRenderer가 없습니다. " +
                "월드 이펙트라면 UI Image가 아니라 SpriteRenderer를 사용하세요.",
                currentRallyMarker
            );
        }
    }

    // =========================================================
    // Q 명령 종료
    // =========================================================

    private void EndQCommand()
    {
        qPressed =
            false;

        holdMode =
            false;

        pointCommandUsed =
            false;

        qPressTime =
            0f;

        if (CommandRangeVisual.Instance != null)
        {
            CommandRangeVisual.Instance
                .HideRange();
        }
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

#if UNITY_EDITOR

    private void OnValidate()
    {
        shortPressTime =
            Mathf.Max(
                0.05f,
                shortPressTime
            );

        holdTime =
            Mathf.Max(
                shortPressTime,
                holdTime
            );
    }

#endif
}