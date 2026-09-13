using System.Collections;
using UnityEngine;

public class SummonSpawnFeedback : MonoBehaviour
{
    [Header("크기 연출")]
    [Tooltip("처음 등장할 때 크기")]
    [SerializeField]
    private float startScale = 0.82f;

    [Tooltip("순간적으로 커지는 최대 크기")]
    [SerializeField]
    private float peakScale = 1.08f;

    [Tooltip("작은 상태에서 최대 크기까지 걸리는 시간")]
    [SerializeField]
    private float popTime = 0.09f;

    [Tooltip("최대 크기에서 원래 크기로 돌아오는 시간")]
    [SerializeField]
    private float returnTime = 0.08f;

    [Header("밝기 점멸")]
    [Tooltip("등장 직후 밝게 보이는 시간")]
    [SerializeField]
    private float flashTime = 0.10f;

    [Tooltip("점멸할 때 추가되는 밝기")]
    [Range(0f, 1f)]
    [SerializeField]
    private float flashStrength = 0.35f;

    [Header("선택")]
    [Tooltip(
        "비워두면 이 오브젝트의 Transform을 사용합니다. " +
        "가능하면 실제 그림이 들어있는 Transform을 넣는 것을 추천합니다."
    )]
    [SerializeField]
    private Transform visualRoot;

    private SpriteRenderer[] renderers;

    private Color[] originalColors;

    private Vector3 originalScale;

    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot =
                transform;
        }

        originalScale =
            visualRoot.localScale;

        renderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );

        originalColors =
            new Color[
                renderers.Length
            ];

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            originalColors[i] =
                renderers[i].color;
        }
    }

    private void Start()
    {
        PlayFeedback();
    }

    public void PlayFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(
                feedbackCoroutine
            );
        }

        feedbackCoroutine =
            StartCoroutine(
                FeedbackRoutine()
            );
    }

    private IEnumerator FeedbackRoutine()
    {
        // =====================================================
        // 초기 상태
        // =====================================================

        visualRoot.localScale =
            originalScale *
            startScale;

        ApplyFlash(
            flashStrength
        );

        // =====================================================
        // 1단계
        // 작게 등장 → 살짝 크게
        // =====================================================

        float timer =
            0f;

        while (timer <
               popTime)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        popTime
                    )
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
                    peakScale,
                    eased
                );

            visualRoot.localScale =
                originalScale *
                scale;

            // 점멸도 서서히 감소
            float flashT =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        flashTime
                    )
                );

            ApplyFlash(
                Mathf.Lerp(
                    flashStrength,
                    0f,
                    flashT
                )
            );

            yield return null;
        }

        // =====================================================
        // 2단계
        // 최대 크기 → 원래 크기
        // =====================================================

        timer =
            0f;

        while (timer <
               returnTime)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        returnTime
                    )
                );

            float scale =
                Mathf.Lerp(
                    peakScale,
                    1f,
                    t
                );

            visualRoot.localScale =
                originalScale *
                scale;

            yield return null;
        }

        // =====================================================
        // 완전히 원상복구
        // =====================================================

        visualRoot.localScale =
            originalScale;

        RestoreColors();

        feedbackCoroutine =
            null;
    }

    private void ApplyFlash(
        float strength)
    {
        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Color original =
                originalColors[i];

            Color bright =
                new Color(
                    Mathf.Lerp(
                        original.r,
                        1f,
                        strength
                    ),
                    Mathf.Lerp(
                        original.g,
                        1f,
                        strength
                    ),
                    Mathf.Lerp(
                        original.b,
                        1f,
                        strength
                    ),
                    original.a
                );

            renderers[i].color =
                bright;
        }
    }

    private void RestoreColors()
    {
        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].color =
                originalColors[i];
        }
    }

    private void OnDisable()
    {
        if (visualRoot != null)
        {
            visualRoot.localScale =
                originalScale;
        }

        RestoreColors();
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
                startScale,
                peakScale
            );

        popTime =
            Mathf.Max(
                0.01f,
                popTime
            );

        returnTime =
            Mathf.Max(
                0.01f,
                returnTime
            );

        flashTime =
            Mathf.Max(
                0.01f,
                flashTime
            );
    }

#endif
}