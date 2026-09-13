using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DeathIrisFade : MonoBehaviour
{
    // =========================================================
    // 참조
    // =========================================================

    [Header("참조")]

    [Tooltip("리벨의 체력 컴포넌트")]
    [SerializeField]
    private RibelHealth ribelHealth;

    [Tooltip("리벨 Transform")]
    [SerializeField]
    private Transform ribel;

    // =========================================================
    // 사망 타이밍
    // =========================================================

    [Header("사망 연출 타이밍")]

    [Tooltip(
        "리벨 Death 애니메이션이 재생되는 시간"
    )]
    [SerializeField]
    private float deathAnimationWait =
        1f;

    [Tooltip(
        "Death 마지막 프레임을 보여주는 시간"
    )]
    [SerializeField]
    private float finalPoseHoldTime =
        0.5f;

    [Tooltip(
        "검은 원이 완전히 닫히는 시간"
    )]
    [SerializeField]
    private float irisCloseTime =
        0.85f;

    [Tooltip(
        "완전한 검은 화면 유지 시간"
    )]
    [SerializeField]
    private float blackHoldTime =
        0.3f;

    // =========================================================
    // 아이리스
    // =========================================================

    [Header("아이리스")]

    [Tooltip(
        "처음 원 크기. 1.2 정도면 화면 전체가 보입니다."
    )]
    [SerializeField]
    private float startRadius =
        1.2f;

    [Tooltip(
        "최종 원 크기"
    )]
    [SerializeField]
    private float endRadius =
        -0.05f;

    [Tooltip(
        "검은 영역과 투명 영역 경계의 부드러움"
    )]
    [SerializeField]
    private float edgeSoftness =
        0.035f;

    // =========================================================
    // 완료 이벤트
    // =========================================================

    [Header("완료")]

    [Tooltip(
        "완전히 검게 된 뒤 호출됩니다. " +
        "나중에 게임오버 창을 연결하면 됩니다."
    )]
    [SerializeField]
    private UnityEvent onFadeComplete;

    // =========================================================
    // Runtime
    // =========================================================

    private Image overlayImage;

    private RectTransform overlayRect;

    private Material irisMaterial;

    private Camera mainCamera;

    private bool deathSequenceStarted;

    private static readonly int CenterID =
        Shader.PropertyToID(
            "_Center"
        );

    private static readonly int RadiusID =
        Shader.PropertyToID(
            "_Radius"
        );

    private static readonly int SoftnessID =
        Shader.PropertyToID(
            "_Softness"
        );

    private static readonly int AspectID =
        Shader.PropertyToID(
            "_Aspect"
        );

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        mainCamera =
            Camera.main;

        FindRibel();

        CreateOverlay();

        ResetOverlay();
    }

    private void Update()
    {
        if (deathSequenceStarted)
        {
            return;
        }

        if (ribelHealth == null)
        {
            FindRibel();

            if (ribelHealth == null)
            {
                return;
            }
        }

        if (ribelHealth.CurrentHealth <= 0f)
        {
            deathSequenceStarted =
                true;

            StartCoroutine(
                DeathSequence()
            );
        }
    }

    // =========================================================
    // 리벨 찾기
    // =========================================================

    private void FindRibel()
    {
        if (ribel != null &&
            ribelHealth != null)
        {
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (player == null)
        {
            return;
        }

        if (ribel == null)
        {
            ribel =
                player.transform;
        }

        if (ribelHealth == null)
        {
            ribelHealth =
                player.GetComponent<RibelHealth>();

            if (ribelHealth == null)
            {
                ribelHealth =
                    player
                        .GetComponentInChildren<RibelHealth>();
            }
        }
    }

    // =========================================================
    // 오버레이 자동 생성
    // =========================================================

    private void CreateOverlay()
    {
        Shader irisShader =
            Shader.Find(
                "UI/RibelDeathIris"
            );

        if (irisShader == null)
        {
            Debug.LogError(
                "[DeathIrisFade] " +
                "UI/RibelDeathIris 셰이더를 찾을 수 없습니다."
            );

            return;
        }

        GameObject overlayObject =
            new GameObject(
                "DeathFadeOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        overlayObject
            .transform
            .SetParent(
                transform,
                false
            );

        overlayObject
            .transform
            .SetAsLastSibling();

        overlayRect =
            overlayObject
                .GetComponent<RectTransform>();

        overlayRect.anchorMin =
            Vector2.zero;

        overlayRect.anchorMax =
            Vector2.one;

        overlayRect.offsetMin =
            Vector2.zero;

        overlayRect.offsetMax =
            Vector2.zero;

        overlayRect.localScale =
            Vector3.one;

        overlayImage =
            overlayObject
                .GetComponent<Image>();

        overlayImage.color =
            Color.white;

        overlayImage.raycastTarget =
            false;

        irisMaterial =
            new Material(
                irisShader
            );

        overlayImage.material =
            irisMaterial;

        irisMaterial.SetFloat(
            SoftnessID,
            edgeSoftness
        );
    }

    // =========================================================
    // 사망 시퀀스
    // =========================================================

    private IEnumerator DeathSequence()
    {
        // -----------------------------------------------------
        // 1. Death 애니메이션 재생 대기
        // -----------------------------------------------------

        if (deathAnimationWait > 0f)
        {
            yield return
                WaitRealtime(
                    deathAnimationWait
                );
        }

        // -----------------------------------------------------
        // 2. 쓰러진 마지막 자세 유지
        // -----------------------------------------------------

        if (finalPoseHoldTime > 0f)
        {
            yield return
                WaitRealtime(
                    finalPoseHoldTime
                );
        }

        // -----------------------------------------------------
        // 3. 검은 아이리스 활성화
        // -----------------------------------------------------

        if (overlayImage == null ||
            irisMaterial == null)
        {
            yield break;
        }

        overlayImage.enabled =
            true;

        overlayImage.raycastTarget =
            true;

        overlayImage.transform
            .SetAsLastSibling();

        irisMaterial.SetFloat(
            SoftnessID,
            edgeSoftness
        );

        float duration =
            Mathf.Max(
                0.01f,
                irisCloseTime
            );

        float timer =
            0f;

        while (timer <
               duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            // 처음에는 천천히,
            // 후반부에 조금 더 빠르게 닫힘
            float eased =
                t *
                t *
                (3f - 2f * t);

            UpdateIrisCenter();

            float radius =
                Mathf.Lerp(
                    startRadius,
                    endRadius,
                    eased
                );

            irisMaterial.SetFloat(
                RadiusID,
                radius
            );

            UpdateAspect();

            yield return null;
        }

        irisMaterial.SetFloat(
            RadiusID,
            endRadius
        );

        UpdateIrisCenter();

        UpdateAspect();

        // -----------------------------------------------------
        // 4. 완전 검정 유지
        // -----------------------------------------------------

        if (blackHoldTime > 0f)
        {
            yield return
                WaitRealtime(
                    blackHoldTime
                );
        }

        // -----------------------------------------------------
        // 5. 게임오버 등 다음 연출
        // -----------------------------------------------------

        onFadeComplete?.Invoke();
    }

    // =========================================================
    // 리벨 위치 → 화면 중심
    // =========================================================

    private void UpdateIrisCenter()
    {
        if (irisMaterial == null)
        {
            return;
        }

        if (ribel == null)
        {
            irisMaterial.SetVector(
                CenterID,
                new Vector4(
                    0.5f,
                    0.5f,
                    0f,
                    0f
                )
            );

            return;
        }

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                ribel.position
            );

        float x =
            screenPosition.x /
            Mathf.Max(
                1f,
                Screen.width
            );

        float y =
            screenPosition.y /
            Mathf.Max(
                1f,
                Screen.height
            );

        irisMaterial.SetVector(
            CenterID,
            new Vector4(
                x,
                y,
                0f,
                0f
            )
        );
    }

    // =========================================================
    // 화면 비율
    // =========================================================

    private void UpdateAspect()
    {
        if (irisMaterial == null)
        {
            return;
        }

        float aspect =
            Screen.height > 0
                ? (float)Screen.width /
                  Screen.height
                : 1f;

        irisMaterial.SetFloat(
            AspectID,
            aspect
        );
    }

    // =========================================================
    // 초기 상태
    // =========================================================

    private void ResetOverlay()
    {
        if (overlayImage == null ||
            irisMaterial == null)
        {
            return;
        }

        overlayImage.enabled =
            false;

        overlayImage.raycastTarget =
            false;

        irisMaterial.SetFloat(
            RadiusID,
            startRadius
        );

        irisMaterial.SetFloat(
            SoftnessID,
            edgeSoftness
        );

        UpdateAspect();
    }

    // =========================================================
    // TimeScale 영향 없는 대기
    // =========================================================

    private IEnumerator WaitRealtime(
        float duration)
    {
        float timer =
            0f;

        while (timer <
               duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }

    // =========================================================
    // 정리
    // =========================================================

    private void OnDestroy()
    {
        if (irisMaterial != null)
        {
            Destroy(
                irisMaterial
            );
        }
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        deathAnimationWait =
            Mathf.Max(
                0f,
                deathAnimationWait
            );

        finalPoseHoldTime =
            Mathf.Max(
                0f,
                finalPoseHoldTime
            );

        irisCloseTime =
            Mathf.Max(
                0.01f,
                irisCloseTime
            );

        blackHoldTime =
            Mathf.Max(
                0f,
                blackHoldTime
            );

        startRadius =
            Mathf.Max(
                0f,
                startRadius
            );

        edgeSoftness =
            Mathf.Max(
                0.001f,
                edgeSoftness
            );
    }

#endif
}