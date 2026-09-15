using UnityEngine;

public class RuntimePixelEffectInstance : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private Transform followTarget;
    private Vector3 followOffset;

    private Vector3 fixedStartPosition;

    private float lifetime;
    private float elapsed;

    private float riseDistance;

    private float startScale;
    private float endScale;

    private bool follow;
    private bool fadeOut;

    private int movementSteps;

    private Color baseColor;

    // =========================================================
    // 초기화 여부
    // =========================================================

    private bool initialized;

    // =========================================================
    // 초기화
    // =========================================================

    public void Initialize(
        Sprite sprite,
        Color color,
        int sortingLayerId,
        int sortingOrder,
        Vector3 worldPosition,
        Transform target,
        Vector3 targetOffset,
        bool shouldFollow,
        float effectLifetime,
        float effectRiseDistance,
        float effectStartScale,
        float effectEndScale,
        bool shouldFadeOut,
        int stepCount)
    {
        initialized = false;

        // =====================================================
        // SpriteRenderer
        // =====================================================

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite =
            sprite;

        spriteRenderer.color =
            color;

        spriteRenderer.sortingLayerID =
            sortingLayerId;

        spriteRenderer.sortingOrder =
            sortingOrder;

        // =====================================================
        // 위치
        // =====================================================

        if (IsFinite(worldPosition))
        {
            fixedStartPosition =
                worldPosition;
        }
        else
        {
            Debug.LogWarning(
                "[RuntimePixelEffectInstance] " +
                "잘못된 시작 위치가 들어왔습니다. " +
                "Vector3.zero로 보정합니다.",
                this
            );

            fixedStartPosition =
                Vector3.zero;
        }

        transform.position =
            fixedStartPosition;

        // =====================================================
        // 추적
        // =====================================================

        followTarget =
            target;

        if (IsFinite(targetOffset))
        {
            followOffset =
                targetOffset;
        }
        else
        {
            followOffset =
                Vector3.zero;
        }

        follow =
            shouldFollow;

        // =====================================================
        // 수명
        // =====================================================

        if (!IsFinite(effectLifetime) ||
            effectLifetime < 0.05f)
        {
            lifetime =
                0.15f;
        }
        else
        {
            lifetime =
                effectLifetime;
        }

        // =====================================================
        // 상승 거리
        // =====================================================

        if (IsFinite(effectRiseDistance))
        {
            riseDistance =
                effectRiseDistance;
        }
        else
        {
            riseDistance =
                0f;
        }

        // =====================================================
        // 시작 크기
        // =====================================================

        if (!IsFinite(effectStartScale) ||
            effectStartScale <= 0f)
        {
            startScale =
                1f;
        }
        else
        {
            startScale =
                effectStartScale;
        }

        // =====================================================
        // 종료 크기
        // =====================================================

        if (!IsFinite(effectEndScale) ||
            effectEndScale <= 0f)
        {
            endScale =
                startScale;
        }
        else
        {
            endScale =
                effectEndScale;
        }

        // =====================================================
        // 기타
        // =====================================================

        fadeOut =
            shouldFadeOut;

        movementSteps =
            Mathf.Clamp(
                stepCount,
                2,
                24
            );

        baseColor =
            color;

        elapsed =
            0f;

        transform.localScale =
            Vector3.one *
            startScale;

        // 반드시 마지막에 true
        initialized =
            true;
    }

    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        // Initialize를 거치지 않은 Instance는
        // 절대로 계산하지 않음
        if (!initialized)
        {
            return;
        }

        // =====================================================
        // 시간
        // =====================================================

        float deltaTime =
            Time.unscaledDeltaTime;

        if (!IsFinite(deltaTime) ||
            deltaTime < 0f)
        {
            deltaTime =
                0f;
        }

        elapsed +=
            deltaTime;

        if (!IsFinite(elapsed))
        {
            elapsed =
                0f;
        }

        // =====================================================
        // Lifetime 안전장치
        // =====================================================

        if (!IsFinite(lifetime) ||
            lifetime <= 0f)
        {
            lifetime =
                0.15f;
        }

        // =====================================================
        // 진행도
        // =====================================================

        float rawT =
            elapsed /
            lifetime;

        if (!IsFinite(rawT))
        {
            rawT =
                0f;
        }

        rawT =
            Mathf.Clamp01(
                rawT
            );

        // =====================================================
        // 도트식 단계 이동
        // =====================================================

        int safeMovementSteps =
            Mathf.Max(
                2,
                movementSteps
            );

        float denominator =
            safeMovementSteps -
            1f;

        float steppedT =
            Mathf.Round(
                rawT *
                denominator
            ) /
            denominator;

        if (!IsFinite(steppedT))
        {
            steppedT =
                rawT;
        }

        steppedT =
            Mathf.Clamp01(
                steppedT
            );

        // =====================================================
        // 기준 위치
        // =====================================================

        Vector3 basePosition =
            fixedStartPosition;

        if (follow &&
            followTarget != null)
        {
            Vector3 targetPosition =
                followTarget.position;

            if (IsFinite(targetPosition))
            {
                basePosition =
                    targetPosition +
                    followOffset;
            }
        }

        if (!IsFinite(basePosition))
        {
            basePosition =
                fixedStartPosition;
        }

        // =====================================================
        // 최종 위치
        // =====================================================

        Vector3 newPosition =
            basePosition +
            Vector3.up *
            (
                riseDistance *
                steppedT
            );

        if (IsFinite(newPosition))
        {
            transform.position =
                newPosition;
        }

        // =====================================================
        // Scale
        // =====================================================

        float scale =
            Mathf.Lerp(
                startScale,
                endScale,
                steppedT
            );

        if (!IsFinite(scale) ||
            scale <= 0f)
        {
            scale =
                Mathf.Max(
                    0.001f,
                    startScale
                );
        }

        Vector3 newScale =
            Vector3.one *
            scale;

        if (IsFinite(newScale))
        {
            transform.localScale =
                newScale;
        }

        // =====================================================
        // Fade
        // =====================================================

        if (spriteRenderer != null)
        {
            Color color =
                baseColor;

            if (fadeOut &&
                steppedT > 0.45f)
            {
                float fade =
                    Mathf.InverseLerp(
                        1f,
                        0.45f,
                        steppedT
                    );

                if (!IsFinite(fade))
                {
                    fade =
                        0f;
                }

                color.a *=
                    fade;
            }

            spriteRenderer.color =
                color;
        }

        // =====================================================
        // 종료
        // =====================================================

        if (elapsed >=
            lifetime)
        {
            Destroy(
                gameObject
            );
        }
    }

    // =========================================================
    // NaN / Infinity 확인
    // =========================================================

    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }

    private static bool IsFinite(
        Vector3 value)
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);
    }
}