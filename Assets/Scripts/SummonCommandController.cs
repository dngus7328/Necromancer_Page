using UnityEngine;

public class SummonCommandController : MonoBehaviour
{
    [Header("Q 입력")]
    [SerializeField] private float shortPressTime = 0.25f;
    [SerializeField] private float holdTime = 0.25f;

    [Header("리벨")]
    [SerializeField] private Transform ribel;

    [Header("집결 위치 표시")]
    [SerializeField] private RallyMarkerSpawner rallyMarkerSpawner;

    private Camera mainCamera;

    private bool qPressed;
    private bool holdMode;
    private bool pointCommandUsed;

    private float qPressTime;

    private bool shouldBlockRibelMoveThisFrame;

    public bool IsPointCommandMode =>
        qPressed && holdMode;

    public bool ShouldBlockRibelMoveThisFrame =>
        shouldBlockRibelMoveThisFrame;

    private void Awake()
    {
        mainCamera = Camera.main;

        FindRibel();
        FindRallyMarkerSpawner();
    }

    private void Update()
    {
        shouldBlockRibelMoveThisFrame = false;

        HandleQInput();
    }

    // =========================================================
    // Q 입력
    // =========================================================

    private void HandleQInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 소환 배치 중에는 집결 명령 사용 안 함
            if (SummonManager.Instance != null &&
                SummonManager.Instance.IsPlacementMode)
            {
                return;
            }

            qPressed = true;
            holdMode = false;
            pointCommandUsed = false;

            qPressTime = 0f;
        }

        if (qPressed &&
            Input.GetKey(KeyCode.Q))
        {
            qPressTime += Time.deltaTime;

            // 일정 시간 이상 Q를 누르면 지정 위치 집결 모드
            if (!holdMode &&
                qPressTime >= holdTime)
            {
                holdMode = true;

                if (CommandRangeVisual.Instance != null)
                {
                    CommandRangeVisual.Instance
                        .ShowRallyRange();
                }
            }

            // Q를 누른 상태에서 좌클릭
            if (holdMode &&
                Input.GetMouseButtonDown(0))
            {
                // 이 클릭은 리벨 이동에 사용하지 않음
                shouldBlockRibelMoveThisFrame = true;

                TryRallyToMousePosition();
            }

            // 우클릭으로 집결 모드 취소
            if (holdMode &&
                Input.GetMouseButtonDown(1))
            {
                shouldBlockRibelMoveThisFrame = true;

                EndQCommand();
                return;
            }
        }

        // Q를 짧게 눌렀다 떼면 리벨에게 집결
        if (qPressed &&
            Input.GetKeyUp(KeyCode.Q))
        {
            if (!pointCommandUsed &&
                !holdMode &&
                qPressTime <= shortPressTime)
            {
                RallyAllToRibel();
            }

            EndQCommand();
        }
    }

    // =========================================================
    // 지정 위치 집결 시도
    // =========================================================

    private void TryRallyToMousePosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

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

        // 집결 가능 범위 밖이면 무효
        if (CommandRangeVisual.Instance != null &&
            !CommandRangeVisual.Instance
                .IsInsideRange(point))
        {
            return;
        }

        // 소환수들을 클릭한 위치로 집결
        RallyAllToPoint(point);

        // 클릭한 위치에 집결 이펙트 표시
        if (rallyMarkerSpawner != null)
        {
            rallyMarkerSpawner.ShowMarker(point);
        }

        pointCommandUsed = true;
    }

    // =========================================================
    // 리벨에게 집결
    // =========================================================

    private void RallyAllToRibel()
    {
        SummonUnitBase[] summons =
            FindObjectsOfType<SummonUnitBase>();

        for (int i = 0; i < summons.Length; i++)
        {
            if (summons[i] == null)
            {
                continue;
            }

            summons[i].RallyToRibel();
        }

        // Q 단독 집결 시 리벨 위치에 집결 이펙트 표시
        if (ribel != null &&
            rallyMarkerSpawner != null)
        {
            rallyMarkerSpawner.ShowMarker(
                ribel.position
            );
        }
    }

    // =========================================================
    // 지정 위치로 집결
    // =========================================================

    private void RallyAllToPoint(Vector2 point)
    {
        SummonUnitBase[] summons =
            FindObjectsOfType<SummonUnitBase>();

        for (int i = 0; i < summons.Length; i++)
        {
            if (summons[i] == null)
            {
                continue;
            }

            summons[i].RallyToPoint(point);
        }
    }

    // =========================================================
    // Q 집결 명령 종료
    // =========================================================

    private void EndQCommand()
    {
        qPressed = false;
        holdMode = false;
        pointCommandUsed = false;

        qPressTime = 0f;

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
            ribel = ribelObject.transform;
        }
    }

    // =========================================================
    // RallyMarkerSpawner 찾기
    // =========================================================

    private void FindRallyMarkerSpawner()
    {
        if (rallyMarkerSpawner != null)
        {
            return;
        }

        rallyMarkerSpawner =
            FindObjectOfType<RallyMarkerSpawner>();
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