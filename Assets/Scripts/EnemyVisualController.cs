using System;
using System.Collections;
using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{
    private enum MotionType
    {
        Idle,
        Walk,
        Telegraph,
        Attack,
        Hit,
        Death
    }

    private enum FacingType
    {
        Down,
        Right,
        Up,
        Left
    }

    // =========================================================
    // 참조
    // =========================================================

    [Header("참조")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    // =========================================================
    // 방향
    // =========================================================

    [Header("방향")]

    [Tooltip(
        "가로/세로 방향 차이가 너무 작으면 " +
        "현재 방향을 유지합니다."
    )]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float directionSwitchMargin = 0.15f;

    // =========================================================
    // Idle
    // =========================================================

    [Header("IDLE - 아래 / 오른쪽 / 위")]

    [Tooltip(
        "순서: Down → Right → Up\n" +
        "왼쪽은 Right를 자동으로 뒤집어서 사용합니다."
    )]
    [SerializeField]
    private Sprite[] idleFrames;

    [Tooltip("방향 하나당 프레임 수")]
    [Min(1)]
    [SerializeField]
    private int idleColumns = 4;

    [Min(0.1f)]
    [SerializeField]
    private float idleFrameRate = 5f;

    // =========================================================
    // Walk
    // =========================================================

    [Header("MOVE - 아래 / 오른쪽 / 위")]

    [SerializeField]
    private Sprite[] walkFrames;

    [Tooltip("방향 하나당 프레임 수")]
    [Min(1)]
    [SerializeField]
    private int walkColumns = 6;

    [Min(0.1f)]
    [SerializeField]
    private float walkFrameRate = 8f;

    // =========================================================
    // Telegraph
    // =========================================================

    [Header("TELEGRAPH - 공격 전조")]

    [Tooltip(
        "도굴꾼이 삽을 들어 공격을 예고하는 프레임.\n" +
        "순서: Down → Right → Up"
    )]
    [SerializeField]
    private Sprite[] telegraphFrames;

    [Tooltip("방향 하나당 전조 프레임 수")]
    [Min(1)]
    [SerializeField]
    private int telegraphColumns = 2;

    [Min(0.1f)]
    [SerializeField]
    private float telegraphFrameRate = 7f;

    // =========================================================
    // Attack
    // =========================================================

    [Header("ATTACK - 아래 / 오른쪽 / 위")]

    [SerializeField]
    private Sprite[] attackFrames;

    [Tooltip("방향 하나당 프레임 수")]
    [Min(1)]
    [SerializeField]
    private int attackColumns = 5;

    [Min(0.1f)]
    [SerializeField]
    private float attackFrameRate = 10f;

    [Tooltip(
        "실제 피해가 들어가는 Attack 프레임.\n" +
        "첫 번째 프레임 = 0"
    )]
    [Min(0)]
    [SerializeField]
    private int attackHitFrame = 2;

    // =========================================================
    // Hit
    // =========================================================

    [Header("HIT")]

    [Tooltip(
        "Down / Right / Up 방향 시트를 넣어도 되고,\n" +
        "공통 2프레임만 넣어도 됩니다."
    )]
    [SerializeField]
    private Sprite[] hitFrames;

    [Min(1)]
    [SerializeField]
    private int hitColumns = 2;

    [Min(0.1f)]
    [SerializeField]
    private float hitFrameRate = 10f;

    // =========================================================
    // Death
    // =========================================================

    [Header("DEATH - 아래 / 오른쪽 / 위")]

    [SerializeField]
    private Sprite[] deathFrames;

    [Tooltip("방향 하나당 프레임 수")]
    [Min(1)]
    [SerializeField]
    private int deathColumns = 5;

    [Min(0.1f)]
    [SerializeField]
    private float deathFrameRate = 8f;

    [Tooltip("마지막 사망 프레임 유지 시간")]
    [Min(0f)]
    [SerializeField]
    private float deathHoldTime = 0.45f;

    [Tooltip("사망 후 사라지는 시간")]
    [Min(0f)]
    [SerializeField]
    private float deathFadeTime = 0.4f;

    // =========================================================
    // 피격 색상
    // =========================================================

    [Header("피격 피드백")]

    [SerializeField]
    private Color hitFlashColor =
        new Color(
            1f,
            0.35f,
            0.35f,
            1f
        );

    [Min(0.01f)]
    [SerializeField]
    private float hitFlashDuration = 0.08f;

    // =========================================================
    // Runtime
    // =========================================================

    private MotionType currentMotion =
        MotionType.Idle;

    private FacingType currentFacing =
        FacingType.Down;

    private FacingType lockedActionFacing =
        FacingType.Down;

    private bool isMoving;

    private bool actionLocked;

    private bool isDead;

    private bool deathFinishing;

    private float animationTimer;

    private int currentFrame;

    private bool attackHitTriggered;

    private Action attackHitCallback;

    private Action actionFinishedCallback;

    private Color originalColor;

    private Coroutine hitFlashCoroutine;

    private Coroutine deathCoroutine;

    // =========================================================
    // Property
    // =========================================================

    public bool IsDead =>
        isDead;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }
    }

    private void Start()
    {
        currentMotion =
            MotionType.Idle;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        UpdateAutomaticMotion();

        UpdateAnimation();
    }

    // =========================================================
    // 이동 상태
    // =========================================================

    public void SetMoving(
        bool moving)
    {
        if (isDead)
        {
            isMoving =
                false;

            return;
        }

        isMoving =
            moving;
    }

    // =========================================================
    // 방향
    // =========================================================

    public void SetFacingDirection(
        Vector2 direction)
    {
        if (isDead ||
            actionLocked ||
            direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

        float absX =
            Mathf.Abs(
                direction.x
            );

        float absY =
            Mathf.Abs(
                direction.y
            );

        if (absX >
            absY +
            directionSwitchMargin)
        {
            currentFacing =
                direction.x >= 0f
                    ? FacingType.Right
                    : FacingType.Left;
        }
        else if (absY >
                 absX +
                 directionSwitchMargin)
        {
            currentFacing =
                direction.y >= 0f
                    ? FacingType.Up
                    : FacingType.Down;
        }

        UpdateCurrentSprite();
    }

    public void SetCombatFacingDirection(
        Vector2 direction)
    {
        if (isDead ||
            actionLocked ||
            direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

        float absX =
            Mathf.Abs(
                direction.x
            );

        float absY =
            Mathf.Abs(
                direction.y
            );

        if (absX >= absY)
        {
            currentFacing =
                direction.x >= 0f
                    ? FacingType.Right
                    : FacingType.Left;
        }
        else
        {
            currentFacing =
                direction.y >= 0f
                    ? FacingType.Up
                    : FacingType.Down;
        }

        UpdateCurrentSprite();
    }

    // =========================================================
    // 자동 Idle / Walk
    // =========================================================

    private void UpdateAutomaticMotion()
    {
        if (isDead ||
            actionLocked)
        {
            return;
        }

        MotionType wantedMotion =
            isMoving
                ? MotionType.Walk
                : MotionType.Idle;

        if (currentMotion ==
            wantedMotion)
        {
            return;
        }

        currentMotion =
            wantedMotion;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
    }

    // =========================================================
    // 공격
    // =========================================================

    public void PlayAttack(
        Action onHit,
        Action onFinished)
    {
        if (isDead)
        {
            onFinished?.Invoke();

            return;
        }

        actionLocked =
            true;

        isMoving =
            false;

        lockedActionFacing =
            currentFacing;

        attackHitCallback =
            onHit;

        actionFinishedCallback =
            onFinished;

        attackHitTriggered =
            false;

        currentFrame =
            0;

        animationTimer =
            0f;

        // 전조 이미지가 있으면 먼저 전조
        if (telegraphFrames != null &&
            telegraphFrames.Length > 0)
        {
            currentMotion =
                MotionType.Telegraph;
        }
        else
        {
            currentMotion =
                MotionType.Attack;
        }

        UpdateCurrentSprite();

        if (currentMotion ==
            MotionType.Attack)
        {
            TryTriggerAttackHit();
        }
    }

    private void BeginAttackAnimation()
    {
        if (isDead)
        {
            return;
        }

        currentMotion =
            MotionType.Attack;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();

        TryTriggerAttackHit();
    }

    private void TryTriggerAttackHit()
    {
        if (attackHitTriggered)
        {
            return;
        }

        if (currentFrame !=
            attackHitFrame)
        {
            return;
        }

        attackHitTriggered =
            true;

        Action callback =
            attackHitCallback;

        attackHitCallback =
            null;

        callback?.Invoke();
    }

    private void FinishAttackAnimation()
    {
        if (!attackHitTriggered)
        {
            attackHitTriggered =
                true;

            Action hitCallback =
                attackHitCallback;

            attackHitCallback =
                null;

            hitCallback?.Invoke();
        }

        actionLocked =
            false;

        currentMotion =
            MotionType.Idle;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();

        Action finishedCallback =
            actionFinishedCallback;

        actionFinishedCallback =
            null;

        finishedCallback?.Invoke();
    }

    // =========================================================
    // 피격
    // =========================================================

    public void PlayHit()
    {
        if (isDead)
        {
            return;
        }

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(
                hitFlashCoroutine
            );
        }

        hitFlashCoroutine =
            StartCoroutine(
                HitFlashRoutine()
            );

        // 공격 중에는 공격 애니메이션은 끊지 않음
        if (actionLocked)
        {
            return;
        }

        if (hitFrames == null ||
            hitFrames.Length == 0)
        {
            return;
        }

        actionLocked =
            true;

        isMoving =
            false;

        lockedActionFacing =
            currentFacing;

        currentMotion =
            MotionType.Hit;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
    }

    private void FinishHitAnimation()
    {
        actionLocked =
            false;

        currentMotion =
            MotionType.Idle;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        Color flash =
            hitFlashColor;

        flash.a =
            originalColor.a;

        spriteRenderer.color =
            flash;

        yield return
            new WaitForSeconds(
                hitFlashDuration
            );

        if (!isDead)
        {
            spriteRenderer.color =
                originalColor;
        }

        hitFlashCoroutine =
            null;
    }

    // =========================================================
    // 사망
    // =========================================================

    public void PlayDeath(
        Action onFinished = null)
    {
        if (isDead)
        {
            return;
        }

        isDead =
            true;

        actionLocked =
            true;

        isMoving =
            false;

        deathFinishing =
            false;

        lockedActionFacing =
            currentFacing;

        attackHitCallback =
            null;

        actionFinishedCallback =
            onFinished;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(
                hitFlashCoroutine
            );

            hitFlashCoroutine =
                null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
        }

        currentMotion =
            MotionType.Death;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();

        // 사망 프레임이 없으면 바로 종료 처리
        if (deathFrames == null ||
            deathFrames.Length == 0)
        {
            BeginDeathFinish();
        }
    }

    private void BeginDeathFinish()
    {
        if (deathFinishing)
        {
            return;
        }

        deathFinishing =
            true;

        if (deathCoroutine != null)
        {
            StopCoroutine(
                deathCoroutine
            );
        }

        deathCoroutine =
            StartCoroutine(
                DeathFinishRoutine()
            );
    }

    private IEnumerator DeathFinishRoutine()
    {
        if (deathHoldTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    deathHoldTime
                );
        }

        if (spriteRenderer != null &&
            deathFadeTime > 0f)
        {
            float timer =
                0f;

            Color startColor =
                spriteRenderer.color;

            while (timer <
                   deathFadeTime)
            {
                timer +=
                    Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        deathFadeTime
                    );

                Color color =
                    startColor;

                color.a =
                    Mathf.Lerp(
                        startColor.a,
                        0f,
                        t
                    );

                spriteRenderer.color =
                    color;

                yield return null;
            }
        }

        Action callback =
            actionFinishedCallback;

        actionFinishedCallback =
            null;

        deathCoroutine =
            null;

        callback?.Invoke();
    }

    // =========================================================
    // 애니메이션 갱신
    // =========================================================

    private void UpdateAnimation()
    {
        Sprite[] frames =
            GetCurrentFrames();

        int frameCount =
            GetCurrentFrameCount();

        float frameRate =
            GetCurrentFrameRate();

        if (frames == null ||
            frames.Length == 0 ||
            frameCount <= 0)
        {
            HandleMissingAnimation();

            return;
        }

        animationTimer +=
            Time.deltaTime;

        float frameDuration =
            1f /
            Mathf.Max(
                0.1f,
                frameRate
            );

        while (animationTimer >=
               frameDuration)
        {
            animationTimer -=
                frameDuration;

            currentFrame++;

            // =============================================
            // 반복 애니메이션
            // =============================================

            if (currentMotion ==
                    MotionType.Idle ||
                currentMotion ==
                    MotionType.Walk)
            {
                if (currentFrame >=
                    frameCount)
                {
                    currentFrame =
                        0;
                }

                UpdateCurrentSprite();

                continue;
            }

            // =============================================
            // 공격 전조 종료
            // =============================================

            if (currentMotion ==
                MotionType.Telegraph)
            {
                if (currentFrame >=
                    frameCount)
                {
                    BeginAttackAnimation();

                    return;
                }

                UpdateCurrentSprite();

                continue;
            }

            // =============================================
            // 공격
            // =============================================

            if (currentMotion ==
                MotionType.Attack)
            {
                if (currentFrame >=
                    frameCount)
                {
                    FinishAttackAnimation();

                    return;
                }

                UpdateCurrentSprite();

                TryTriggerAttackHit();

                continue;
            }

            // =============================================
            // 피격
            // =============================================

            if (currentMotion ==
                MotionType.Hit)
            {
                if (currentFrame >=
                    frameCount)
                {
                    FinishHitAnimation();

                    return;
                }

                UpdateCurrentSprite();

                continue;
            }

            // =============================================
            // 사망
            // =============================================

            if (currentMotion ==
                MotionType.Death)
            {
                if (currentFrame >=
                    frameCount)
                {
                    currentFrame =
                        Mathf.Max(
                            0,
                            frameCount - 1
                        );

                    UpdateCurrentSprite();

                    BeginDeathFinish();

                    return;
                }

                UpdateCurrentSprite();
            }
        }
    }

    // =========================================================
    // 없는 애니메이션 처리
    // =========================================================

    private void HandleMissingAnimation()
    {
        switch (currentMotion)
        {
            case MotionType.Telegraph:
                BeginAttackAnimation();
                break;

            case MotionType.Attack:
                FinishAttackAnimation();
                break;

            case MotionType.Hit:
                FinishHitAnimation();
                break;

            case MotionType.Death:
                BeginDeathFinish();
                break;
        }
    }

    // =========================================================
    // 현재 Sprite 갱신
    // =========================================================

    private void UpdateCurrentSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite[] frames =
            GetCurrentFrames();

        int columns =
            GetCurrentColumns();

        if (frames == null ||
            frames.Length == 0 ||
            columns <= 0)
        {
            return;
        }

        FacingType facing =
            actionLocked
                ? lockedActionFacing
                : currentFacing;

        spriteRenderer.flipX =
            facing ==
            FacingType.Left;

        int directionRow =
            GetDirectionRow(
                facing
            );

        bool directional =
            HasDirectionalFrames(
                frames,
                columns
            );

        int spriteIndex;

        if (directional)
        {
            spriteIndex =
                directionRow *
                columns +
                Mathf.Clamp(
                    currentFrame,
                    0,
                    columns - 1
                );
        }
        else
        {
            spriteIndex =
                Mathf.Clamp(
                    currentFrame,
                    0,
                    frames.Length - 1
                );
        }

        if (spriteIndex < 0 ||
            spriteIndex >=
            frames.Length)
        {
            return;
        }

        if (frames[spriteIndex] != null)
        {
            spriteRenderer.sprite =
                frames[spriteIndex];
        }
    }

    // =========================================================
    // 방향 Row
    // =========================================================

    private int GetDirectionRow(
        FacingType facing)
    {
        switch (facing)
        {
            case FacingType.Up:
                return 2;

            case FacingType.Right:
            case FacingType.Left:
                return 1;

            default:
                return 0;
        }
    }

    // =========================================================
    // 현재 프레임 정보
    // =========================================================

    private Sprite[] GetCurrentFrames()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkFrames;

            case MotionType.Telegraph:
                return telegraphFrames;

            case MotionType.Attack:
                return attackFrames;

            case MotionType.Hit:
                return hitFrames;

            case MotionType.Death:
                return deathFrames;

            default:
                return idleFrames;
        }
    }

    private int GetCurrentColumns()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkColumns;

            case MotionType.Telegraph:
                return telegraphColumns;

            case MotionType.Attack:
                return attackColumns;

            case MotionType.Hit:
                return hitColumns;

            case MotionType.Death:
                return deathColumns;

            default:
                return idleColumns;
        }
    }

    private float GetCurrentFrameRate()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkFrameRate;

            case MotionType.Telegraph:
                return telegraphFrameRate;

            case MotionType.Attack:
                return attackFrameRate;

            case MotionType.Hit:
                return hitFrameRate;

            case MotionType.Death:
                return deathFrameRate;

            default:
                return idleFrameRate;
        }
    }

    private int GetCurrentFrameCount()
    {
        Sprite[] frames =
            GetCurrentFrames();

        int columns =
            GetCurrentColumns();

        if (frames == null ||
            frames.Length == 0)
        {
            return 0;
        }

        if (HasDirectionalFrames(
                frames,
                columns))
        {
            return columns;
        }

        return Mathf.Min(
            columns,
            frames.Length
        );
    }

    private bool HasDirectionalFrames(
        Sprite[] frames,
        int columns)
    {
        if (frames == null ||
            columns <= 0)
        {
            return false;
        }

        return frames.Length >=
               columns * 3;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        idleColumns =
            Mathf.Max(
                1,
                idleColumns
            );

        walkColumns =
            Mathf.Max(
                1,
                walkColumns
            );

        telegraphColumns =
            Mathf.Max(
                1,
                telegraphColumns
            );

        attackColumns =
            Mathf.Max(
                1,
                attackColumns
            );

        hitColumns =
            Mathf.Max(
                1,
                hitColumns
            );

        deathColumns =
            Mathf.Max(
                1,
                deathColumns
            );

        attackHitFrame =
            Mathf.Clamp(
                attackHitFrame,
                0,
                attackColumns - 1
            );
    }

#endif
}