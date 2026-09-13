using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    // =========================================================
    // 따라갈 대상
    // =========================================================

    [Header("따라갈 대상")]
    [SerializeField]
    private Transform target;

    // =========================================================
    // 카메라 기본 위치
    // =========================================================

    [Header("카메라 기본 위치")]
    [SerializeField]
    private Vector3 offset =
        new Vector3(0f, 0f, -10f);

    // =========================================================
    // 화면 기준점
    // =========================================================

    [Header("화면 기준점")]
    [Tooltip("리벨을 화면 정중앙보다 약간 위쪽에 두기 위한 값")]
    [SerializeField]
    private float verticalTargetOffset = 1.2f;

    // =========================================================
    // 카메라 데드존
    // =========================================================

    [Header("카메라 데드존")]
    [Tooltip("좌우 데드존")]
    [SerializeField]
    private float deadZoneX = 3f;

    [Tooltip("리벨이 위쪽으로 움직일 때 허용하는 데드존")]
    [SerializeField]
    private float deadZoneUp = 1.8f;

    [Tooltip("리벨이 아래쪽으로 움직일 때 허용하는 데드존")]
    [SerializeField]
    private float deadZoneDown = 0.8f;

    // =========================================================
    // 미세 흔들림 방지
    // =========================================================

    [Header("미세 흔들림 방지")]
    [Tooltip("이 값보다 작은 카메라 이동은 무시")]
    [SerializeField]
    private float deadZone = 0.01f;

    // =========================================================
    // 현재 방 카메라 경계
    // =========================================================

    [Header("현재 방 카메라 경계")]
    [Tooltip("현재 방의 CameraBounds BoxCollider2D")]
    [SerializeField]
    private BoxCollider2D cameraBounds;

    // =========================================================
    // Runtime
    // =========================================================

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
        // 핵심:
        // 게임이 시작되는 순간부터
        // 카메라를 리벨 기준으로 먼저 맞춘다.
        AlignCameraToTargetImmediately();
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

            // 타깃을 뒤늦게 찾았을 경우에도
            // 첫 프레임에 위치를 바로 맞춘다.
            AlignCameraToTargetImmediately();

            return;
        }

        Vector3 currentPosition =
            transform.position;

        Vector3 desiredPosition =
            currentPosition;

        // =====================================================
        // 리벨이 화면에서 위치해야 하는 기준점
        // =====================================================

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

        if (differenceX >
            deadZoneX)
        {
            desiredPosition.x =
                target.position.x -
                deadZoneX;
        }
        else if (differenceX <
                 -deadZoneX)
        {
            desiredPosition.x =
                target.position.x +
                deadZoneX;
        }

        // =====================================================
        // 위쪽 데드존
        // =====================================================

        if (differenceY >
            deadZoneUp)
        {
            desiredPosition.y =
                target.position.y -
                deadZoneUp -
                verticalTargetOffset;
        }

        // =====================================================
        // 아래쪽 데드존
        // =====================================================

        else if (differenceY <
                 -deadZoneDown)
        {
            desiredPosition.y =
                target.position.y +
                deadZoneDown -
                verticalTargetOffset;
        }

        desiredPosition.z =
            offset.z;

        // =====================================================
        // 방 경계 제한
        // =====================================================

        desiredPosition =
            ClampToRoomBounds(
                desiredPosition
            );

        // =====================================================
        // 미세 이동 무시
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
    // 시작 시 리벨 기준으로 카메라 즉시 정렬
    // =========================================================

    private void AlignCameraToTargetImmediately()
    {
        if (target == null)
        {
            FindTarget();

            if (target == null)
            {
                return;
            }
        }

        // 리벨이 화면 중앙보다
        // verticalTargetOffset만큼 위에 오도록 배치
        Vector3 newPosition =
            new Vector3(
                target.position.x,
                target.position.y -
                verticalTargetOffset,
                offset.z
            );

        newPosition =
            ClampToRoomBounds(
                newPosition
            );

        transform.position =
            newPosition;
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

        // =====================================================
        // 좌우
        // =====================================================

        if (minX >
            maxX)
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

        // =====================================================
        // 상하
        // =====================================================

        if (minY >
            maxY)
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
    // 방 변경
    // =========================================================

    public void SetCameraBounds(
        BoxCollider2D newBounds)
    {
        cameraBounds =
            newBounds;

        AlignCameraToTargetImmediately();
    }

    // =========================================================
    // 타깃 변경
    // =========================================================

    public void SetTarget(
        Transform newTarget)
    {
        target =
            newTarget;

        if (target != null)
        {
            AlignCameraToTargetImmediately();
        }
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
    // Scene 창 데드존 확인
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 center =
            transform.position +
            Vector3.up *
            verticalTargetOffset;

        float width =
            deadZoneX *
            2f;

        float height =
            deadZoneUp +
            deadZoneDown;

        float centerOffsetY =
            (
                deadZoneUp -
                deadZoneDown
            ) *
            0.5f;

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