using UnityEngine;

public class SummonRangeIndicator : MonoBehaviour
{
    [Header("표시할 사거리 오브젝트")]
    [SerializeField] private GameObject rangeVisual;

    private void Start()
    {
        if (rangeVisual != null)
        {
            rangeVisual.SetActive(false);
        }
    }

    private void Update()
    {
        if (rangeVisual == null)
        {
            return;
        }

        bool shouldShow =
            SummonManager.Instance != null &&
            SummonManager.Instance.IsPlacementMode;

        if (rangeVisual.activeSelf != shouldShow)
        {
            rangeVisual.SetActive(shouldShow);
        }
    }
}