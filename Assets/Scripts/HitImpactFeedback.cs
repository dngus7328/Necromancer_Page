using System.Collections;
using UnityEngine;

public class HitImpactFeedback : MonoBehaviour
{
    // =========================================================
    // 비주얼
    // =========================================================

    [Header("피격 비주얼")]

    [Tooltip(
        "흔들릴 스프라이트 Transform. " +
        "비워두면 SpriteRenderer를 찾아 자동 사용합니다."
    )]
    [SerializeField]
    private Transform visualRoot;

    [Tooltip("피격 시 좌우 흔들림 거리")]
    [SerializeField]
    private float shakeDistance = 0.035f;

    [Tooltip("피격 흔들림 시간")]
    [SerializeField]
    private float shakeDuration = 0.09f;

    [Tooltip("피격 순간 X 방향 압축/확장")]
    [SerializeField]
    private float hitScaleX = 1.06f;

    [Tooltip("피격 순간 Y 방향 압축")]
    [SerializeField]
    private float hitScaleY = 0.94f;

    // =========================================================
    // 먼지
    // =========================================================

    [Header("피격 먼지")]

    [Tooltip("먼지가 생성될 위치 보정")]
    [SerializeField]
    private Vector2 dustOffset =
        new Vector2(0f, -0.12f);

    [Tooltip("한 번 맞을 때 생성되는 먼지 개수")]
    [Range(1, 12)]
    [SerializeField]
    private int dustCount = 5;

    [Tooltip("먼지가 퍼지는 속도")]
    [SerializeField]
    private float dustSpeed = 0.55f;

    [Tooltip("먼지 수명")]
    [SerializeField]
    private float dustLifetime = 0.25f;

    [Tooltip("먼지 크기")]
    [SerializeField]
    private float dustSize = 0.055f;

    [Tooltip("먼지가 위로 퍼지는 각도")]
    [Range(10f, 170f)]
    [SerializeField]
    private float dustSpreadAngle = 95f;

    [Tooltip("먼지 색")]
    [SerializeField]
    private Color dustColor =
        new Color(
            0.45f,
            0.42f,
            0.46f,
            0.65f
        );

    [Header("먼지 렌더")]

    [SerializeField]
    private string dustSortingLayer =
        "Effect";

    [SerializeField]
    private int dustSortingOrder = 45;

    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;

    private Coroutine hitCoroutine;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        FindVisualRoot();

        if (visualRoot != null)
        {
            originalLocalPosition =
                visualRoot.localPosition;

            originalLocalScale =
                visualRoot.localScale;
        }
    }

    // =========================================================
    // 비주얼 자동 찾기
    // =========================================================

    private void FindVisualRoot()
    {
        if (visualRoot != null)
        {
            return;
        }

        SpriteRenderer renderer =
            GetComponentInChildren<SpriteRenderer>();

        if (renderer != null)
        {
            visualRoot =
                renderer.transform;
        }
    }

    // =========================================================
    // 피격 실행
    // =========================================================

    public void PlayHitImpact()
    {
        FindVisualRoot();

        if (visualRoot != null)
        {
            if (hitCoroutine != null)
            {
                StopCoroutine(
                    hitCoroutine
                );
            }

            hitCoroutine =
                StartCoroutine(
                    HitRoutine()
                );
        }

        SpawnDust();
    }

    // =========================================================
    // 흔들림
    // =========================================================

    private IEnumerator HitRoutine()
    {
        float duration =
            Mathf.Max(
                0.01f,
                shakeDuration
            );

        float timer =
            0f;

        while (timer <
               duration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float strength =
                1f - t;

            float shake =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    4f
                ) *
                shakeDistance *
                strength;

            visualRoot.localPosition =
                originalLocalPosition +
                Vector3.right *
                shake;

            float scaleX =
                Mathf.Lerp(
                    hitScaleX,
                    1f,
                    t
                );

            float scaleY =
                Mathf.Lerp(
                    hitScaleY,
                    1f,
                    t
                );

            visualRoot.localScale =
                new Vector3(
                    originalLocalScale.x *
                    scaleX,
                    originalLocalScale.y *
                    scaleY,
                    originalLocalScale.z
                );

            yield return null;
        }

        visualRoot.localPosition =
            originalLocalPosition;

        visualRoot.localScale =
            originalLocalScale;

        hitCoroutine =
            null;
    }

    // =========================================================
    // 먼지 생성
    // =========================================================

    private void SpawnDust()
    {
        GameObject dustObject =
            new GameObject(
                "HitDust"
            );

        dustObject.transform.position =
            transform.position +
            (Vector3)dustOffset;

        ParticleSystem particleSystem =
            dustObject.AddComponent<ParticleSystem>();

        ParticleSystemRenderer particleRenderer =
            dustObject.GetComponent<ParticleSystemRenderer>();

        // -----------------------------------------------------
        // Main
        // -----------------------------------------------------

        var main =
            particleSystem.main;

        main.loop =
            false;

        main.playOnAwake =
            false;

        main.startLifetime =
            dustLifetime;

        main.startSpeed =
            dustSpeed;

        main.startSize =
            dustSize;

        main.startColor =
            dustColor;

        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        main.maxParticles =
            Mathf.Max(
                8,
                dustCount
            );

        main.gravityModifier =
            0.12f;

        // -----------------------------------------------------
        // Emission
        // -----------------------------------------------------

        var emission =
            particleSystem.emission;

        emission.enabled =
            false;

        // -----------------------------------------------------
        // Shape
        // -----------------------------------------------------

        var shape =
            particleSystem.shape;

        shape.enabled =
            true;

        shape.shapeType =
            ParticleSystemShapeType.Cone;

        shape.angle =
            dustSpreadAngle *
            0.5f;

        shape.radius =
            0.02f;

        // 위쪽으로 분출
        dustObject.transform.rotation =
            Quaternion.Euler(
                -90f,
                0f,
                0f
            );

        // -----------------------------------------------------
        // 크기 감소
        // -----------------------------------------------------

        var sizeOverLifetime =
            particleSystem.sizeOverLifetime;

        sizeOverLifetime.enabled =
            true;

        AnimationCurve sizeCurve =
            new AnimationCurve(
                new Keyframe(
                    0f,
                    1f
                ),
                new Keyframe(
                    1f,
                    0f
                )
            );

        sizeOverLifetime.size =
            new ParticleSystem.MinMaxCurve(
                1f,
                sizeCurve
            );

        // -----------------------------------------------------
        // 투명해지며 사라짐
        // -----------------------------------------------------

        var colorOverLifetime =
            particleSystem.colorOverLifetime;

        colorOverLifetime.enabled =
            true;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    Color.white,
                    0f
                ),

                new GradientColorKey(
                    Color.white,
                    1f
                )
            },
            new[]
            {
                new GradientAlphaKey(
                    1f,
                    0f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );

        colorOverLifetime.color =
            gradient;

        // -----------------------------------------------------
        // 렌더
        // -----------------------------------------------------

        particleRenderer.sortingLayerName =
            dustSortingLayer;

        particleRenderer.sortingOrder =
            dustSortingOrder;

        particleRenderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        // -----------------------------------------------------
        // 한 번 방출
        // -----------------------------------------------------

        particleSystem.Emit(
            dustCount
        );

        particleSystem.Play();

        Destroy(
            dustObject,
            dustLifetime +
            0.2f
        );
    }

    // =========================================================
    // 비활성화
    // =========================================================

    private void OnDisable()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition =
            originalLocalPosition;

        visualRoot.localScale =
            originalLocalScale;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        shakeDistance =
            Mathf.Max(
                0f,
                shakeDistance
            );

        shakeDuration =
            Mathf.Max(
                0.01f,
                shakeDuration
            );

        hitScaleX =
            Mathf.Max(
                0.01f,
                hitScaleX
            );

        hitScaleY =
            Mathf.Max(
                0.01f,
                hitScaleY
            );

        dustCount =
            Mathf.Max(
                1,
                dustCount
            );

        dustSpeed =
            Mathf.Max(
                0f,
                dustSpeed
            );

        dustLifetime =
            Mathf.Max(
                0.05f,
                dustLifetime
            );

        dustSize =
            Mathf.Max(
                0.005f,
                dustSize
            );
    }

#endif
}