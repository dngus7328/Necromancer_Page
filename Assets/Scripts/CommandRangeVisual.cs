using UnityEngine;

public class CommandRangeVisual : MonoBehaviour
{
    public static CommandRangeVisual Instance { get; private set; }

    [Header("집결 가능 범위")]
    [SerializeField] private float commandRange = 9f;

    [Header("집결 범위 원")]
    [Tooltip("Ribel 아래에 이미 만들어둔 CommandRangeCircle")]
    [SerializeField] private GameObject commandRangeCircle;

    [Tooltip("CommandRangeCircle의 SpriteRenderer")]
    [SerializeField] private SpriteRenderer circleRenderer;

    [Header("집결 원 색")]
    [SerializeField]
    private Color rallyColor =
        new Color(
            0.15f,
            0.75f,
            1f,
            0.25f
        );

    public float CommandRange =>
        commandRange;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        HideRange();
    }

    // =========================================================
    // 집결 가능 범위 판정
    // =========================================================

    public bool IsInsideRange(Vector2 worldPosition)
    {
        float distance =
            Vector2.Distance(
                transform.position,
                worldPosition
            );

        return distance <= commandRange;
    }

    // =========================================================
    // Q 홀드 시 표시
    // =========================================================

    public void ShowRallyRange()
    {
        if (commandRangeCircle == null)
        {
            return;
        }

        commandRangeCircle.SetActive(true);

        if (circleRenderer != null)
        {
            circleRenderer.color =
                rallyColor;
        }
    }

    // =========================================================
    // 숨기기
    // =========================================================

    public void HideRange()
    {
        if (commandRangeCircle != null)
        {
            commandRangeCircle.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        commandRange =
            Mathf.Max(
                0.1f,
                commandRange
            );
    }
#endif
}