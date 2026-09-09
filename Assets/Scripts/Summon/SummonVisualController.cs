using System;
using System.Collections;
using UnityEngine;

public class SummonVisualController : MonoBehaviour
{
    private enum MotionType
    {
        Idle,
        Walk,
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

    [Header("참조")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("방향 안정화")]
    [Tooltip("가로/세로 차이가 이 값보다 작으면 현재 방향 유지")]
    [SerializeField] private float directionSwitchMargin = 0.15f;

    [Header("IDLE - 아래 / 오른쪽 / 위")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private int idleColumns = 4;
    [SerializeField] private float idleFrameRate = 5f;

    [Header("WALK - 아래 / 오른쪽 / 위")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private int walkColumns = 6;
    [SerializeField] private float walkFrameRate = 8f;

    [Header("ATTACK - 아래 / 오른쪽 / 위")]
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private int attackColumns = 5;
    [SerializeField] private float attackFrameRate = 10f;

    [Tooltip("실제 데미지가 들어가는 프레임. 첫 프레임 = 0")]
    [SerializeField] private int attackHitFrame = 2;

    [Header("HIT - 공통")]
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private float hitFrameRate = 10f;

    [Header("DEATH - 아래 / 오른쪽 / 위")]
    [SerializeField] private Sprite[] deathFrames;

    [Tooltip("한 방향당 Death 프레임 개수")]
    [SerializeField] private int deathColumns = 4;

    [SerializeField] private float deathFrameRate = 8f;

    [Tooltip("죽음 애니메이션 종료 후 마지막 프레임 유지 시간")]
    [SerializeField] private float deathHoldTime = 0.6f;

    [Tooltip("쓰러진 모습이 서서히 사라지는 시간")]
    [SerializeField] private float deathFadeTime = 0.7f;

    [Header("피격 피드백")]
    [SerializeField]
    private Color hitFlashColor =
        new Color(
            1f,
            0.35f,
            0.35f,
            1f
        );

    [SerializeField] private float hitFlashDuration = 0.09f;

    [Header("소환 등장")]
    [SerializeField] private float summonAppearDuration = 0.22f;

    [Range(0.1f, 1f)]
    [SerializeField] private float summonStartScale = 0.82f;

    [SerializeField] private float summonRiseDistance = 0.12f;

    private MotionType currentMotion =
        MotionType.Idle;

    private FacingType currentFacing =
        FacingType.Down;

    // 공격/죽음 시작 순간 방향
    private FacingType lockedActionFacing =
        FacingType.Down;

    private bool isMoving;
    private bool actionLocked;

    private bool isDead;
    private bool deathFinished;
    private bool isDeathSequenceRunning;

    private bool isSummonAppearing;

    private float animationTimer;
    private int currentFrame;

    private bool attackDamageTriggered;

    private Action attackHitCallback;
    private Action actionFinishedCallback;

    private Color originalColor;

    private Vector3 originalSpriteScale;
    private Vector3 originalSpriteLocalPosition;

    private Coroutine hitFlashCoroutine;
    private Coroutine summonAppearCoroutine;
    private Coroutine deathSequenceCoroutine;

    public bool IsDead =>
        isDead;

    public bool IsSummonAppearing =>
        isSummonAppearing;

    // =========================================================
    // 초기화
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

            originalSpriteScale =
                spriteRenderer.transform.localScale;

            originalSpriteLocalPosition =
                spriteRenderer.transform.localPosition;
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
    // 소환 등장
    // =========================================================

    public void PlaySummonAppearance(
        Action onFinished = null)
    {
        if (spriteRenderer == null)
        {
            onFinished?.Invoke();
            return;
        }

        if (summonAppearCoroutine != null)
        {
            StopCoroutine(
                summonAppearCoroutine
            );
        }

        summonAppearCoroutine =
            StartCoroutine(
                SummonAppearanceRoutine(
                    onFinished
                )
            );
    }

    private IEnumerator SummonAppearanceRoutine(
        Action onFinished)
    {
        isSummonAppearing =
            true;

        actionLocked =
            true;

        isMoving =
            false;

        Transform spriteTransform =
            spriteRenderer.transform;

        Color startColor =
            originalColor;

        startColor.a =
            0f;

        spriteRenderer.color =
            startColor;

        spriteTransform.localScale =
            originalSpriteScale *
            summonStartScale;

        spriteTransform.localPosition =
            originalSpriteLocalPosition +
            Vector3.down *
            summonRiseDistance;

        float duration =
            Mathf.Max(
                0.01f,
                summonAppearDuration
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

            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    3f
                );

            Color color =
                originalColor;

            color.a =
                Mathf.Lerp(
                    0f,
                    originalColor.a,
                    eased
                );

            spriteRenderer.color =
                color;

            spriteTransform.localScale =
                Vector3.Lerp(
                    originalSpriteScale *
                    summonStartScale,
                    originalSpriteScale,
                    eased
                );

            spriteTransform.localPosition =
                Vector3.Lerp(
                    originalSpriteLocalPosition +
                    Vector3.down *
                    summonRiseDistance,
                    originalSpriteLocalPosition,
                    eased
                );

            yield return null;
        }

        spriteRenderer.color =
            originalColor;

        spriteTransform.localScale =
            originalSpriteScale;

        spriteTransform.localPosition =
            originalSpriteLocalPosition;

        isSummonAppearing =
            false;

        actionLocked =
            false;

        summonAppearCoroutine =
            null;

        UpdateCurrentSprite();

        onFinished?.Invoke();
    }

    // =========================================================
    // 피격 점멸
    // =========================================================

    private void PlayHitFlash()
    {
        if (spriteRenderer == null ||
            isDead)
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
    }

    private IEnumerator HitFlashRoutine()
    {
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
    // 이동
    // =========================================================

    public void SetMoving(
        bool moving)
    {
        if (isDead ||
            isSummonAppearing)
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
        if (actionLocked ||
            isSummonAppearing ||
            direction.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        direction.Normalize();

        currentFacing =
            DetermineFacing(
                direction
            );

        UpdateCurrentSprite();
    }

    private FacingType DetermineFacing(
        Vector2 direction)
    {
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
            return direction.x >= 0f
                ? FacingType.Right
                : FacingType.Left;
        }

        if (absY >
            absX +
            directionSwitchMargin)
        {
            return direction.y >= 0f
                ? FacingType.Up
                : FacingType.Down;
        }

        return currentFacing;
    }

    public Vector2 GetFacingDirection()
    {
        switch (currentFacing)
        {
            case FacingType.Up:
                return Vector2.up;

            case FacingType.Right:
                return Vector2.right;

            case FacingType.Left:
                return Vector2.left;

            default:
                return Vector2.down;
        }
    }

    // =========================================================
    // Idle / Walk 자동 전환
    // =========================================================

    private void UpdateAutomaticMotion()
    {
        if (isDead ||
            actionLocked ||
            isSummonAppearing)
        {
            return;
        }

        ChangeMotion(
            isMoving
                ? MotionType.Walk
                : MotionType.Idle
        );
    }

    // =========================================================
    // 공격
    // =========================================================

    public void PlayAttack(
        Action onHit = null,
        Action onFinished = null)
    {
        if (isDead ||
            isSummonAppearing)
        {
            return;
        }

        // 공격 시작 순간 방향 저장
        lockedActionFacing =
            currentFacing;

        attackHitCallback =
            onHit;

        actionFinishedCallback =
            onFinished;

        attackDamageTriggered =
            false;

        actionLocked =
            true;

        currentMotion =
            MotionType.Attack;

        currentFrame =
            0;

        animationTimer =
            0f;

        if (attackFrames == null ||
            attackFrames.Length <
            attackColumns * 3)
        {
            TriggerAttackHit();
            FinishAttackMotion();
            return;
        }

        UpdateCurrentSprite();

        if (attackHitFrame <=
            0)
        {
            TriggerAttackHit();
        }
    }

    // =========================================================
    // 피격
    // =========================================================

    public void PlayHit()
    {
        if (isDead ||
            isSummonAppearing)
        {
            return;
        }

        PlayHitFlash();

        // 공격 중에는 공격 애니메이션 유지
        if (currentMotion ==
            MotionType.Attack)
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

        currentMotion =
            MotionType.Hit;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
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

        deathFinished =
            false;

        isDeathSequenceRunning =
            false;

        isMoving =
            false;

        actionLocked =
            true;

        // 죽는 순간 방향 저장
        lockedActionFacing =
            currentFacing;

        if (summonAppearCoroutine != null)
        {
            StopCoroutine(
                summonAppearCoroutine
            );

            summonAppearCoroutine =
                null;
        }

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(
                hitFlashCoroutine
            );

            hitFlashCoroutine =
                null;
        }

        if (deathSequenceCoroutine != null)
        {
            StopCoroutine(
                deathSequenceCoroutine
            );

            deathSequenceCoroutine =
                null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;

            spriteRenderer.transform.localScale =
                originalSpriteScale;

            spriteRenderer.transform.localPosition =
                originalSpriteLocalPosition;
        }

        attackHitCallback =
            null;

        actionFinishedCallback =
            onFinished;

        currentMotion =
            MotionType.Death;

        currentFrame =
            0;

        animationTimer =
            0f;

        // Death 배열이 잘못되어 있어도
        // 바로 삭제하지 않고 현재 모습으로 페이드 처리
        if (deathFrames == null ||
            deathFrames.Length <
            deathColumns * 3)
        {
            StartDeathSequence();
            return;
        }

        UpdateCurrentSprite();
    }

    // =========================================================
    // 애니메이션
    // =========================================================

    private void UpdateAnimation()
    {
        if (deathFinished ||
            isDeathSequenceRunning ||
            isSummonAppearing)
        {
            return;
        }

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
            Time.deltaTime;

        float frameDuration =
            1f /
            frameRate;

        while (animationTimer >=
               frameDuration)
        {
            animationTimer -=
                frameDuration;

            currentFrame++;

            switch (currentMotion)
            {
                case MotionType.Idle:

                    if (currentFrame >=
                        idleColumns)
                    {
                        currentFrame =
                            0;
                    }

                    break;

                case MotionType.Walk:

                    if (currentFrame >=
                        walkColumns)
                    {
                        currentFrame =
                            0;
                    }

                    break;

                case MotionType.Attack:

                    if (!attackDamageTriggered &&
                        currentFrame >=
                        attackHitFrame)
                    {
                        TriggerAttackHit();
                    }

                    if (currentFrame >=
                        attackColumns)
                    {
                        FinishAttackMotion();
                        return;
                    }

                    break;

                case MotionType.Hit:

                    if (currentFrame >=
                        hitFrames.Length)
                    {
                        FinishHitMotion();
                        return;
                    }

                    break;

                case MotionType.Death:

                    if (currentFrame >=
                        deathColumns)
                    {
                        // 마지막 쓰러진 프레임 유지
                        currentFrame =
                            deathColumns - 1;

                        UpdateCurrentSprite();

                        StartDeathSequence();

                        return;
                    }

                    break;
            }

            UpdateCurrentSprite();
        }
    }

    // =========================================================
    // 공격 타격
    // =========================================================

    private void TriggerAttackHit()
    {
        if (attackDamageTriggered)
        {
            return;
        }

        attackDamageTriggered =
            true;

        Action callback =
            attackHitCallback;

        attackHitCallback =
            null;

        callback?.Invoke();
    }

    // =========================================================
    // 공격 종료
    // =========================================================

    private void FinishAttackMotion()
    {
        if (!attackDamageTriggered)
        {
            TriggerAttackHit();
        }

        actionLocked =
            false;

        // 공격하던 방향 그대로 유지
        currentFacing =
            lockedActionFacing;

        currentMotion =
            MotionType.Idle;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();

        Action callback =
            actionFinishedCallback;

        actionFinishedCallback =
            null;

        callback?.Invoke();
    }

    // =========================================================
    // 피격 종료
    // =========================================================

    private void FinishHitMotion()
    {
        actionLocked =
            false;

        currentMotion =
            isMoving
                ? MotionType.Walk
                : MotionType.Idle;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
    }

    // =========================================================
    // 죽음 유지 + 페이드아웃
    // =========================================================

    private void StartDeathSequence()
    {
        if (isDeathSequenceRunning ||
            deathFinished)
        {
            return;
        }

        isDeathSequenceRunning =
            true;

        deathSequenceCoroutine =
            StartCoroutine(
                DeathSequenceRoutine()
            );
    }

    private IEnumerator DeathSequenceRoutine()
    {
        // -----------------------------------------------------
        // 1. 쓰러진 마지막 모습 잠깐 유지
        // -----------------------------------------------------

        if (deathHoldTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    deathHoldTime
                );
        }

        // -----------------------------------------------------
        // 2. 서서히 투명해짐
        // -----------------------------------------------------

        if (spriteRenderer != null &&
            deathFadeTime > 0f)
        {
            Color startColor =
                spriteRenderer.color;

            float startAlpha =
                startColor.a;

            float timer =
                0f;

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
                        startAlpha,
                        0f,
                        t
                    );

                spriteRenderer.color =
                    color;

                yield return null;
            }

            Color finalColor =
                startColor;

            finalColor.a =
                0f;

            spriteRenderer.color =
                finalColor;
        }

        // -----------------------------------------------------
        // 3. 완전히 사라진 뒤 삭제 콜백
        // -----------------------------------------------------

        FinishDeath();
    }

    private void FinishDeath()
    {
        if (deathFinished)
        {
            return;
        }

        deathFinished =
            true;

        isDeathSequenceRunning =
            false;

        deathSequenceCoroutine =
            null;

        Action callback =
            actionFinishedCallback;

        actionFinishedCallback =
            null;

        callback?.Invoke();
    }

    // =========================================================
    // 현재 배열
    // =========================================================

    private Sprite[] GetCurrentFrames()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkFrames;

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

    private float GetCurrentFrameRate()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkFrameRate;

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

    // =========================================================
    // Sprite 적용
    // =========================================================

    private void UpdateCurrentSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (currentMotion)
        {
            case MotionType.Attack:

                ApplyDirectionalAnimation(
                    attackFrames,
                    attackColumns,
                    lockedActionFacing
                );

                break;

            case MotionType.Death:

                ApplyDirectionalAnimation(
                    deathFrames,
                    deathColumns,
                    lockedActionFacing
                );

                break;

            case MotionType.Walk:

                ApplyDirectionalAnimation(
                    walkFrames,
                    walkColumns,
                    currentFacing
                );

                break;

            case MotionType.Hit:

                ApplySingleAnimation(
                    hitFrames
                );

                break;

            default:

                ApplyDirectionalAnimation(
                    idleFrames,
                    idleColumns,
                    currentFacing
                );

                break;
        }
    }

    // =========================================================
    // 방향 애니메이션
    //
    // 아래
    // 오른쪽
    // 위
    //
    // 왼쪽 = 오른쪽 Flip X
    // =========================================================

    private void ApplyDirectionalAnimation(
        Sprite[] frames,
        int columns,
        FacingType facing)
    {
        if (frames == null ||
            columns <= 0 ||
            frames.Length <
            columns * 3)
        {
            return;
        }

        int row;
        bool flipX =
            false;

        switch (facing)
        {
            case FacingType.Up:

                row =
                    2;

                break;

            case FacingType.Right:

                row =
                    1;

                break;

            case FacingType.Left:

                row =
                    1;

                flipX =
                    true;

                break;

            default:

                row =
                    0;

                break;
        }

        int frame =
            Mathf.Clamp(
                currentFrame,
                0,
                columns - 1
            );

        int index =
            row *
            columns +
            frame;

        spriteRenderer.flipX =
            flipX;

        spriteRenderer.sprite =
            frames[index];
    }

    // =========================================================
    // 방향 없는 애니메이션
    // =========================================================

    private void ApplySingleAnimation(
        Sprite[] frames)
    {
        if (frames == null ||
            frames.Length == 0)
        {
            return;
        }

        int index =
            Mathf.Clamp(
                currentFrame,
                0,
                frames.Length - 1
            );

        spriteRenderer.sprite =
            frames[index];
    }

    // =========================================================
    // 상태 변경
    // =========================================================

    private void ChangeMotion(
        MotionType newMotion)
    {
        if (currentMotion ==
            newMotion)
        {
            return;
        }

        currentMotion =
            newMotion;

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        directionSwitchMargin =
            Mathf.Clamp(
                directionSwitchMargin,
                0f,
                0.5f
            );

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

        attackColumns =
            Mathf.Max(
                1,
                attackColumns
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

        attackFrameRate =
            Mathf.Max(
                0.1f,
                attackFrameRate
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

        deathHoldTime =
            Mathf.Max(
                0f,
                deathHoldTime
            );

        deathFadeTime =
            Mathf.Max(
                0f,
                deathFadeTime
            );

        hitFlashDuration =
            Mathf.Max(
                0.01f,
                hitFlashDuration
            );

        summonAppearDuration =
            Mathf.Max(
                0.01f,
                summonAppearDuration
            );

        summonRiseDistance =
            Mathf.Max(
                0f,
                summonRiseDistance
            );
    }
#endif
}