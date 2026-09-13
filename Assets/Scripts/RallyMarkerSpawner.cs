using UnityEngine;

public class RallyMarkerSpawner : MonoBehaviour
{
    [Header("집결 위치 표시")]
    [SerializeField]
    private GameObject rallyMarkerPrefab;

    [Header("위치 보정")]
    [SerializeField]
    private Vector3 spawnOffset =
        Vector3.zero;

    private GameObject currentMarker;

    public void ShowMarker(
        Vector2 worldPosition)
    {
        if (rallyMarkerPrefab == null)
        {
            Debug.LogWarning(
                "[RallyMarkerSpawner] " +
                "Rally Marker Prefab이 지정되지 않았습니다.",
                gameObject
            );

            return;
        }

        if (currentMarker != null)
        {
            Destroy(
                currentMarker
            );

            currentMarker =
                null;
        }

        Vector3 position =
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f
            ) +
            spawnOffset;

        currentMarker =
            Instantiate(
                rallyMarkerPrefab,
                position,
                Quaternion.identity
            );
    }
}