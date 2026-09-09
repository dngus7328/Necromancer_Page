using System.Collections;
using UnityEngine;

public class SummonCircleEffect : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("크기")]
    [SerializeField] private float startScale = 0.65f;
    [SerializeField] private float burstScale = 1.15f;
    [SerializeField] private float endScale = 1.30f;

    [Header("시간")]
    [SerializeField] private float burstDuration = 0.07f;
    [SerializeField] private float holdDuration = 0.05f;
    [SerializeField] private float fadeDuration = 0.22f;

    [Header("표시")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 1f;

    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        // 루트에 없더라도 자식에서 자동 탐색
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "SummonCircleEffect: SpriteRenderer를 찾을 수 없습니다.",
                gameObject
            );

            return;
        }

        baseScale =
            transform.localScale;

        baseColor =
            spriteRenderer.color;

        // 혹시 알파가 0으로 저장돼 있어도 강제로 보이게
        baseColor.a = 1f;

        spriteRenderer.color =
            baseColor;
    }

    private void Start()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(
            PlayEffect()
        );
    }

    private IEnumerator PlayEffect()
    {
        // =====================================================
        // 시작
        // =====================================================

        transform.localScale =
            baseScale *
            startScale;

        Color color =
            baseColor;

        color.a =
            startAlpha;

        spriteRenderer.color =
            color;

        // =====================================================
        // 팍 커짐
        // =====================================================

        float timer = 0f;

        while (timer <
               burstDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    burstDuration
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    3f
                );

            float scale =
                Mathf.Lerp(
                    startScale,
                    burstScale,
                    eased
                );

            transform.localScale =
                baseScale *
                scale;

            color =
                baseColor;

            color.a =
                Mathf.Lerp(
                    startAlpha,
                    1f,
                    eased
                );

            spriteRenderer.color =
                color;

            yield return null;
        }

        // =====================================================
        // 잠깐 유지
        // =====================================================

        transform.localScale =
            baseScale *
            burstScale;

        spriteRenderer.color =
            baseColor;

        if (holdDuration > 0f)
        {
            yield return
                new WaitForSeconds(
                    holdDuration
                );
        }

        // =====================================================
        // 퍼지면서 사라짐
        // =====================================================

        timer = 0f;

        while (timer <
               fadeDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    fadeDuration
                );

            float scale =
                Mathf.Lerp(
                    burstScale,
                    endScale,
                    t
                );

            transform.localScale =
                baseScale *
                scale;

            color =
                baseColor;

            color.a =
                Mathf.Lerp(
                    1f,
                    0f,
                    t
                );

            spriteRenderer.color =
                color;

            yield return null;
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        startScale =
            Mathf.Max(
                0.01f,
                startScale
            );

        burstScale =
            Mathf.Max(
                startScale,
                burstScale
            );

        endScale =
            Mathf.Max(
                burstScale,
                endScale
            );

        burstDuration =
            Mathf.Max(
                0.01f,
                burstDuration
            );

        holdDuration =
            Mathf.Max(
                0f,
                holdDuration
            );

        fadeDuration =
            Mathf.Max(
                0.01f,
                fadeDuration
            );
    }
#endif
}