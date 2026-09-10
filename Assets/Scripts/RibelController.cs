using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class RibelController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField]
    private float moveSpeed = 4.2f;

    [SerializeField]
    private float stopDistance = 0.02f;

    [Header("IDLE 모션")]
    [FormerlySerializedAs("idleFrames")]
    [SerializeField]
    private Sprite[] spriteFrames;

    [SerializeField]
    private int idleColumns = 6;

    [SerializeField]
    private float idleFrameRate = 8f;

    [Header("WALK 모션")]
    [SerializeField]
    private Sprite[] walkFrames;

    [SerializeField]
    private int walkColumns = 8;

    [SerializeField]
    private float walkFrameRate = 8f;

    [Header("SUMMON 모션")]
    [SerializeField]
    private Sprite[] summonFrames;

    [SerializeField]
    private int summonColumns = 6;

    [SerializeField]
    private float summonFrameRate = 8f;

    [Header("HIT 모션")]
    [SerializeField]
    private Sprite[] hitFrames;

    [SerializeField]
    private int hitColumns = 4;

    [SerializeField]
    private float hitFrameRate = 10f;

    [Header("DEATH 모션")]
    [Tooltip("방향과 관계없이 사용하는 단일 사망 모션")]
    [SerializeField]
    private Sprite[] deathFrames;

    [SerializeField]
    private float deathFrameRate = 8f;

    [Header("클릭 이동 이펙트")]
    [SerializeField]
    private GameObject clickMoveEffectPrefab;

    [SerializeField]
    private Vector3 clickEffectOffset =
        Vector3.zero;

    // =========================================================
    // Runtime
    // =========================================================

    private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;

    private Camera mainCamera;

    private Vector2 targetPosition;

    private bool hasTarget;

    private Vector2 lastLookDirection =
        Vector2.down;

    private float animationTimer;

    private int currentFrame;

    private GameObject currentClickEffect;

    private const int TotalRows = 5;

    private bool lockMotion;

    private bool isDead;

    public bool IsDead =>
        isDead;

    private enum MotionType
    {
        Idle,
        Walk,
        Summon,
        Hit,
        Death
    }

    private MotionType currentMotion =
        MotionType.Idle;

    private enum RowType
    {
        Down = 0,
        DownRight = 1,
        Right = 2,
        UpRight = 3,
        Up = 4
    }

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        rb =
            GetComponent<Rigidbody2D>();

        mainCamera =
            Camera.main;

        targetPosition =
            rb.position;
    }

    private void Start()
    {
        ChangeMotion(
            MotionType.Idle
        );

        UpdateCurrentSprite(
            lastLookDirection
        );
    }

    private void Update()
    {
        if (!isDead)
        {
            HandleMouseInput();
        }

        UpdateAutomaticMotion();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!isDead &&
            !lockMotion)
        {
            MoveCharacter();
        }
    }

    // =========================================================
    // 자동 모션
    // =========================================================

    private void UpdateAutomaticMotion()
    {
        if (lockMotion ||
            isDead)
        {
            return;
        }

        if (hasTarget)
        {
            ChangeMotion(
                MotionType.Walk
            );
        }
        else
        {
            ChangeMotion(
                MotionType.Idle
            );
        }
    }

    // =========================================================
    // 좌클릭 이동
    // =========================================================

    private void HandleMouseInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        // =====================================================
        // 1. 소환 배치 모드
        //
        // 가능한 위치를 눌러도,
        // 불가능한 위치를 눌러도
        // 리벨 이동에는 절대 사용하지 않는다.
        //
        // 프레임 실행 순서와 무관하게 동작한다.
        // =====================================================

        if (SummonManager.Instance != null &&
            SummonManager.Instance.IsConsumingWorldClick)
        {
            return;
        }

        // =====================================================
        // 2. Q + 좌클릭 = 위치 집결
        //
        // Q를 누른 채 클릭하는 동안
        // 그 클릭으로 리벨은 이동하지 않는다.
        // =====================================================

        if (Input.GetKey(KeyCode.Q))
        {
            return;
        }

        // =====================================================
        // 기존 한 프레임 차단값도 호환용으로 유지
        // =====================================================

        if (SummonManager.Instance != null &&
            SummonManager.Instance.ShouldBlockRibelMoveThisFrame)
        {
            return;
        }

        // =====================================================
        // UI 위 클릭
        // =====================================================

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // =====================================================
        // 카메라
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;

            if (mainCamera == null)
            {
                return;
            }
        }

        // =====================================================
        // 정상적인 일반 이동 클릭
        // =====================================================

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorld.z =
            0f;

        targetPosition =
            new Vector2(
                mouseWorld.x,
                mouseWorld.y
            );

        hasTarget =
            true;

        SpawnClickEffect(
            mouseWorld
        );
    }

    // =========================================================
    // 클릭 이펙트
    // =========================================================

    private void SpawnClickEffect(
        Vector3 position)
    {
        if (clickMoveEffectPrefab == null)
        {
            return;
        }

        if (currentClickEffect != null)
        {
            Destroy(
                currentClickEffect
            );
        }

        currentClickEffect =
            Instantiate(
                clickMoveEffectPrefab,
                position +
                clickEffectOffset,
                Quaternion.identity
            );
    }

    // =========================================================
    // 이동
    // =========================================================

    private void MoveCharacter()
    {
        if (!hasTarget)
        {
            return;
        }

        Vector2 currentPosition =
            rb.position;

        float distance =
            Vector2.Distance(
                currentPosition,
                targetPosition
            );

        if (distance <=
            stopDistance)
        {
            rb.MovePosition(
                targetPosition
            );

            hasTarget =
                false;

            return;
        }

        Vector2 nextPosition =
            Vector2.MoveTowards(
                currentPosition,
                targetPosition,
                moveSpeed *
                Time.fixedDeltaTime
            );

        rb.MovePosition(
            nextPosition
        );
    }

    // =========================================================
    // 애니메이션
    // =========================================================

    private void UpdateAnimation()
    {
        if (currentMotion ==
            MotionType.Death)
        {
            UpdateDeathAnimation();

            return;
        }

        Vector2 lookDirection =
            lastLookDirection;

        if (hasTarget &&
            !lockMotion)
        {
            Vector2 moveDirection =
                targetPosition -
                rb.position;

            if (moveDirection.sqrMagnitude >
                0.0001f)
            {
                lookDirection =
                    moveDirection.normalized;

                lastLookDirection =
                    lookDirection;
            }
        }

        Sprite[] frames =
            GetCurrentFrames();

        int columns =
            GetCurrentColumns();

        float frameRate =
            GetCurrentFrameRate();

        if (frames == null ||
            frames.Length == 0)
        {
            return;
        }

        animationTimer +=
            Time.deltaTime;

        if (animationTimer >=
            1f / frameRate)
        {
            animationTimer -=
                1f / frameRate;

            currentFrame++;

            if (currentMotion ==
                    MotionType.Summon ||
                currentMotion ==
                    MotionType.Hit)
            {
                if (currentFrame >=
                    columns)
                {
                    FinishLockedMotion();

                    return;
                }
            }
            else
            {
                if (currentFrame >=
                    columns)
                {
                    currentFrame =
                        0;
                }
            }
        }

        UpdateCurrentSprite(
            lookDirection
        );
    }

    // =========================================================
    // 사망 애니메이션
    // =========================================================

    private void UpdateDeathAnimation()
    {
        if (deathFrames == null ||
            deathFrames.Length == 0)
        {
            return;
        }

        animationTimer +=
            Time.deltaTime;

        if (animationTimer >=
            1f / deathFrameRate)
        {
            animationTimer -=
                1f / deathFrameRate;

            if (currentFrame <
                deathFrames.Length - 1)
            {
                currentFrame++;
            }
        }

        spriteRenderer.flipX =
            false;

        spriteRenderer.sprite =
            deathFrames[currentFrame];
    }

    // =========================================================
    // 현재 프레임
    // =========================================================

    private Sprite[] GetCurrentFrames()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkFrames;

            case MotionType.Summon:
                return summonFrames;

            case MotionType.Hit:
                return hitFrames;

            default:
                return spriteFrames;
        }
    }

    private int GetCurrentColumns()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkColumns;

            case MotionType.Summon:
                return summonColumns;

            case MotionType.Hit:
                return hitColumns;

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

            case MotionType.Summon:
                return summonFrameRate;

            case MotionType.Hit:
                return hitFrameRate;

            default:
                return idleFrameRate;
        }
    }

    // =========================================================
    // 방향별 Sprite
    // =========================================================

    private void UpdateCurrentSprite(
        Vector2 direction)
    {
        Sprite[] frames =
            GetCurrentFrames();

        int columns =
            GetCurrentColumns();

        if (frames == null ||
            frames.Length == 0)
        {
            return;
        }

        int requiredFrameCount =
            columns *
            TotalRows;

        if (frames.Length <
            requiredFrameCount)
        {
            return;
        }

        bool flipX;

        int row =
            GetRowIndex(
                direction,
                out flipX
            );

        int spriteIndex =
            row *
            columns +
            currentFrame;

        if (spriteIndex < 0 ||
            spriteIndex >=
            frames.Length)
        {
            return;
        }

        spriteRenderer.flipX =
            flipX;

        spriteRenderer.sprite =
            frames[spriteIndex];
    }

    // =========================================================
    // 모션 변경
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
    }

    private void FinishLockedMotion()
    {
        lockMotion =
            false;

        currentFrame =
            0;

        animationTimer =
            0f;

        if (hasTarget)
        {
            currentMotion =
                MotionType.Walk;
        }
        else
        {
            currentMotion =
                MotionType.Idle;
        }
    }

    // =========================================================
    // 소환 모션
    // =========================================================

    public void PlaySummonMotion()
    {
        if (isDead)
        {
            return;
        }

        hasTarget =
            false;

        StopMovement();

        lockMotion =
            true;

        ChangeMotion(
            MotionType.Summon
        );
    }

    // =========================================================
    // 피격 모션
    // =========================================================

    public void PlayHitMotion()
    {
        if (isDead)
        {
            return;
        }

        hasTarget =
            false;

        StopMovement();

        lockMotion =
            true;

        ChangeMotion(
            MotionType.Hit
        );
    }

    // =========================================================
    // 사망 모션
    // =========================================================

    public void PlayDeathMotion()
    {
        if (isDead)
        {
            return;
        }

        isDead =
            true;

        hasTarget =
            false;

        lockMotion =
            true;

        StopMovement();

        spriteRenderer.flipX =
            false;

        ChangeMotion(
            MotionType.Death
        );
    }

    // =========================================================
    // 이동 정지
    // =========================================================

    private void StopMovement()
    {
        rb.velocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;
    }

    // =========================================================
    // 5방향 + 좌우반전
    // =========================================================

    private int GetRowIndex(
        Vector2 direction,
        out bool flipX)
    {
        flipX =
            false;

        float x =
            direction.x;

        float y =
            direction.y;

        const float deadZone =
            0.25f;

        if (Mathf.Abs(x) <
                deadZone &&
            y >
                deadZone)
        {
            return (int)
                RowType.Up;
        }

        if (Mathf.Abs(x) <
                deadZone &&
            y <
                -deadZone)
        {
            return (int)
                RowType.Down;
        }

        if (Mathf.Abs(y) <
                deadZone &&
            x >
                deadZone)
        {
            return (int)
                RowType.Right;
        }

        if (Mathf.Abs(y) <
                deadZone &&
            x <
                -deadZone)
        {
            flipX =
                true;

            return (int)
                RowType.Right;
        }

        if (x > 0f &&
            y > 0f)
        {
            return (int)
                RowType.UpRight;
        }

        if (x > 0f &&
            y < 0f)
        {
            return (int)
                RowType.DownRight;
        }

        if (x < 0f &&
            y > 0f)
        {
            flipX =
                true;

            return (int)
                RowType.UpRight;
        }

        if (x < 0f &&
            y < 0f)
        {
            flipX =
                true;

            return (int)
                RowType.DownRight;
        }

        return (int)
            RowType.Down;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        moveSpeed =
            Mathf.Max(
                0f,
                moveSpeed
            );

        stopDistance =
            Mathf.Max(
                0.001f,
                stopDistance
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

        summonColumns =
            Mathf.Max(
                1,
                summonColumns
            );

        hitColumns =
            Mathf.Max(
                1,
                hitColumns
            );

        idleFrameRate =
            Mathf.Max(
                1f,
                idleFrameRate
            );

        walkFrameRate =
            Mathf.Max(
                1f,
                walkFrameRate
            );

        summonFrameRate =
            Mathf.Max(
                1f,
                summonFrameRate
            );

        hitFrameRate =
            Mathf.Max(
                1f,
                hitFrameRate
            );

        deathFrameRate =
            Mathf.Max(
                1f,
                deathFrameRate
            );
    }

#endif
}