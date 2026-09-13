using UnityEngine;

public class RallyMarkerEffect : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("비워두면 이 오브젝트와 자식에서 자동으로 찾습니다.")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("렌더 순서")]
    [SerializeField]
    private string sortingLayerName = "Effect";

    [SerializeField]
    private int sortingOrder = 60;

    [Header("등장 크기")]
    [Tooltip("처음 나타날 때 크기")]
    [Min(0.01f)]
    [SerializeField]
    private float startScale = 0.55f;

    [Tooltip("최대로 커졌을 때 크기")]
    [Min(0.01f)]
    [SerializeField]
    private float peakScale = 1f;

    [Header("등장 속도")]
    [Tooltip("작게 나타나서 커지는 시간")]
    [Min(0.01f)]
    [SerializeField]
    private float appearTime = 0.12f;

    [Header("유지 시간")]
    [Min(0f)]
    [SerializeField]
    private float holdTime = 0.18f;

    [Header("사라짐")]
    [Tooltip("사라지면서 마지막에 도달할 크기")]
    [Min(0.01f)]
    [SerializeField]
    private float endScale = 1.12f;

    [Min(0.01f)]
    [SerializeField]
    private float fadeTime = 0.38f;

    [Header("회전")]
    [Tooltip("0이면 회전하지 않음")]
    [SerializeField]
    private float rotationSpeed = 40f;

    private float timer;

    private Phase phase =
        Phase.Appear;

    private Color originalColor;

    private Vector3 baseScale;

    private enum Phase
    {
        Appear,
        Hold,
        Fade
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "[RallyMarkerEffect] SpriteRenderer가 없습니다.",
                gameObject
            );

            enabled = false;

            return;
        }

        originalColor =
            spriteRenderer.color;

        baseScale =
            transform.localScale;

        // 집결 이펙트가 월드 오브젝트 뒤에 묻히지 않도록
        // 항상 지정된 렌더 순서를 사용
        spriteRenderer.sortingLayerName =
            sortingLayerName;

        spriteRenderer.sortingOrder =
            sortingOrder;

        ResetEffect();
    }

    private void OnEnable()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        ResetEffect();
    }

    private void ResetEffect()
    {
        timer =
            0f;

        phase =
            Phase.Appear;

        transform.localScale =
            baseScale *
            startScale;

        spriteRenderer.color =
            originalColor;
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // 회전
        if (Mathf.Abs(rotationSpeed) >
            0.001f)
        {
            transform.Rotate(
                0f,
                0f,
                rotationSpeed *
                Time.deltaTime
            );
        }

        switch (phase)
        {
            case Phase.Appear:
                UpdateAppear();
                break;

            case Phase.Hold:
                UpdateHold();
                break;

            case Phase.Fade:
                UpdateFade();
                break;
        }
    }

    private void UpdateAppear()
    {
        timer +=
            Time.deltaTime;

        float t =
            Mathf.Clamp01(
                timer /
                Mathf.Max(
                    0.01f,
                    appearTime
                )
            );

        // 처음 빠르게 커졌다가
        // 마지막에 부드럽게 멈추는 느낌
        float eased =
            1f -
            Mathf.Pow(
                1f - t,
                3f
            );

        float scale =
            Mathf.Lerp(
                startScale,
                peakScale,
                eased
            );

        transform.localScale =
            baseScale *
            scale;

        if (t >= 1f)
        {
            timer =
                0f;

            phase =
                Phase.Hold;
        }
    }

    private void UpdateHold()
    {
        timer +=
            Time.deltaTime;

        transform.localScale =
            baseScale *
            peakScale;

        if (timer >=
            holdTime)
        {
            timer =
                0f;

            phase =
                Phase.Fade;
        }
    }

    private void UpdateFade()
    {
        timer +=
            Time.deltaTime;

        float t =
            Mathf.Clamp01(
                timer /
                Mathf.Max(
                    0.01f,
                    fadeTime
                )
            );

        float scale =
            Mathf.Lerp(
                peakScale,
                endScale,
                t
            );

        transform.localScale =
            baseScale *
            scale;

        Color color =
            originalColor;

        color.a =
            Mathf.Lerp(
                originalColor.a,
                0f,
                t
            );

        spriteRenderer.color =
            color;

        if (t >= 1f)
        {
            Destroy(
                gameObject
            );
        }
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        startScale =
            Mathf.Max(
                0.01f,
                startScale
            );

        peakScale =
            Mathf.Max(
                0.01f,
                peakScale
            );

        endScale =
            Mathf.Max(
                0.01f,
                endScale
            );

        appearTime =
            Mathf.Max(
                0.01f,
                appearTime
            );

        holdTime =
            Mathf.Max(
                0f,
                holdTime
            );

        fadeTime =
            Mathf.Max(
                0.01f,
                fadeTime
            );

        if (string.IsNullOrWhiteSpace(
                sortingLayerName))
        {
            sortingLayerName =
                "Effect";
        }
    }

#endif
}