using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상")]
    [SerializeField] private Transform target;

    [Header("카메라 위치")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("미세 흔들림 방지")]
    [Tooltip("이 거리보다 작은 움직임은 카메라가 무시")]
    [SerializeField] private float deadZone = 0.01f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition =
            target.position + offset;

        Vector2 currentXY =
            new Vector2(
                transform.position.x,
                transform.position.y
            );

        Vector2 desiredXY =
            new Vector2(
                desiredPosition.x,
                desiredPosition.y
            );

        float distance =
            Vector2.Distance(
                currentXY,
                desiredXY
            );

        // 아주 미세한 차이는 무시
        if (distance < deadZone)
        {
            return;
        }

        transform.position =
            new Vector3(
                desiredPosition.x,
                desiredPosition.y,
                offset.z
            );
    }
}