using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상")]
    [SerializeField] private Transform target;

    [Header("카메라 기본 위치")]
    [SerializeField]
    private Vector3 offset =
        new Vector3(0f, 0f, -10f);

    [Header("화면 기준점")]
    [Tooltip("리벨을 화면 정중앙보다 약간 위쪽에 두기 위한 값")]
    [SerializeField] private float verticalTargetOffset = 1.2f;

    [Header("카메라 데드존")]
    [Tooltip("좌우 데드존")]
    [SerializeField] private float deadZoneX = 3f;

    [Tooltip("리벨이 위쪽으로 이동할 때 허용하는 데드존")]
    [SerializeField] private float deadZoneUp = 1.8f;

    [Tooltip("리벨이 아래쪽으로 이동할 때 허용하는 데드존")]
    [SerializeField] private float deadZoneDown = 0.8f;

    [Header("미세 흔들림 방지")]
    [Tooltip("이 값보다 작은 카메라 이동은 무시")]
    [SerializeField] private float deadZone = 0.01f;

    [Header("현재 방 카메라 경계")]
    [Tooltip("현재 방의 CameraBounds BoxCollider2D")]
    [SerializeField] private BoxCollider2D cameraBounds;

    private Camera cam;

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
        cam =
            GetComponent<Camera>();

        FindTarget();
    }

    private void Start()
    {
        ClampCameraImmediately();
    }

    // =========================================================
    // 카메라 추적
    // =========================================================

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();

            if (target == null)
            {
                return;
            }
        }

        Vector3 currentPosition =
            transform.position;

        Vector3 desiredPosition =
            currentPosition;

        // ---------------------------------------------
        // 리벨이 카메라에서 어느 위치에 있어야 하는지
        //
        // verticalTargetOffset이 양수면
        // 리벨은 화면 중앙보다 위쪽에 위치하게 됨
        // ---------------------------------------------

        float targetScreenCenterX =
            currentPosition.x;

        float targetScreenCenterY =
            currentPosition.y +
            verticalTargetOffset;

        float differenceX =
            target.position.x -
            targetScreenCenterX;

        float differenceY =
            target.position.y -
            targetScreenCenterY;

        // =====================================================
        // 좌우 데드존
        // =====================================================

        if (differenceX > deadZoneX)
        {
            desiredPosition.x =
                target.position.x -
                deadZoneX;
        }
        else if (differenceX < -deadZoneX)
        {
            desiredPosition.x =
                target.position.x +
                deadZoneX;
        }

        // =====================================================
        // 위쪽 데드존
        // =====================================================

        if (differenceY > deadZoneUp)
        {
            desiredPosition.y =
                target.position.y -
                deadZoneUp -
                verticalTargetOffset;
        }

        // =====================================================
        // 아래쪽 데드존
        // =====================================================

        else if (differenceY < -deadZoneDown)
        {
            desiredPosition.y =
                target.position.y +
                deadZoneDown -
                verticalTargetOffset;
        }

        desiredPosition.z =
            offset.z;

        // 방 밖이 보이지 않도록 Clamp
        desiredPosition =
            ClampToRoomBounds(
                desiredPosition
            );

        // =====================================================
        // 미세 흔들림 방지
        // =====================================================

        Vector2 currentXY =
            new Vector2(
                currentPosition.x,
                currentPosition.y
            );

        Vector2 desiredXY =
            new Vector2(
                desiredPosition.x,
                desiredPosition.y
            );

        float moveDistance =
            Vector2.Distance(
                currentXY,
                desiredXY
            );

        if (moveDistance <
            deadZone)
        {
            return;
        }

        transform.position =
            desiredPosition;
    }

    // =========================================================
    // 리벨 자동 찾기
    // =========================================================

    private void FindTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (player != null)
        {
            target =
                player.transform;
        }
    }

    // =========================================================
    // 방 경계 Clamp
    // =========================================================

    private Vector3 ClampToRoomBounds(
        Vector3 position)
    {
        if (cameraBounds == null ||
            cam == null)
        {
            return position;
        }

        Bounds bounds =
            cameraBounds.bounds;

        float halfHeight =
            cam.orthographicSize;

        float halfWidth =
            halfHeight *
            cam.aspect;

        float minX =
            bounds.min.x +
            halfWidth;

        float maxX =
            bounds.max.x -
            halfWidth;

        float minY =
            bounds.min.y +
            halfHeight;

        float maxY =
            bounds.max.y -
            halfHeight;

        if (minX > maxX)
        {
            position.x =
                bounds.center.x;
        }
        else
        {
            position.x =
                Mathf.Clamp(
                    position.x,
                    minX,
                    maxX
                );
        }

        if (minY > maxY)
        {
            position.y =
                bounds.center.y;
        }
        else
        {
            position.y =
                Mathf.Clamp(
                    position.y,
                    minY,
                    maxY
                );
        }

        return position;
    }

    // =========================================================
    // 시작 / 방 전환 시 즉시 Clamp
    // =========================================================

    private void ClampCameraImmediately()
    {
        if (cameraBounds == null)
        {
            return;
        }

        Vector3 clampedPosition =
            ClampToRoomBounds(
                transform.position
            );

        clampedPosition.z =
            offset.z;

        transform.position =
            clampedPosition;
    }

    // =========================================================
    // 방 변경용
    // =========================================================

    public void SetCameraBounds(
        BoxCollider2D newBounds)
    {
        cameraBounds =
            newBounds;

        ClampCameraImmediately();
    }

    // =========================================================
    // 타겟 변경용
    // =========================================================

    public void SetTarget(
        Transform newTarget)
    {
        target =
            newTarget;
    }

#if UNITY_EDITOR

    // =========================================================
    // Inspector 값 보호
    // =========================================================

    private void OnValidate()
    {
        deadZoneX =
            Mathf.Max(
                0f,
                deadZoneX
            );

        deadZoneUp =
            Mathf.Max(
                0f,
                deadZoneUp
            );

        deadZoneDown =
            Mathf.Max(
                0f,
                deadZoneDown
            );

        deadZone =
            Mathf.Max(
                0f,
                deadZone
            );
    }

    // =========================================================
    // Scene 창에서 데드존 확인
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 center =
            transform.position +
            Vector3.up *
            verticalTargetOffset;

        float width =
            deadZoneX * 2f;

        float height =
            deadZoneUp +
            deadZoneDown;

        float centerOffsetY =
            (
                deadZoneUp -
                deadZoneDown
            ) * 0.5f;

        center.y +=
            centerOffsetY;

        Gizmos.DrawWireCube(
            center,
            new Vector3(
                width,
                height,
                0f
            )
        );
    }

#endif
}