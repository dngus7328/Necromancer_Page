using UnityEngine;

public class PlacementCancelFollower : MonoBehaviour
{
    [Header("따라갈 대상")]
    [SerializeField] private Transform ribel;

    [Header("화면상 위치 보정")]
    [SerializeField] private Vector2 screenOffset = new Vector2(55f, 70f);

    private RectTransform rectTransform;
    private Camera mainCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (ribel == null || mainCamera == null)
        {
            return;
        }

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(ribel.position);

        rectTransform.position =
            new Vector2(
                screenPosition.x + screenOffset.x,
                screenPosition.y + screenOffset.y
            );
    }
}