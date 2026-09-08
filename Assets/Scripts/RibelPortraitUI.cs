using UnityEngine;
using UnityEngine.UI;

public class RibelPortraitUI : MonoBehaviour
{
    public enum PortraitState
    {
        Idle,
        Walk,
        Summon,
        Hit,
        Death
    }

    [Header("리벨")]
    [Tooltip("인게임의 Ribel 오브젝트")]
    [SerializeField] private Transform ribel;

    [Tooltip("리벨의 체력 스크립트")]
    [SerializeField] private RibelHealth ribelHealth;

    [Header("초상 이미지")]
    [Tooltip("HUD에서 실제 리벨 초상을 표시하는 Image")]
    [SerializeField] private Image portraitImage;

    [Tooltip("움직임을 줄 초상 이미지의 RectTransform")]
    [SerializeField] private RectTransform portraitRoot;

    [Header("Idle 프레임")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float idleFrameRate = 4f;

    [Header("Walk 프레임")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float walkFrameRate = 6f;

    [Header("Summon 프레임")]
    [SerializeField] private Sprite[] summonFrames;
    [SerializeField] private float summonFrameRate = 8f;

    [Header("Hit 프레임")]
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private float hitFrameRate = 10f;

    [Header("Death 프레임")]
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float deathFrameRate = 6f;

    [Header("Idle 미세 움직임")]
    [Tooltip("숨 쉬는 듯 위아래로 움직이는 거리")]
    [SerializeField] private float idleBobAmount = 2f;

    [Tooltip("Idle 위아래 움직임 속도")]
    [SerializeField] private float idleBobSpeed = 1.4f;

    [Header("Walk 미세 움직임")]
    [SerializeField] private float walkBobAmount = 3f;
    [SerializeField] private float walkBobSpeed = 4f;

    [Header("피격 효과")]
    [SerializeField]
    private Color hitColor =
        new Color(1f, 0.55f, 0.55f, 1f);

    [Header("이동 판정")]
    [Tooltip("이 거리보다 많이 움직이면 Walk로 판정")]
    [SerializeField] private float moveThreshold = 0.001f;

    private PortraitState currentState =
        PortraitState.Idle;

    private Vector3 previousRibelPosition;

    private Vector2 originalPortraitPosition;

    private float animationTimer;

    private int currentFrame;

    private bool actionLocked;

    private float actionTimer;

    private float currentActionDuration;

    public PortraitState CurrentState =>
        currentState;

    private void Awake()
    {
        if (portraitImage == null)
        {
            portraitImage =
                GetComponent<Image>();
        }

        if (portraitRoot == null &&
            portraitImage != null)
        {
            portraitRoot =
                portraitImage.rectTransform;
        }

        if (portraitRoot != null)
        {
            originalPortraitPosition =
                portraitRoot.anchoredPosition;
        }

        if (ribel != null)
        {
            previousRibelPosition =
                ribel.position;
        }
    }

    private void Start()
    {
        SetState(
            PortraitState.Idle
        );
    }

    private void Update()
    {
        if (ribel == null ||
            portraitImage == null)
        {
            return;
        }

        UpdateState();

        UpdateAnimation();

        UpdatePortraitMovement();

        previousRibelPosition =
            ribel.position;
    }

    // =========================================================
    // 상태 판정
    // =========================================================

    private void UpdateState()
    {
        if (currentState ==
            PortraitState.Death)
        {
            return;
        }

        if (actionLocked)
        {
            actionTimer +=
                Time.unscaledDeltaTime;

            if (actionTimer >=
                currentActionDuration)
            {
                actionLocked = false;

                actionTimer = 0f;

                UpdateMovementState();
            }

            return;
        }

        if (ribelHealth != null &&
            ribelHealth.CurrentHealth <= 0f)
        {
            PlayDeath();
            return;
        }

        UpdateMovementState();
    }

    // =========================================================
    // Idle / Walk 자동 판정
    // =========================================================

    private void UpdateMovementState()
    {
        Vector3 movement =
            ribel.position -
            previousRibelPosition;

        bool isMoving =
            movement.sqrMagnitude >
            moveThreshold *
            moveThreshold;

        PortraitState desiredState =
            isMoving
                ? PortraitState.Walk
                : PortraitState.Idle;

        if (currentState !=
            desiredState)
        {
            SetState(
                desiredState
            );
        }
    }

    // =========================================================
    // 상태 변경
    // =========================================================

    private void SetState(
        PortraitState newState)
    {
        if (currentState ==
                newState &&
            currentFrame == 0)
        {
            return;
        }

        currentState =
            newState;

        animationTimer = 0f;

        currentFrame = 0;

        ResetPortraitColor();

        ApplyCurrentFrame();
    }

    // =========================================================
    // 외부에서 호출하는 행동
    // =========================================================

    public void PlaySummon()
    {
        if (currentState ==
            PortraitState.Death)
        {
            return;
        }

        float duration =
            GetAnimationDuration(
                summonFrames,
                summonFrameRate
            );

        BeginAction(
            PortraitState.Summon,
            duration
        );
    }

    public void PlayHit()
    {
        if (currentState ==
            PortraitState.Death)
        {
            return;
        }

        float duration =
            GetAnimationDuration(
                hitFrames,
                hitFrameRate
            );

        BeginAction(
            PortraitState.Hit,
            duration
        );

        if (portraitImage != null)
        {
            portraitImage.color =
                hitColor;
        }
    }

    public void PlayDeath()
    {
        actionLocked = true;

        actionTimer = 0f;

        currentActionDuration =
            float.MaxValue;

        SetState(
            PortraitState.Death
        );
    }

    private void BeginAction(
        PortraitState state,
        float duration)
    {
        actionLocked = true;

        actionTimer = 0f;

        currentActionDuration =
            Mathf.Max(
                0.05f,
                duration
            );

        SetState(state);
    }

    // =========================================================
    // 프레임 애니메이션
    // =========================================================

    private void UpdateAnimation()
    {
        Sprite[] frames =
            GetCurrentFrames();

        float frameRate =
            GetCurrentFrameRate();

        if (frames == null ||
            frames.Length == 0 ||
            frameRate <= 0f)
        {
            return;
        }

        animationTimer +=
            Time.unscaledDeltaTime;

        float frameDuration =
            1f / frameRate;

        if (animationTimer <
            frameDuration)
        {
            return;
        }

        animationTimer -=
            frameDuration;

        currentFrame++;

        // Death는 마지막 프레임에서 정지
        if (currentState ==
            PortraitState.Death)
        {
            if (currentFrame >=
                frames.Length)
            {
                currentFrame =
                    frames.Length - 1;
            }
        }
        // Summon / Hit도 마지막 프레임에서
        // 행동 종료 전까지 잠깐 유지
        else if (currentState ==
                     PortraitState.Summon ||
                 currentState ==
                     PortraitState.Hit)
        {
            if (currentFrame >=
                frames.Length)
            {
                currentFrame =
                    frames.Length - 1;
            }
        }
        // Idle / Walk 반복
        else
        {
            if (currentFrame >=
                frames.Length)
            {
                currentFrame = 0;
            }
        }

        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        if (portraitImage == null)
        {
            return;
        }

        Sprite[] frames =
            GetCurrentFrames();

        if (frames == null ||
            frames.Length == 0)
        {
            return;
        }

        currentFrame =
            Mathf.Clamp(
                currentFrame,
                0,
                frames.Length - 1
            );

        if (frames[currentFrame] != null)
        {
            portraitImage.sprite =
                frames[currentFrame];
        }
    }

    private Sprite[] GetCurrentFrames()
    {
        switch (currentState)
        {
            case PortraitState.Walk:
                return walkFrames;

            case PortraitState.Summon:
                return summonFrames;

            case PortraitState.Hit:
                return hitFrames;

            case PortraitState.Death:
                return deathFrames;

            default:
                return idleFrames;
        }
    }

    private float GetCurrentFrameRate()
    {
        switch (currentState)
        {
            case PortraitState.Walk:
                return walkFrameRate;

            case PortraitState.Summon:
                return summonFrameRate;

            case PortraitState.Hit:
                return hitFrameRate;

            case PortraitState.Death:
                return deathFrameRate;

            default:
                return idleFrameRate;
        }
    }

    // =========================================================
    // 초상 자체의 미세 움직임
    // =========================================================

    private void UpdatePortraitMovement()
    {
        if (portraitRoot == null)
        {
            return;
        }

        float yOffset = 0f;

        switch (currentState)
        {
            case PortraitState.Idle:

                yOffset =
                    Mathf.Sin(
                        Time.unscaledTime *
                        idleBobSpeed
                    ) *
                    idleBobAmount;

                break;

            case PortraitState.Walk:

                yOffset =
                    Mathf.Sin(
                        Time.unscaledTime *
                        walkBobSpeed
                    ) *
                    walkBobAmount;

                break;

            case PortraitState.Hit:

                float shake =
                    Mathf.Sin(
                        Time.unscaledTime *
                        55f
                    );

                portraitRoot.anchoredPosition =
                    originalPortraitPosition +
                    new Vector2(
                        shake * 2f,
                        0f
                    );

                return;
        }

        portraitRoot.anchoredPosition =
            originalPortraitPosition +
            new Vector2(
                0f,
                yOffset
            );
    }

    // =========================================================
    // 행동 길이
    // =========================================================

    private float GetAnimationDuration(
        Sprite[] frames,
        float frameRate)
    {
        if (frames == null ||
            frames.Length == 0 ||
            frameRate <= 0f)
        {
            return 0.15f;
        }

        return
            frames.Length /
            frameRate;
    }

    private void ResetPortraitColor()
    {
        if (portraitImage != null)
        {
            portraitImage.color =
                Color.white;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        idleFrameRate =
            Mathf.Max(
                0.1f,
                idleFrameRate
            );

        walkFrameRate =
            Mathf.Max(
                0.1f,
                walkFrameRate
            );

        summonFrameRate =
            Mathf.Max(
                0.1f,
                summonFrameRate
            );

        hitFrameRate =
            Mathf.Max(
                0.1f,
                hitFrameRate
            );

        deathFrameRate =
            Mathf.Max(
                0.1f,
                deathFrameRate
            );

        moveThreshold =
            Mathf.Max(
                0.0001f,
                moveThreshold
            );
    }
#endif
}