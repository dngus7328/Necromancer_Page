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

    private enum RowType
    {
        Down = 0,
        Right = 1,
        Up = 2
    }

    [Header("참조")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;

    [Header("자동 전환 기준")]
    [Tooltip("이 값보다 속도가 크면 Walk로 판정")]
    [SerializeField] private float moveThreshold = 0.03f;

    [Header("IDLE (3방향 배열)")]
    [Tooltip("순서: 아래 / 오른쪽 / 위")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private int idleColumns = 4;
    [SerializeField] private float idleFrameRate = 5f;

    [Header("WALK (3방향 배열)")]
    [Tooltip("순서: 아래 / 오른쪽 / 위")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private int walkColumns = 6;
    [SerializeField] private float walkFrameRate = 8f;

    [Header("ATTACK (3방향 배열)")]
    [Tooltip("순서: 아래 / 오른쪽 / 위")]
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private int attackColumns = 5;
    [SerializeField] private float attackFrameRate = 10f;

    [Header("HIT (공통 1종)")]
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private float hitFrameRate = 10f;

    [Header("DEATH (공통 1종)")]
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float deathFrameRate = 8f;

    private const int TotalRows = 3;

    private MotionType currentMotion = MotionType.Idle;

    private Vector2 lastLookDirection = Vector2.down;

    private float animationTimer;
    private int currentFrame;

    private bool actionLocked;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void Start()
    {
        currentMotion = MotionType.Idle;
        currentFrame = 0;
        animationTimer = 0f;

        UpdateCurrentSprite();
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        UpdateLookDirection();
        UpdateAutomaticMotion();
        UpdateAnimation();
    }

    // =========================================================
    // 이동 방향 갱신
    // =========================================================

    private void UpdateLookDirection()
    {
        if (rb == null)
        {
            return;
        }

        if (actionLocked || isDead)
        {
            return;
        }

        Vector2 velocity = rb.velocity;

        if (velocity.sqrMagnitude >
            moveThreshold * moveThreshold)
        {
            lastLookDirection =
                velocity.normalized;
        }
    }

    // =========================================================
    // Idle / Walk 자동 전환
    // =========================================================

    private void UpdateAutomaticMotion()
    {
        if (isDead || actionLocked)
        {
            return;
        }

        if (rb == null)
        {
            ChangeMotion(MotionType.Idle);
            return;
        }

        bool isMoving =
            rb.velocity.sqrMagnitude >
            moveThreshold * moveThreshold;

        if (isMoving)
        {
            ChangeMotion(MotionType.Walk);
        }
        else
        {
            ChangeMotion(MotionType.Idle);
        }
    }

    // =========================================================
    // 애니메이션 재생
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
            Time.deltaTime;

        float frameDuration =
            1f / frameRate;

        if (animationTimer < frameDuration)
        {
            return;
        }

        animationTimer -=
            frameDuration;

        currentFrame++;

        switch (currentMotion)
        {
            case MotionType.Idle:
                if (currentFrame >= idleColumns)
                {
                    currentFrame = 0;
                }
                break;

            case MotionType.Walk:
                if (currentFrame >= walkColumns)
                {
                    currentFrame = 0;
                }
                break;

            case MotionType.Attack:
                if (currentFrame >= attackColumns)
                {
                    FinishOneShotMotion();
                    return;
                }
                break;

            case MotionType.Hit:
                if (currentFrame >= hitFrames.Length)
                {
                    FinishOneShotMotion();
                    return;
                }
                break;

            case MotionType.Death:
                if (currentFrame >= deathFrames.Length)
                {
                    currentFrame =
                        deathFrames.Length - 1;
                }
                break;
        }

        UpdateCurrentSprite();
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

    // =========================================================
    // 현재 FPS
    // =========================================================

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
    // 현재 스프라이트 표시
    // =========================================================

    private void UpdateCurrentSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (currentMotion)
        {
            case MotionType.Hit:
                ApplySingleAnimation(hitFrames);
                break;

            case MotionType.Death:
                ApplySingleAnimation(deathFrames);
                break;

            case MotionType.Attack:
                ApplyDirectionalAnimation(
                    attackFrames,
                    attackColumns
                );
                break;

            case MotionType.Walk:
                ApplyDirectionalAnimation(
                    walkFrames,
                    walkColumns
                );
                break;

            default:
                ApplyDirectionalAnimation(
                    idleFrames,
                    idleColumns
                );
                break;
        }
    }

    // =========================================================
    // 3방향 애니메이션
    // =========================================================

    private void ApplyDirectionalAnimation(
        Sprite[] frames,
        int columns)
    {
        if (frames == null ||
            frames.Length == 0 ||
            columns <= 0)
        {
            return;
        }

        int requiredCount =
            columns * TotalRows;

        if (frames.Length < requiredCount)
        {
            return;
        }

        bool flipX;

        int row =
            GetRowIndex(
                lastLookDirection,
                out flipX
            );

        int spriteIndex =
            row * columns +
            Mathf.Clamp(
                currentFrame,
                0,
                columns - 1
            );

        if (spriteIndex < 0 ||
            spriteIndex >= frames.Length)
        {
            return;
        }

        spriteRenderer.flipX =
            flipX;

        spriteRenderer.sprite =
            frames[spriteIndex];
    }

    // =========================================================
    // Hit / Death 공통 애니메이션
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

        spriteRenderer.flipX = false;

        spriteRenderer.sprite =
            frames[index];
    }

    // =========================================================
    // 모션 변경
    // =========================================================

    private void ChangeMotion(
        MotionType newMotion)
    {
        if (currentMotion == newMotion)
        {
            return;
        }

        currentMotion =
            newMotion;

        currentFrame = 0;
        animationTimer = 0f;

        UpdateCurrentSprite();
    }

    // =========================================================
    // Attack / Hit 종료
    // =========================================================

    private void FinishOneShotMotion()
    {
        actionLocked = false;

        currentFrame = 0;
        animationTimer = 0f;

        if (isDead)
        {
            currentMotion =
                MotionType.Death;

            UpdateCurrentSprite();
            return;
        }

        if (rb != null &&
            rb.velocity.sqrMagnitude >
            moveThreshold * moveThreshold)
        {
            currentMotion =
                MotionType.Walk;
        }
        else
        {
            currentMotion =
                MotionType.Idle;
        }

        UpdateCurrentSprite();
    }

    // =========================================================
    // 외부에서 방향 지정
    // =========================================================

    public void SetFacingDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        lastLookDirection =
            direction.normalized;

        UpdateCurrentSprite();
    }

    // =========================================================
    // 공격
    // =========================================================

    public void PlayAttack()
    {
        if (isDead)
        {
            return;
        }

        if (attackFrames == null ||
            attackFrames.Length == 0)
        {
            return;
        }

        actionLocked = true;

        ChangeMotion(
            MotionType.Attack
        );
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

        if (hitFrames == null ||
            hitFrames.Length == 0)
        {
            return;
        }

        actionLocked = true;

        ChangeMotion(
            MotionType.Hit
        );
    }

    // =========================================================
    // 사망
    // =========================================================

    public void PlayDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        actionLocked = true;

        ChangeMotion(
            MotionType.Death
        );
    }

    // =========================================================
    // 3방향 판정
    // =========================================================

    private int GetRowIndex(
        Vector2 direction,
        out bool flipX)
    {
        flipX = false;

        float x = direction.x;
        float y = direction.y;

        // 세로 방향 성분이 더 크면 위/아래
        if (Mathf.Abs(y) >
            Mathf.Abs(x))
        {
            if (y > 0f)
            {
                return (int)RowType.Up;
            }

            return (int)RowType.Down;
        }

        // 가로 방향
        if (x < 0f)
        {
            flipX = true;
        }

        return (int)RowType.Right;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveThreshold =
            Mathf.Max(
                0.001f,
                moveThreshold
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
    }
#endif
}