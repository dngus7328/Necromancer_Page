using System;
using System.Collections;
using UnityEngine;

public class SummonVisualController : MonoBehaviour
{
    // =========================================================
    // 방향 방식
    // =========================================================

    public enum DirectionMode
    {
        ThreeDirections,
        FiveDirections
    }

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
        DownRight,
        Right,
        UpRight,
        Up,
        UpLeft,
        Left,
        DownLeft
    }

    // =========================================================
    // 기본 설정
    // =========================================================

    [Header("참조")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("방향 설정")]
    [Tooltip(
        "ThreeDirections = 아래/오른쪽/위\n" +
        "FiveDirections = 아래/우하/오른쪽/우상/위"
    )]
    [SerializeField]
    private DirectionMode directionMode =
        DirectionMode.ThreeDirections;

    [Tooltip("3방향에서 방향 전환이 너무 민감하지 않게 하는 값")]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float directionSwitchMargin = 0.15f;

    [Header("5방향 각도 판정")]

    [Tooltip(
        "이 각도 이상이면 위 방향으로 판정합니다.\n" +
        "값을 낮추면 Up이 더 쉽게 나옵니다."
    )]
    [Range(30f, 89f)]
    [SerializeField]
    private float upDirectionAngle = 67.5f;

    [Tooltip(
        "이 각도 이상이면 위 대각선으로 판정합니다.\n" +
        "값을 낮추면 대각선 방향이 더 쉽게 나옵니다."
    )]
    [Range(0f, 45f)]
    [SerializeField]
    private float diagonalDirectionAngle = 22.5f;

    // =========================================================
    // Idle
    // =========================================================

    [Header("IDLE")]
    [Tooltip(
        "3방향: Down → Right → Up\n" +
        "5방향: Down → DownRight → Right → UpRight → Up"
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

    [Header("WALK")]
    [Tooltip(
        "3방향: Down → Right → Up\n" +
        "5방향: Down → DownRight → Right → UpRight → Up\n" +
        "Flying / Floating은 비워둬도 됩니다."
    )]
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
    // Attack
    // =========================================================

    [Header("ATTACK")]
    [Tooltip(
        "3방향: Down → Right → Up\n" +
        "5방향: Down → DownRight → Right → UpRight → Up"
    )]
    [SerializeField]
    private Sprite[] attackFrames;

    [Tooltip("방향 하나당 프레임 수")]
    [Min(1)]
    [SerializeField]
    private int attackColumns = 5;

    [Min(0.1f)]
    [SerializeField]
    private float attackFrameRate = 10f;

    [Tooltip("실제 공격 판정이 발생하는 프레임")]
    [Min(0)]
    [SerializeField]
    private int attackHitFrame = 2;

    // =========================================================
    // Hit
    // =========================================================

    [Header("HIT")]
    [Tooltip(
        "3방향: Down → Right → Up\n" +
        "5방향: Down → DownRight → Right → UpRight → Up\n" +
        "왼쪽 계열은 오른쪽 계열 스프라이트를 자동으로 좌우 반전합니다."
    )]
    [SerializeField]
    private Sprite[] hitFrames;

    [Tooltip(
        "방향 하나당 피격 프레임 수입니다.\n" +
        "방향별 피격 이미지가 1장씩이면 1로 설정합니다."
    )]
    [Min(1)]
    [SerializeField]
    private int hitColumns = 1;

    [Min(0.1f)]
    [SerializeField]
    private float hitFrameRate = 10f;

    // =========================================================
    // Death
    // =========================================================

    [Header("DEATH")]
    [Tooltip(
        "3방향: Down → Right → Up\n" +
        "5방향: Down → DownRight → Right → UpRight → Up"
    )]
    [SerializeField]
    private Sprite[] deathFrames;

    [Tooltip("방향 하나당 프레임 수")]
    [Min(1)]
    [SerializeField]
    private int deathColumns = 4;

    [Min(0.1f)]
    [SerializeField]
    private float deathFrameRate = 8f;

    [Min(0f)]
    [SerializeField]
    private float deathHoldTime = 0.6f;

    [Min(0f)]
    [SerializeField]
    private float deathFadeTime = 0.7f;

    // =========================================================
    // 피격 피드백
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
    private float hitFlashDuration = 0.09f;

    // =========================================================
    // 피격 먼지
    // =========================================================

    [Header("피격 먼지")]
    [Tooltip("FX001_01 → FX001_05 순서로 넣습니다.")]
    [SerializeField]
    private Sprite[] hitDustFrames;

    [Tooltip("먼지 애니메이션 초당 프레임 수")]
    [Min(0.1f)]
    [SerializeField]
    private float hitDustFrameRate = 14f;

    [Tooltip("소환수 중심에서 먼지가 생길 위치입니다. 발밑이면 Y를 음수로 둡니다.")]
    [SerializeField]
    private Vector2 hitDustOffset =
        new Vector2(0f, -0.18f);

    [Tooltip("먼지 이미지 크기 배율")]
    [Min(0.01f)]
    [SerializeField]
    private float hitDustScale = 1f;

    [Tooltip("본체보다 몇 Sorting Order 아래에 그릴지 설정합니다.")]
    [SerializeField]
    private int hitDustSortingOffset = -1;

    // =========================================================
    // 능력치 상승 피드백
    // =========================================================

    [Header("능력치 상승 피드백")]
    [Tooltip("강화계약으로 소환수 능력치가 실제로 증가했을 때 재생합니다.")]
    [SerializeField]
    private bool useStatUpEffect = true;

    [Tooltip("몸이 잠깐 빛나는 시간")]
    [Min(0.05f)]
    [SerializeField]
    private float statUpDuration = 0.55f;

    [Tooltip("능력치 상승 순간 몸에 섞이는 밝은 색")]
    [SerializeField]
    private Color statUpFlashColor =
        new Color(
            1f,
            0.88f,
            0.45f,
            1f
        );

    [Tooltip("몸 주변에 나타나는 빛의 색")]
    [SerializeField]
    private Color statUpGlowColor =
        new Color(
            0.75f,
            0.5f,
            1f,
            0.42f
        );

    [Tooltip("몸 주변 빛의 크기")]
    [Min(0.05f)]
    [SerializeField]
    private float statUpGlowScale = 0.9f;

    [Tooltip("상승 화살표 색")]
    [SerializeField]
    private Color statUpArrowColor =
        new Color(
            0.9f,
            0.72f,
            1f,
            1f
        );

    [Tooltip("상승 화살표 시작 위치")]
    [SerializeField]
    private Vector2 statUpArrowOffset =
        new Vector2(
            0f,
            0.35f
        );

    [Tooltip("상승 화살표가 올라가는 거리")]
    [Min(0.05f)]
    [SerializeField]
    private float statUpArrowRiseDistance = 0.42f;

    [Tooltip("상승 화살표 크기")]
    [Min(0.05f)]
    [SerializeField]
    private float statUpArrowScale = 0.42f;

    // =========================================================
    // 소환 등장
    // =========================================================

    [Header("소환 등장")]
    [Min(0.01f)]
    [SerializeField]
    private float summonAppearDuration = 0.22f;

    [Range(0.1f, 1f)]
    [SerializeField]
    private float summonStartScale = 0.82f;

    [Min(0f)]
    [SerializeField]
    private float summonRiseDistance = 0.12f;

    // =========================================================
    // Runtime
    // =========================================================

    private SummonData summonData;

    private SummonMovementType movementType =
        SummonMovementType.Ground;

    private MotionType currentMotion =
        MotionType.Idle;

    private FacingType currentFacing =
        FacingType.Down;

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

    // =========================================================
    // Sprite 원본 상태
    // =========================================================

    private Color originalColor;

    private Vector3 originalSpriteScale;

    private Vector3 originalSpriteLocalPosition;

    // =========================================================
    // 공중 / 부유
    // =========================================================

    private float hoverPhase;

    private float currentHoverOffset;

    private bool attackBodyMotionRunning;

    // =========================================================
    // 그림자
    // =========================================================

    private GameObject shadowObject;

    private SpriteRenderer shadowRenderer;

    // =========================================================
    // Coroutine
    // =========================================================

    private Coroutine hitFlashCoroutine;

    private Coroutine summonAppearCoroutine;

    private Coroutine deathSequenceCoroutine;

    private Coroutine attackBodyCoroutine;

    private Coroutine statUpEffectCoroutine;

    private static Sprite cachedStatUpArrowSprite;

    private static Sprite cachedStatUpGlowSprite;

    // =========================================================
    // Property
    // =========================================================

    public bool IsDead =>
        isDead;

    public bool IsSummonAppearing =>
        isSummonAppearing;

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

            originalSpriteScale =
                spriteRenderer.transform.localScale;

            originalSpriteLocalPosition =
                spriteRenderer.transform.localPosition;
        }

        hoverPhase =
            UnityEngine.Random.Range(
                0f,
                Mathf.PI * 2f
            );
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

        UpdateHover();

        UpdateShadow();
    }

    // =========================================================
    // SummonData 적용
    // =========================================================

    public void ApplySummonData(
        SummonData data)
    {
        summonData =
            data;

        if (summonData == null)
        {
            movementType =
                SummonMovementType.Ground;

            return;
        }

        movementType =
            summonData.MovementType;

        SetupAerialVisual();
    }

    // =========================================================
    // 공중형 초기 설정
    // =========================================================

    private void SetupAerialVisual()
    {
        DestroyShadow();

        if (spriteRenderer == null ||
            summonData == null)
        {
            return;
        }

        if (movementType ==
            SummonMovementType.Ground)
        {
            spriteRenderer.transform.localPosition =
                originalSpriteLocalPosition;

            return;
        }

        spriteRenderer.transform.localPosition =
            originalSpriteLocalPosition +
            Vector3.up *
            summonData.HoverHeight;

        if (movementType !=
            SummonMovementType.Flying)
        {
            return;
        }

        if (summonData.ShadowSprite == null)
        {
            return;
        }

        shadowObject =
            new GameObject(
                "Runtime_Shadow"
            );

        shadowObject.transform.SetParent(
            transform,
            false
        );

        shadowObject.transform.localPosition =
            new Vector3(
                summonData.ShadowOffset.x,
                summonData.ShadowOffset.y,
                0f
            );

        shadowObject.transform.localScale =
            Vector3.one *
            summonData.ShadowBaseScale;

        shadowRenderer =
            shadowObject.AddComponent<SpriteRenderer>();

        shadowRenderer.sprite =
            summonData.ShadowSprite;

        shadowRenderer.sortingLayerName =
            spriteRenderer.sortingLayerName;

        shadowRenderer.sortingOrder =
            spriteRenderer.sortingOrder -
            10;

        Color shadowColor =
            Color.black;

        shadowColor.a =
            summonData.ShadowBaseAlpha;

        shadowRenderer.color =
            shadowColor;
    }

    private void DestroyShadow()
    {
        if (shadowObject != null)
        {
            Destroy(
                shadowObject
            );
        }

        shadowObject =
            null;

        shadowRenderer =
            null;
    }

    // =========================================================
    // 부유
    // =========================================================

    private void UpdateHover()
    {
        if (summonData == null ||
            spriteRenderer == null ||
            isDead ||
            isSummonAppearing ||
            attackBodyMotionRunning)
        {
            return;
        }

        if (movementType ==
            SummonMovementType.Ground)
        {
            return;
        }

        hoverPhase +=
            Time.deltaTime *
            summonData.HoverSpeed;

        currentHoverOffset =
            Mathf.Sin(
                hoverPhase
            ) *
            summonData.HoverAmplitude;

        Vector3 position =
            originalSpriteLocalPosition;

        position.y +=
            summonData.HoverHeight +
            currentHoverOffset;

        spriteRenderer.transform.localPosition =
            position;
    }

    // =========================================================
    // 그림자
    // =========================================================

    private void UpdateShadow()
    {
        if (shadowRenderer == null ||
            shadowObject == null ||
            summonData == null ||
            isDead)
        {
            return;
        }

        float amplitude =
            Mathf.Max(
                0.001f,
                summonData.HoverAmplitude
            );

        float normalizedHeight =
            Mathf.Clamp(
                currentHoverOffset /
                amplitude,
                -1f,
                1f
            );

        float scaleMultiplier =
            1f -
            normalizedHeight *
            summonData.ShadowScaleVariation;

        shadowObject.transform.localScale =
            Vector3.one *
            summonData.ShadowBaseScale *
            scaleMultiplier;

        float alpha =
            summonData.ShadowBaseAlpha -
            normalizedHeight *
            summonData.ShadowAlphaVariation;

        Color color =
            shadowRenderer.color;

        color.a =
            Mathf.Clamp01(
                alpha
            );

        shadowRenderer.color =
            color;
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

        Vector3 targetPosition =
            GetNormalSpritePosition();

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
            targetPosition +
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
                    targetPosition +
                    Vector3.down *
                    summonRiseDistance,
                    targetPosition,
                    eased
                );

            yield return null;
        }

        spriteRenderer.color =
            originalColor;

        spriteTransform.localScale =
            originalSpriteScale;

        spriteTransform.localPosition =
            targetPosition;

        isSummonAppearing =
            false;

        actionLocked =
            false;

        summonAppearCoroutine =
            null;

        UpdateCurrentSprite();

        onFinished?.Invoke();
    }

    private Vector3 GetNormalSpritePosition()
    {
        Vector3 position =
            originalSpriteLocalPosition;

        if (summonData != null &&
            movementType !=
            SummonMovementType.Ground)
        {
            position.y +=
                summonData.HoverHeight;
        }

        return position;
    }

    // =========================================================
    // 이동 상태
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

    public void SetCombatFacingDirection(
        Vector2 direction)
    {
        if (isDead ||
            actionLocked ||
            direction.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        direction.Normalize();

        currentFacing =
            DetermineFacingImmediate(
                direction
            );

        UpdateCurrentSprite();
    }

    // =========================================================
    // 방향 판정
    // =========================================================

    private FacingType DetermineFacing(
        Vector2 direction)
    {
        if (directionMode ==
            DirectionMode.FiveDirections)
        {
            return DetermineFiveDirection(
                direction
            );
        }

        return DetermineThreeDirection(
            direction
        );
    }

    private FacingType DetermineFacingImmediate(
        Vector2 direction)
    {
        if (directionMode ==
            DirectionMode.FiveDirections)
        {
            return DetermineFiveDirection(
                direction
            );
        }

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
            return direction.x >= 0f
                ? FacingType.Right
                : FacingType.Left;
        }

        return direction.y >= 0f
            ? FacingType.Up
            : FacingType.Down;
    }

    // =========================================================
    // 3방향
    // =========================================================

    private FacingType DetermineThreeDirection(
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

    // =========================================================
    // 5방향
    // =========================================================

    private FacingType DetermineFiveDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return currentFacing;
        }

        direction.Normalize();

        bool left =
            direction.x < 0f;

        float absX =
            Mathf.Abs(
                direction.x
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                absX
            ) *
            Mathf.Rad2Deg;

        // 위
        if (angle >=
            upDirectionAngle)
        {
            return FacingType.Up;
        }

        // 위 대각선
        if (angle >=
            diagonalDirectionAngle)
        {
            return left
                ? FacingType.UpLeft
                : FacingType.UpRight;
        }

        // 좌 / 우
        if (angle >
            -diagonalDirectionAngle)
        {
            return left
                ? FacingType.Left
                : FacingType.Right;
        }

        // 아래 대각선
        if (angle >
            -upDirectionAngle)
        {
            return left
                ? FacingType.DownLeft
                : FacingType.DownRight;
        }

        // 아래
        return FacingType.Down;
    }

    // =========================================================
    // 자동 Idle / Walk
    // =========================================================

    private void UpdateAutomaticMotion()
    {
        if (isDead ||
            actionLocked ||
            isSummonAppearing)
        {
            return;
        }

        if (movementType ==
            SummonMovementType.Ground)
        {
            ChangeMotion(
                isMoving
                    ? MotionType.Walk
                    : MotionType.Idle
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

        StartAttackBodyMotion();

        if (!HasEnoughDirectionalFrames(
                attackFrames,
                attackColumns
            ))
        {
            TriggerAttackHit();

            FinishAttackMotion();

            return;
        }

        UpdateCurrentSprite();

        if (attackHitFrame <= 0)
        {
            TriggerAttackHit();
        }
    }

    // =========================================================
    // 공격 몸동작
    // =========================================================

    private void StartAttackBodyMotion()
    {
        if (summonData == null ||
            summonData.AttackMotionType ==
            SummonAttackMotionType.None)
        {
            return;
        }

        if (attackBodyCoroutine != null)
        {
            StopCoroutine(
                attackBodyCoroutine
            );
        }

        attackBodyCoroutine =
            StartCoroutine(
                AttackBodyMotionRoutine()
            );
    }

    private IEnumerator AttackBodyMotionRoutine()
    {
        attackBodyMotionRunning =
            true;

        Vector2 direction =
            FacingToVector(
                lockedActionFacing
            );

        Vector3 basePosition =
            GetNormalSpritePosition();

        switch (summonData.AttackMotionType)
        {
            case SummonAttackMotionType.Lunge:
                {
                    Vector3 forward =
                        basePosition +
                        (Vector3)(
                            direction *
                            summonData.AttackForwardDistance
                        );

                    yield return MoveSpriteLocal(
                        basePosition,
                        forward,
                        summonData.AttackForwardTime
                    );

                    yield return MoveSpriteLocal(
                        forward,
                        basePosition,
                        summonData.AttackReturnTime
                    );

                    break;
                }

            case SummonAttackMotionType.BackstepDive:
                {
                    Vector3 back =
                        basePosition -
                        (Vector3)(
                            direction *
                            summonData.AttackBackstepDistance
                        );

                    Vector3 forward =
                        basePosition +
                        (Vector3)(
                            direction *
                            summonData.AttackForwardDistance
                        );

                    yield return MoveSpriteLocal(
                        basePosition,
                        back,
                        summonData.AttackBackstepTime
                    );

                    yield return MoveSpriteLocal(
                        back,
                        forward,
                        summonData.AttackForwardTime
                    );

                    yield return MoveSpriteLocal(
                        forward,
                        basePosition,
                        summonData.AttackReturnTime
                    );

                    break;
                }

            case SummonAttackMotionType.HeavyStep:
                {
                    Vector3 forward =
                        basePosition +
                        (Vector3)(
                            direction *
                            summonData.AttackForwardDistance
                        );

                    yield return MoveSpriteLocal(
                        basePosition,
                        forward,
                        summonData.AttackForwardTime
                    );

                    yield return MoveSpriteLocal(
                        forward,
                        basePosition,
                        summonData.AttackReturnTime
                    );

                    break;
                }

            case SummonAttackMotionType.Recoil:
                {
                    Vector3 back =
                        basePosition -
                        (Vector3)(
                            direction *
                            summonData.AttackBackstepDistance
                        );

                    yield return MoveSpriteLocal(
                        basePosition,
                        back,
                        summonData.AttackBackstepTime
                    );

                    yield return MoveSpriteLocal(
                        back,
                        basePosition,
                        summonData.AttackReturnTime
                    );

                    break;
                }
        }

        if (!isDead &&
            spriteRenderer != null)
        {
            spriteRenderer.transform.localPosition =
                basePosition;
        }

        attackBodyMotionRunning =
            false;

        attackBodyCoroutine =
            null;
    }

    private IEnumerator MoveSpriteLocal(
        Vector3 from,
        Vector3 to,
        float duration)
    {
        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        float timer =
            0f;

        while (timer <
               duration)
        {
            if (isDead)
            {
                yield break;
            }

            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                t *
                t *
                (3f - 2f * t);

            spriteRenderer.transform.localPosition =
                Vector3.Lerp(
                    from,
                    to,
                    eased
                );

            yield return null;
        }

        spriteRenderer.transform.localPosition =
            to;
    }

    private Vector2 FacingToVector(
        FacingType facing)
    {
        switch (facing)
        {
            case FacingType.Up:
                return Vector2.up;

            case FacingType.UpRight:
                return new Vector2(
                    1f,
                    1f
                ).normalized;

            case FacingType.Right:
                return Vector2.right;

            case FacingType.DownRight:
                return new Vector2(
                    1f,
                    -1f
                ).normalized;

            case FacingType.Down:
                return Vector2.down;

            case FacingType.DownLeft:
                return new Vector2(
                    -1f,
                    -1f
                ).normalized;

            case FacingType.Left:
                return Vector2.left;

            case FacingType.UpLeft:
                return new Vector2(
                    -1f,
                    1f
                ).normalized;
        }

        return Vector2.down;
    }

    // =========================================================
    // 능력치 상승 피드백
    // =========================================================

    public void PlayStatUpEffect()
    {
        if (!useStatUpEffect ||
            isDead ||
            isSummonAppearing ||
            spriteRenderer == null)
        {
            return;
        }

        if (statUpEffectCoroutine != null)
        {
            StopCoroutine(
                statUpEffectCoroutine
            );

            statUpEffectCoroutine =
                null;
        }

        statUpEffectCoroutine =
            StartCoroutine(
                StatUpEffectRoutine()
            );
    }

    private IEnumerator StatUpEffectRoutine()
    {
        GameObject glowObject =
            CreateStatUpGlow();

        GameObject[] arrows =
            CreateStatUpArrows();

        float duration =
            Mathf.Max(
                0.05f,
                statUpDuration
            );

        float timer =
            0f;

        Color startColor =
            spriteRenderer.color;

        Vector3 glowBaseScale =
            glowObject != null
                ? glowObject.transform.localScale
                : Vector3.one;

        Vector3[] arrowStartPositions =
            new Vector3[
                arrows.Length
            ];

        SpriteRenderer[] arrowRenderers =
            new SpriteRenderer[
                arrows.Length
            ];

        for (int i = 0;
             i < arrows.Length;
             i++)
        {
            if (arrows[i] == null)
            {
                continue;
            }

            arrowStartPositions[i] =
                arrows[i].transform.position;

            arrowRenderers[i] =
                arrows[i]
                    .GetComponent<SpriteRenderer>();
        }

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

            float pulse =
                Mathf.Sin(
                    t *
                    Mathf.PI
                );

            // 몸이 짧게 밝아졌다가 원래 색으로 돌아옵니다.
            Color bodyColor =
                Color.Lerp(
                    startColor,
                    statUpFlashColor,
                    pulse * 0.75f
                );

            bodyColor.a =
                startColor.a;

            spriteRenderer.color =
                bodyColor;

            if (glowObject != null)
            {
                float glowPulse =
                    0.9f +
                    pulse * 0.25f;

                glowObject.transform.localScale =
                    glowBaseScale *
                    glowPulse;

                SpriteRenderer glowRenderer =
                    glowObject
                        .GetComponent<SpriteRenderer>();

                if (glowRenderer != null)
                {
                    Color glowColor =
                        statUpGlowColor;

                    glowColor.a *=
                        Mathf.Sin(
                            Mathf.PI *
                            Mathf.Clamp01(t)
                        );

                    glowRenderer.color =
                        glowColor;
                }
            }

            for (int i = 0;
                 i < arrows.Length;
                 i++)
            {
                if (arrows[i] == null)
                {
                    continue;
                }

                float delay =
                    i * 0.12f;

                float arrowT =
                    Mathf.Clamp01(
                        (t - delay) /
                        Mathf.Max(
                            0.01f,
                            1f - delay
                        )
                    );

                Vector3 position =
                    arrowStartPositions[i];

                position.y +=
                    statUpArrowRiseDistance *
                    arrowT;

                arrows[i].transform.position =
                    position;

                if (arrowRenderers[i] != null)
                {
                    Color arrowColor =
                        statUpArrowColor;

                    arrowColor.a *=
                        1f - arrowT;

                    arrowRenderers[i].color =
                        arrowColor;
                }
            }

            yield return null;
        }

        if (!isDead &&
            spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
        }

        if (glowObject != null)
        {
            Destroy(
                glowObject
            );
        }

        for (int i = 0;
             i < arrows.Length;
             i++)
        {
            if (arrows[i] != null)
            {
                Destroy(
                    arrows[i]
                );
            }
        }

        statUpEffectCoroutine =
            null;
    }

    private GameObject CreateStatUpGlow()
    {
        if (spriteRenderer == null)
        {
            return null;
        }

        EnsureStatUpSprites();

        GameObject glowObject =
            new GameObject(
                "StatUpGlow"
            );

        glowObject.transform.SetParent(
            spriteRenderer.transform,
            false
        );

        glowObject.transform.localPosition =
            Vector3.zero;

        glowObject.transform.localScale =
            Vector3.one *
            statUpGlowScale;

        SpriteRenderer glowRenderer =
            glowObject.AddComponent<SpriteRenderer>();

        glowRenderer.sprite =
            cachedStatUpGlowSprite;

        glowRenderer.color =
            statUpGlowColor;

        glowRenderer.sortingLayerID =
            spriteRenderer.sortingLayerID;

        glowRenderer.sortingOrder =
            spriteRenderer.sortingOrder -
            1;

        return glowObject;
    }

    private GameObject[] CreateStatUpArrows()
    {
        EnsureStatUpSprites();

        GameObject[] arrows =
            new GameObject[3];

        float[] xOffsets =
        {
            -0.16f,
            0f,
            0.16f
        };

        for (int i = 0;
             i < arrows.Length;
             i++)
        {
            GameObject arrow =
                new GameObject(
                    "StatUpArrow"
                );

            arrow.transform.position =
                transform.position +
                (Vector3)statUpArrowOffset +
                Vector3.right *
                xOffsets[i];

            arrow.transform.localScale =
                Vector3.one *
                statUpArrowScale;

            SpriteRenderer arrowRenderer =
                arrow.AddComponent<SpriteRenderer>();

            arrowRenderer.sprite =
                cachedStatUpArrowSprite;

            arrowRenderer.color =
                statUpArrowColor;

            arrowRenderer.sortingLayerID =
                spriteRenderer.sortingLayerID;

            arrowRenderer.sortingOrder =
                spriteRenderer.sortingOrder +
                2;

            arrows[i] =
                arrow;

            Destroy(
                arrow,
                Mathf.Max(
                    0.1f,
                    statUpDuration
                ) +
                0.25f
            );
        }

        return arrows;
    }

    private static void EnsureStatUpSprites()
    {
        if (cachedStatUpArrowSprite == null)
        {
            cachedStatUpArrowSprite =
                CreatePixelArrowSprite();
        }

        if (cachedStatUpGlowSprite == null)
        {
            cachedStatUpGlowSprite =
                CreatePixelGlowSprite();
        }
    }

    private static Sprite CreatePixelArrowSprite()
    {
        const int width = 7;
        const int height = 9;

        Texture2D texture =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );

        texture.filterMode =
            FilterMode.Point;

        texture.wrapMode =
            TextureWrapMode.Clamp;

        Color clear =
            new Color(
                1f,
                1f,
                1f,
                0f
            );

        Color solid =
            Color.white;

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                texture.SetPixel(
                    x,
                    y,
                    clear
                );
            }
        }

        // 7x9 픽셀 화살표
        int[,] pixels =
        {
            { 3, 8 },
            { 2, 7 }, { 3, 7 }, { 4, 7 },
            { 1, 6 }, { 2, 6 }, { 3, 6 }, { 4, 6 }, { 5, 6 },
            { 0, 5 }, { 1, 5 }, { 2, 5 }, { 3, 5 }, { 4, 5 }, { 5, 5 }, { 6, 5 },
            { 2, 4 }, { 3, 4 }, { 4, 4 },
            { 2, 3 }, { 3, 3 }, { 4, 3 },
            { 2, 2 }, { 3, 2 }, { 4, 2 },
            { 2, 1 }, { 3, 1 }, { 4, 1 },
            { 2, 0 }, { 3, 0 }, { 4, 0 }
        };

        for (int i = 0;
             i < pixels.GetLength(0);
             i++)
        {
            texture.SetPixel(
                pixels[i, 0],
                pixels[i, 1],
                solid
            );
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(
                0f,
                0f,
                width,
                height
            ),
            new Vector2(
                0.5f,
                0.5f
            ),
            16f
        );
    }

    private static Sprite CreatePixelGlowSprite()
    {
        const int size = 16;

        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        texture.filterMode =
            FilterMode.Point;

        texture.wrapMode =
            TextureWrapMode.Clamp;

        Vector2 center =
            new Vector2(
                (size - 1) * 0.5f,
                (size - 1) * 0.5f
            );

        for (int y = 0;
             y < size;
             y++)
        {
            for (int x = 0;
                 x < size;
                 x++)
            {
                float distance =
                    Vector2.Distance(
                        new Vector2(
                            x,
                            y
                        ),
                        center
                    );

                float alpha;

                if (distance <= 4f)
                {
                    alpha = 0.65f;
                }
                else if (distance <= 5.5f)
                {
                    alpha = 0.38f;
                }
                else if (distance <= 7f)
                {
                    alpha = 0.16f;
                }
                else
                {
                    alpha = 0f;
                }

                texture.SetPixel(
                    x,
                    y,
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha
                    )
                );
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(
                0f,
                0f,
                size,
                size
            ),
            new Vector2(
                0.5f,
                0.5f
            ),
            16f
        );
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
        PlayHitDust();

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

        lockedActionFacing =
            currentFacing;

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

    private void PlayHitDust()
    {
        if (hitDustFrames == null ||
            hitDustFrames.Length == 0 ||
            spriteRenderer == null)
        {
            return;
        }

        StartCoroutine(
            HitDustRoutine()
        );
    }

    private IEnumerator HitDustRoutine()
    {
        GameObject dustObject =
            new GameObject(
                "HitDust"
            );

        dustObject.transform.position =
            transform.position +
            (Vector3)hitDustOffset;

        dustObject.transform.localScale =
            Vector3.one *
            hitDustScale;

        SpriteRenderer dustRenderer =
            dustObject.AddComponent<SpriteRenderer>();

        dustRenderer.sortingLayerID =
            spriteRenderer.sortingLayerID;

        dustRenderer.sortingOrder =
            spriteRenderer.sortingOrder +
            hitDustSortingOffset;

        dustRenderer.flipX =
            false;

        float frameDuration =
            1f /
            Mathf.Max(
                0.1f,
                hitDustFrameRate
            );

        // 이 컨트롤러가 도중에 파괴돼 코루틴이 멈춰도
        // 먼지 오브젝트가 씬에 남지 않도록 안전 삭제를 예약합니다.
        Destroy(
            dustObject,
            frameDuration *
            hitDustFrames.Length +
            0.25f
        );

        for (int i = 0;
             i < hitDustFrames.Length;
             i++)
        {
            if (dustRenderer == null)
            {
                yield break;
            }

            Sprite frame =
                hitDustFrames[i];

            if (frame != null)
            {
                dustRenderer.sprite =
                    frame;
            }

            yield return
                new WaitForSeconds(
                    frameDuration
                );
        }

        if (dustObject != null)
        {
            Destroy(
                dustObject
            );
        }
    }

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

        lockedActionFacing =
            currentFacing;

        StopActiveVisualCoroutines();

        attackHitCallback =
            null;

        actionFinishedCallback =
            onFinished;

        if (summonData != null)
        {
            switch (summonData.DeathMotionType)
            {
                case SummonDeathMotionType.Fall:
                    {
                        deathSequenceCoroutine =
                            StartCoroutine(
                                FlyingFallDeathRoutine()
                            );

                        return;
                    }

                case SummonDeathMotionType.Fade:
                    {
                        deathSequenceCoroutine =
                            StartCoroutine(
                                FloatingFadeDeathRoutine()
                            );

                        return;
                    }
            }
        }

        StartNormalDeathAnimation();
    }

    private void StopActiveVisualCoroutines()
    {
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

        if (attackBodyCoroutine != null)
        {
            StopCoroutine(
                attackBodyCoroutine
            );

            attackBodyCoroutine =
                null;

            attackBodyMotionRunning =
                false;
        }

        if (statUpEffectCoroutine != null)
        {
            StopCoroutine(
                statUpEffectCoroutine
            );

            statUpEffectCoroutine =
                null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;

            spriteRenderer.transform.localScale =
                originalSpriteScale;
        }
    }

    // =========================================================
    // Flying 사망
    // =========================================================

    private IEnumerator FlyingFallDeathRoutine()
    {
        isDeathSequenceRunning =
            true;

        Vector3 start =
            spriteRenderer.transform.localPosition;

        Vector3 ground =
            originalSpriteLocalPosition;

        float duration =
            Mathf.Max(
                0.01f,
                summonData.FallDeathTime
            );

        float timer =
            0f;

        Vector3 shadowStartScale =
            shadowObject != null
                ? shadowObject.transform.localScale
                : Vector3.one;

        float shadowStartAlpha =
            shadowRenderer != null
                ? shadowRenderer.color.a
                : 0f;

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

            float fallT =
                t * t;

            spriteRenderer.transform.localPosition =
                Vector3.Lerp(
                    start,
                    ground,
                    fallT
                );

            if (shadowRenderer != null &&
                shadowObject != null)
            {
                shadowObject.transform.localScale =
                    Vector3.Lerp(
                        shadowStartScale,
                        Vector3.one *
                        summonData.ShadowBaseScale *
                        1.12f,
                        t
                    );

                Color shadowColor =
                    shadowRenderer.color;

                shadowColor.a =
                    Mathf.Lerp(
                        shadowStartAlpha,
                        Mathf.Clamp01(
                            summonData.ShadowBaseAlpha +
                            0.2f
                        ),
                        t
                    );

                shadowRenderer.color =
                    shadowColor;
            }

            yield return null;
        }

        spriteRenderer.transform.localPosition =
            ground;

        isDeathSequenceRunning =
            false;

        deathSequenceCoroutine =
            null;

        StartNormalDeathAnimation();
    }

    // =========================================================
    // Floating 사망
    // =========================================================

    private IEnumerator FloatingFadeDeathRoutine()
    {
        isDeathSequenceRunning =
            true;

        Vector3 startPosition =
            spriteRenderer.transform.localPosition;

        Color startColor =
            originalColor;

        float duration =
            Mathf.Max(
                0.1f,
                deathFadeTime
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

            float shake =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    8f
                ) *
                summonData.FloatingDeathShake *
                (1f - t);

            spriteRenderer.transform.localPosition =
                startPosition +
                Vector3.right *
                shake +
                Vector3.up *
                (t * 0.12f);

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

        FinishDeath();
    }

    private void StartNormalDeathAnimation()
    {
        currentMotion =
            MotionType.Death;

        currentFrame =
            0;

        animationTimer =
            0f;

        isDeathSequenceRunning =
            false;

        if (!HasEnoughDirectionalFrames(
                deathFrames,
                deathColumns
            ))
        {
            StartDeathFadeSequence();

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
                    {
                        if (currentFrame >=
                            idleColumns)
                        {
                            currentFrame =
                                0;
                        }

                        break;
                    }

                case MotionType.Walk:
                    {
                        if (currentFrame >=
                            walkColumns)
                        {
                            currentFrame =
                                0;
                        }

                        break;
                    }

                case MotionType.Attack:
                    {
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
                    }

                case MotionType.Hit:
                    {
                        if (currentFrame >=
                            hitColumns)
                        {
                            FinishHitMotion();

                            return;
                        }

                        break;
                    }

                case MotionType.Death:
                    {
                        if (currentFrame >=
                            deathColumns)
                        {
                            currentFrame =
                                deathColumns - 1;

                            UpdateCurrentSprite();

                            StartDeathFadeSequence();

                            return;
                        }

                        break;
                    }
            }

            UpdateCurrentSprite();
        }
    }

    // =========================================================
    // 공격 처리
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

    private void FinishAttackMotion()
    {
        if (!attackDamageTriggered)
        {
            TriggerAttackHit();
        }

        actionLocked =
            false;

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

    private void FinishHitMotion()
    {
        actionLocked =
            false;

        if (movementType ==
            SummonMovementType.Ground &&
            isMoving)
        {
            currentMotion =
                MotionType.Walk;
        }
        else
        {
            currentMotion =
                MotionType.Idle;
        }

        currentFrame =
            0;

        animationTimer =
            0f;

        UpdateCurrentSprite();
    }

    // =========================================================
    // Death Fade
    // =========================================================

    private void StartDeathFadeSequence()
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
                DeathFadeRoutine()
            );
    }

    private IEnumerator DeathFadeRoutine()
    {
        if (deathHoldTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    deathHoldTime
                );
        }

        Color start =
            spriteRenderer.color;

        Color shadowStart =
            shadowRenderer != null
                ? shadowRenderer.color
                : Color.clear;

        float timer =
            0f;

        if (deathFadeTime > 0f)
        {
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
                    start;

                color.a =
                    Mathf.Lerp(
                        start.a,
                        0f,
                        t
                    );

                spriteRenderer.color =
                    color;

                if (shadowRenderer != null)
                {
                    Color shadowColor =
                        shadowStart;

                    shadowColor.a =
                        Mathf.Lerp(
                            shadowStart.a,
                            0f,
                            t
                        );

                    shadowRenderer.color =
                        shadowColor;
                }

                yield return null;
            }
        }

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

        Action callback =
            actionFinishedCallback;

        actionFinishedCallback =
            null;

        callback?.Invoke();
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

    private int GetCurrentColumns()
    {
        switch (currentMotion)
        {
            case MotionType.Walk:
                return walkColumns;

            case MotionType.Attack:
                return attackColumns;

            case MotionType.Death:
                return deathColumns;

            case MotionType.Hit:
                return hitColumns;

            default:
                return idleColumns;
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

        Sprite[] frames =
            GetCurrentFrames();

        int columns =
            GetCurrentColumns();

        FacingType facing =
            currentMotion ==
                MotionType.Attack ||
            currentMotion ==
                MotionType.Death ||
            currentMotion ==
                MotionType.Hit
                ? lockedActionFacing
                : currentFacing;

        ApplyDirectionalAnimation(
            frames,
            columns,
            facing
        );
    }

    private void ApplyDirectionalAnimation(
        Sprite[] frames,
        int columns,
        FacingType facing)
    {
        if (frames == null ||
            frames.Length == 0)
        {
            return;
        }

        if (columns <= 0)
        {
            return;
        }

        int row =
            GetDirectionRow(
                facing
            );

        bool flipX =
            ShouldFlipX(
                facing
            );

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

        if (index < 0 ||
            index >= frames.Length)
        {
            return;
        }

        spriteRenderer.flipX =
            flipX;

        spriteRenderer.sprite =
            frames[index];
    }

    // =========================================================
    // 방향 → 배열 줄
    // =========================================================

    private int GetDirectionRow(
        FacingType facing)
    {
        if (directionMode ==
            DirectionMode.ThreeDirections)
        {
            switch (facing)
            {
                case FacingType.Up:
                case FacingType.UpLeft:
                case FacingType.UpRight:
                    return 2;

                case FacingType.Right:
                case FacingType.Left:
                case FacingType.DownRight:
                case FacingType.DownLeft:
                    return 1;

                default:
                    return 0;
            }
        }

        switch (facing)
        {
            case FacingType.Down:
                return 0;

            case FacingType.DownRight:
            case FacingType.DownLeft:
                return 1;

            case FacingType.Right:
            case FacingType.Left:
                return 2;

            case FacingType.UpRight:
            case FacingType.UpLeft:
                return 3;

            case FacingType.Up:
                return 4;
        }

        return 0;
    }

    private bool ShouldFlipX(
        FacingType facing)
    {
        switch (facing)
        {
            case FacingType.Left:
            case FacingType.UpLeft:
            case FacingType.DownLeft:
                return true;

            default:
                return false;
        }
    }

    // =========================================================
    // 배열 검사
    // =========================================================

    private bool HasEnoughDirectionalFrames(
        Sprite[] frames,
        int columns)
    {
        if (frames == null ||
            columns <= 0)
        {
            return false;
        }

        int directionCount =
            directionMode ==
            DirectionMode.FiveDirections
                ? 5
                : 3;

        int required =
            columns *
            directionCount;

        return frames.Length >=
               required;
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

        diagonalDirectionAngle =
            Mathf.Clamp(
                diagonalDirectionAngle,
                0f,
                45f
            );

        upDirectionAngle =
            Mathf.Clamp(
                upDirectionAngle,
                diagonalDirectionAngle + 1f,
                89f
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

        hitColumns =
            Mathf.Max(
                1,
                hitColumns
            );

        hitDustFrameRate =
            Mathf.Max(
                0.1f,
                hitDustFrameRate
            );

        hitDustScale =
            Mathf.Max(
                0.01f,
                hitDustScale
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
    }

#endif
}