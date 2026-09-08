using UnityEngine;

public class SummonCommandController : MonoBehaviour
{
    [Header("Q 입력")]
    [SerializeField] private float shortPressTime = 0.25f;
    [SerializeField] private float holdTime = 0.25f;

    [Header("리벨")]
    [SerializeField] private Transform ribel;

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
            qPressTime +=
                Time.deltaTime;

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

            if (holdMode &&
                Input.GetMouseButtonDown(0))
            {
                // 이 클릭은 절대로 리벨 이동에 사용하지 않음
                shouldBlockRibelMoveThisFrame = true;

                TryRallyToMousePosition();
            }

            if (holdMode &&
                Input.GetMouseButtonDown(1))
            {
                shouldBlockRibelMoveThisFrame = true;

                EndQCommand();
                return;
            }
        }

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
    // 지정 위치 집결
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

        // 집결 원 밖 클릭은 무효
        if (CommandRangeVisual.Instance != null &&
            !CommandRangeVisual.Instance
                .IsInsideRange(point))
        {
            return;
        }

        RallyAllToPoint(point);

        pointCommandUsed = true;
    }

    // =========================================================
    // 리벨 집결
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
    }

    // =========================================================
    // 위치 집결
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
    // 종료
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