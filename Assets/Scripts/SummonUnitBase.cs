using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SummonUnitBase : MonoBehaviour
{
    [Header("리벨")]
    [SerializeField] protected Transform ribel;

    [Header("기본 능력치")]
    [SerializeField] protected float maxHealth = 60f;
    [SerializeField] protected float defense = 0f;

    [Header("이동")]
    [SerializeField] protected float moveSpeed = 3.8f;
    [SerializeField] protected float stopDistance = 0.5f;
    [SerializeField] protected float followStartDistance = 0.9f;

    [Header("리벨 주변 분산")]
    [SerializeField] protected float followRadius = 1.6f;
    [SerializeField] protected float followRadiusRandom = 0.35f;

    [Header("적 탐색")]
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float searchInterval = 0.2f;

    [Header("공격 거리")]
    [Tooltip("이 거리 안에 들어오면 공격 상태로 진입")]
    [SerializeField] protected float attackRange = 0.9f;

    [Tooltip("공격 상태 진입 후 이 거리까지는 계속 공격")]
    [SerializeField] protected float attackKeepRange = 1.2f;

    [Header("공격")]
    [SerializeField] protected float attackDamage = 20f;
    [SerializeField] protected float attackInterval = 1f;

    [Tooltip("공격 애니메이션 종료 뒤 짧은 정지 시간")]
    [SerializeField] protected float postAttackHoldTime = 0.18f;

    [Tooltip("공격 도중 적이 조금 움직여도 타격을 허용하는 배율")]
    [SerializeField] protected float attackHitRangeMultiplier = 1.4f;

    [Header("공격 안전장치")]
    [Tooltip("공격 애니메이션 종료 콜백이 오지 않아도 이 시간이 지나면 공격 잠금을 강제로 해제")]
    [SerializeField] protected float maxAttackLockTime = 1.5f;

    [Header("소환 보호")]
    [Tooltip("완전히 등장한 뒤 추가 무적 시간")]
    [SerializeField] protected float postSummonInvincibleTime = 0.5f;

    [Header("비주얼")]
    [SerializeField] protected SummonVisualController visualController;

    protected Rigidbody2D rb;
    protected Transform currentTarget;

    protected bool isFollowingRibel;
    protected Vector2 followOffset;

    protected bool isRallyingToRibel;
    protected bool isRallyingToPoint;
    protected Vector2 rallyPoint;

    private float currentHealth;

    private float searchTimer;
    private float attackTimer;
    private float postAttackHoldTimer;

    // 공격 잠금이 걸린 시간
    private float attackLockTimer;

    private bool isDead;
    private bool isAttacking;
    private bool isInAttackMode;

    private bool isSummoning;
    private float summonInvincibleTimer;

    private Collider2D[] cachedColliders;
    private bool[] colliderOriginalStates;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float Defense => defense;
    public float DetectionRange => detectionRange;

    public bool IsRallyingToRibel => isRallyingToRibel;
    public bool IsRallyingToPoint => isRallyingToPoint;

    public bool IsDead => isDead;
    public bool IsSummoning => isSummoning;

    public bool IsSummonInvincible =>
        isSummoning ||
        summonInvincibleTimer > 0f;

    // =========================================================
    // 초기화
    // =========================================================

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (visualController == null)
        {
            visualController =
                GetComponentInChildren<SummonVisualController>();
        }

        currentHealth = maxHealth;

        CacheColliders();
        CreateFollowOffset();
    }

    protected virtual void Start()
    {
        FindRibel();
    }

    protected virtual void Update()
    {
        if (isDead)
        {
            return;
        }

        UpdateSummonProtection();

        if (isSummoning)
        {
            return;
        }

        SearchTarget();
        UpdateTimers();
        UpdateAttackSafety();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead ||
            isSummoning)
        {
            StopMovement();
            return;
        }

        UpdateMovement();
    }

    // =========================================================
    // 소환 등장
    // =========================================================

    public virtual void BeginSummonAppearance()
    {
        if (isDead)
        {
            return;
        }

        isSummoning = true;

        summonInvincibleTimer = 0f;

        currentTarget = null;

        ResetAttackState();

        isRallyingToRibel = false;
        isRallyingToPoint = false;

        StopMovement();

        SetCollidersEnabled(false);

        if (visualController != null)
        {
            visualController.PlaySummonAppearance(
                FinishSummonAppearance
            );
        }
        else
        {
            FinishSummonAppearance();
        }
    }

    protected virtual void FinishSummonAppearance()
    {
        if (isDead)
        {
            return;
        }

        isSummoning = false;

        summonInvincibleTimer =
            postSummonInvincibleTime;
    }

    private void UpdateSummonProtection()
    {
        if (isSummoning)
        {
            return;
        }

        if (summonInvincibleTimer <= 0f)
        {
            return;
        }

        summonInvincibleTimer -=
            Time.deltaTime;

        if (summonInvincibleTimer <= 0f)
        {
            summonInvincibleTimer = 0f;

            RestoreColliders();
        }
    }

    // =========================================================
    // Collider
    // =========================================================

    private void CacheColliders()
    {
        cachedColliders =
            GetComponentsInChildren<Collider2D>(
                true
            );

        colliderOriginalStates =
            new bool[
                cachedColliders.Length
            ];

        for (int i = 0;
             i < cachedColliders.Length;
             i++)
        {
            colliderOriginalStates[i] =
                cachedColliders[i] != null &&
                cachedColliders[i].enabled;
        }
    }

    private void SetCollidersEnabled(
        bool enabled)
    {
        if (cachedColliders == null)
        {
            return;
        }

        for (int i = 0;
             i < cachedColliders.Length;
             i++)
        {
            if (cachedColliders[i] != null)
            {
                cachedColliders[i].enabled =
                    enabled;
            }
        }
    }

    private void RestoreColliders()
    {
        if (cachedColliders == null ||
            colliderOriginalStates == null)
        {
            return;
        }

        for (int i = 0;
             i < cachedColliders.Length;
             i++)
        {
            if (cachedColliders[i] != null)
            {
                cachedColliders[i].enabled =
                    colliderOriginalStates[i];
            }
        }
    }

    // =========================================================
    // 피해
    // =========================================================

    public virtual void TakeDamage(
        float damage)
    {
        if (isDead ||
            isSummoning ||
            summonInvincibleTimer > 0f ||
            damage <= 0f)
        {
            return;
        }

        float finalDamage =
            Mathf.Max(
                1f,
                damage - defense
            );

        currentHealth -=
            finalDamage;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;

            Die();
            return;
        }

        if (visualController != null)
        {
            visualController.PlayHit();
        }
    }

    // =========================================================
    // 사망
    // =========================================================

    protected virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        currentHealth = 0f;
        currentTarget = null;

        ResetAttackState();

        isSummoning = false;
        summonInvincibleTimer = 0f;

        StopMovement();
        SetCollidersEnabled(false);

        if (SummonManager.Instance != null)
        {
            SummonManager.Instance.ReleaseCapacity(
                1
            );
        }

        if (visualController != null)
        {
            visualController.PlayDeath(
                DestroyAfterDeath
            );
        }
        else
        {
            DestroyAfterDeath();
        }
    }

    private void DestroyAfterDeath()
    {
        Destroy(gameObject);
    }

    // =========================================================
    // 리벨
    // =========================================================

    protected virtual void FindRibel()
    {
        if (ribel != null)
        {
            return;
        }

        GameObject ribelObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (ribelObject != null)
        {
            ribel =
                ribelObject.transform;
        }
    }

    protected virtual void CreateFollowOffset()
    {
        float angle =
            Random.Range(
                0f,
                360f
            ) *
            Mathf.Deg2Rad;

        float radius =
            followRadius +
            Random.Range(
                -followRadiusRandom,
                followRadiusRandom
            );

        radius =
            Mathf.Max(
                0.1f,
                radius
            );

        followOffset =
            new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) *
            radius;
    }

    // =========================================================
    // 집결
    // =========================================================

    public virtual void RallyToRibel()
    {
        if (isDead ||
            isSummoning)
        {
            return;
        }

        FindRibel();

        if (ribel == null)
        {
            return;
        }

        CancelCombatState();

        isFollowingRibel = false;

        isRallyingToPoint = false;
        isRallyingToRibel = true;
    }

    public virtual void RallyToPoint(
        Vector2 point)
    {
        if (isDead ||
            isSummoning)
        {
            return;
        }

        CancelCombatState();

        isFollowingRibel = false;

        isRallyingToRibel = false;
        isRallyingToPoint = true;

        rallyPoint =
            point +
            followOffset * 0.45f;
    }

    private void CancelCombatState()
    {
        currentTarget = null;

        ResetAttackState();
    }

    // =========================================================
    // 공격 상태 초기화
    // =========================================================

    private void ResetAttackState()
    {
        isAttacking = false;
        isInAttackMode = false;

        attackTimer = 0f;
        postAttackHoldTimer = 0f;
        attackLockTimer = 0f;
    }

    // =========================================================
    // ★ 공격 잠김 안전장치
    // =========================================================

    private void UpdateAttackSafety()
    {
        if (!isAttacking)
        {
            attackLockTimer = 0f;
            return;
        }

        // 타깃 자체가 사라졌다면 바로 공격 잠금 해제
        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            ForceReleaseAttack();
            return;
        }

        attackLockTimer +=
            Time.deltaTime;

        // 애니메이션 콜백이 오지 않아도
        // 일정 시간이 지나면 무조건 복구
        if (attackLockTimer >=
            maxAttackLockTime)
        {
            ForceReleaseAttack();
        }
    }

    private void ForceReleaseAttack()
    {
        isAttacking = false;

        attackLockTimer = 0f;

        // 강제로 풀린 뒤 바로 연속 난타하지 않도록
        // 기존 공격 쿨타임 적용
        attackTimer =
            Mathf.Max(
                attackTimer,
                attackInterval
            );

        postAttackHoldTimer = 0f;

        // 타깃이 없으면 공격 모드도 종료
        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            isInAttackMode = false;
            return;
        }

        float distance =
            Vector2.Distance(
                rb.position,
                currentTarget.position
            );

        // 적이 이미 멀어졌다면 바로 추적할 수 있도록
        // 공격 모드 해제
        if (distance >
            attackKeepRange)
        {
            isInAttackMode = false;
        }
    }

    // =========================================================
    // 적 탐색
    // =========================================================

    protected virtual void SearchTarget()
    {
        if (isRallyingToRibel ||
            isRallyingToPoint)
        {
            currentTarget = null;
            isInAttackMode = false;

            return;
        }

        searchTimer -=
            Time.deltaTime;

        if (searchTimer > 0f)
        {
            return;
        }

        searchTimer =
            searchInterval;

        // 이미 잡은 적 유지
        if (currentTarget != null)
        {
            float distance =
                Vector2.Distance(
                    rb.position,
                    currentTarget.position
                );

            if (!currentTarget.gameObject.activeInHierarchy ||
                distance > detectionRange)
            {
                currentTarget = null;

                isInAttackMode = false;

                // 적이 사라질 때 공격 잠금도 같이 해제
                isAttacking = false;
                attackLockTimer = 0f;
            }
        }

        if (currentTarget != null)
        {
            return;
        }

        Collider2D[] enemies =
            Physics2D.OverlapCircleAll(
                rb.position,
                detectionRange,
                enemyLayer
            );

        Transform nearestEnemy =
            null;

        float nearestDistance =
            Mathf.Infinity;

        for (int i = 0;
             i < enemies.Length;
             i++)
        {
            EnemyUnitBase enemy =
                enemies[i]
                    .GetComponentInParent<EnemyUnitBase>();

            if (enemy == null)
            {
                continue;
            }

            float distance =
                Vector2.SqrMagnitude(
                    (Vector2)enemy.transform.position -
                    rb.position
                );

            if (distance <
                nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestEnemy =
                    enemy.transform;
            }
        }

        currentTarget =
            nearestEnemy;

        isInAttackMode =
            false;
    }

    // =========================================================
    // 이동 / 행동
    // =========================================================

    protected virtual void UpdateMovement()
    {
        if (isAttacking)
        {
            StopMovement();
            return;
        }

        if (postAttackHoldTimer > 0f)
        {
            StopMovement();
            return;
        }

        if (isRallyingToRibel)
        {
            UpdateRallyToRibel();
            return;
        }

        if (isRallyingToPoint)
        {
            UpdateRallyToPoint();
            return;
        }

        if (currentTarget != null)
        {
            FollowOrAttackTarget();
            return;
        }

        FollowRibel();
    }

    // =========================================================
    // 리벨 집결
    // =========================================================

    protected virtual void UpdateRallyToRibel()
    {
        if (ribel == null)
        {
            FindRibel();

            if (ribel == null)
            {
                StopMovement();
                return;
            }
        }

        Vector2 destination =
            (Vector2)ribel.position +
            followOffset;

        float distance =
            Vector2.Distance(
                rb.position,
                destination
            );

        if (distance <=
            stopDistance)
        {
            isRallyingToRibel = false;
            isFollowingRibel = false;

            StopMovement();
            return;
        }

        MoveToward(destination);
    }

    // =========================================================
    // 지정 위치 집결
    // =========================================================

    protected virtual void UpdateRallyToPoint()
    {
        float distance =
            Vector2.Distance(
                rb.position,
                rallyPoint
            );

        if (distance <=
            stopDistance)
        {
            isRallyingToPoint = false;

            StopMovement();
            return;
        }

        MoveToward(rallyPoint);
    }

    // =========================================================
    // 공통 공격 AI
    // =========================================================

    protected virtual void FollowOrAttackTarget()
    {
        if (currentTarget == null)
        {
            isInAttackMode = false;
            return;
        }

        Vector2 toTarget =
            (Vector2)currentTarget.position -
            rb.position;

        float distance =
            toTarget.magnitude;

        // =====================================================
        // 이미 공격 모드
        // =====================================================

        if (isInAttackMode)
        {
            // 적이 확실히 사거리 밖으로 빠졌을 때만
            // 공격 모드 해제
            if (distance >
                attackKeepRange)
            {
                isInAttackMode =
                    false;

                MoveToward(
                    currentTarget.position
                );

                return;
            }

            StopMovement();

            if (visualController != null &&
                toTarget.sqrMagnitude >
                0.0001f)
            {
                visualController.SetFacingDirection(
                    toTarget
                );
            }

            TryAttack();

            return;
        }

        // =====================================================
        // 공격 모드 진입
        // =====================================================

        if (distance <=
            attackRange)
        {
            isInAttackMode =
                true;

            StopMovement();

            if (visualController != null &&
                toTarget.sqrMagnitude >
                0.0001f)
            {
                visualController.SetFacingDirection(
                    toTarget
                );
            }

            TryAttack();

            return;
        }

        // =====================================================
        // 사거리 밖
        // =====================================================

        MoveToward(
            currentTarget.position
        );
    }

    // =========================================================
    // 공격
    // =========================================================

    protected virtual void TryAttack()
    {
        if (isDead ||
            isSummoning ||
            summonInvincibleTimer > 0f ||
            isAttacking ||
            attackTimer > 0f ||
            currentTarget == null)
        {
            return;
        }

        EnemyUnitBase enemy =
            currentTarget
                .GetComponent<EnemyUnitBase>();

        if (enemy == null)
        {
            enemy =
                currentTarget
                    .GetComponentInChildren<EnemyUnitBase>();
        }

        if (enemy == null)
        {
            currentTarget = null;

            isInAttackMode =
                false;

            isAttacking =
                false;

            attackLockTimer =
                0f;

            return;
        }

        Vector2 toTarget =
            (Vector2)currentTarget.position -
            rb.position;

        if (visualController != null &&
            toTarget.sqrMagnitude >
            0.0001f)
        {
            visualController.SetFacingDirection(
                toTarget
            );
        }

        isAttacking =
            true;

        // ★ 공격 잠금 시간 측정 시작
        attackLockTimer =
            0f;

        StopMovement();

        if (visualController != null)
        {
            visualController.PlayAttack(
                ApplyAttackDamage,
                FinishAttack
            );
        }
        else
        {
            ApplyAttackDamage();
            FinishAttack();
        }
    }

    // =========================================================
    // 실제 타격
    // Attack Hit Frame에서 호출
    // =========================================================

    protected virtual void ApplyAttackDamage()
    {
        if (isDead ||
            isSummoning ||
            currentTarget == null)
        {
            return;
        }

        EnemyUnitBase enemy =
            currentTarget
                .GetComponent<EnemyUnitBase>();

        if (enemy == null)
        {
            enemy =
                currentTarget
                    .GetComponentInChildren<EnemyUnitBase>();
        }

        if (enemy == null)
        {
            return;
        }

        float distance =
            Vector2.Distance(
                rb.position,
                currentTarget.position
            );

        float validHitRange =
            attackKeepRange *
            attackHitRangeMultiplier;

        if (distance >
            validHitRange)
        {
            return;
        }

        enemy.TakeDamage(
            attackDamage
        );
    }

    // =========================================================
    // 정상 공격 종료
    // =========================================================

    protected virtual void FinishAttack()
    {
        // 이미 안전장치가 먼저 풀었다면
        // 중복 종료 방지
        if (!isAttacking)
        {
            return;
        }

        isAttacking =
            false;

        attackLockTimer =
            0f;

        attackTimer =
            attackInterval;

        postAttackHoldTimer =
            postAttackHoldTime;
    }

    // =========================================================
    // 타이머
    // =========================================================

    protected virtual void UpdateTimers()
    {
        if (attackTimer > 0f)
        {
            attackTimer -=
                Time.deltaTime;

            if (attackTimer < 0f)
            {
                attackTimer = 0f;
            }
        }

        if (postAttackHoldTimer > 0f)
        {
            postAttackHoldTimer -=
                Time.deltaTime;

            if (postAttackHoldTimer < 0f)
            {
                postAttackHoldTimer = 0f;
            }
        }
    }

    // =========================================================
    // 리벨 추종
    // =========================================================

    protected virtual void FollowRibel()
    {
        if (ribel == null)
        {
            StopMovement();
            return;
        }

        Vector2 destination =
            (Vector2)ribel.position +
            followOffset;

        float distance =
            Vector2.Distance(
                rb.position,
                destination
            );

        if (distance <=
            stopDistance)
        {
            isFollowingRibel =
                false;

            StopMovement();
            return;
        }

        if (!isFollowingRibel)
        {
            if (distance >=
                followStartDistance)
            {
                isFollowingRibel =
                    true;
            }
            else
            {
                StopMovement();
                return;
            }
        }

        MoveToward(
            destination
        );
    }

    // =========================================================
    // 이동
    // =========================================================

    protected virtual void MoveToward(
        Vector2 destination)
    {
        Vector2 direction =
            destination -
            rb.position;

        float distance =
            direction.magnitude;

        if (distance <=
            0.001f)
        {
            StopMovement();
            return;
        }

        direction /=
            distance;

        if (visualController != null)
        {
            visualController.SetFacingDirection(
                direction
            );

            visualController.SetMoving(
                true
            );
        }

        Vector2 nextPosition =
            Vector2.MoveTowards(
                rb.position,
                destination,
                moveSpeed *
                Time.fixedDeltaTime
            );

        rb.MovePosition(
            nextPosition
        );
    }

    // =========================================================
    // 정지
    // =========================================================

    protected virtual void StopMovement()
    {
        rb.velocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;

        if (visualController != null)
        {
            visualController.SetMoving(
                false
            );
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        maxHealth =
            Mathf.Max(
                1f,
                maxHealth
            );

        defense =
            Mathf.Max(
                0f,
                defense
            );

        moveSpeed =
            Mathf.Max(
                0f,
                moveSpeed
            );

        stopDistance =
            Mathf.Max(
                0.05f,
                stopDistance
            );

        followStartDistance =
            Mathf.Max(
                stopDistance + 0.05f,
                followStartDistance
            );

        followRadius =
            Mathf.Max(
                0.1f,
                followRadius
            );

        followRadiusRandom =
            Mathf.Max(
                0f,
                followRadiusRandom
            );

        detectionRange =
            Mathf.Max(
                0.1f,
                detectionRange
            );

        attackRange =
            Mathf.Clamp(
                attackRange,
                0.05f,
                detectionRange
            );

        attackKeepRange =
            Mathf.Clamp(
                attackKeepRange,
                attackRange,
                detectionRange
            );

        searchInterval =
            Mathf.Max(
                0.05f,
                searchInterval
            );

        attackDamage =
            Mathf.Max(
                0f,
                attackDamage
            );

        attackInterval =
            Mathf.Max(
                0.05f,
                attackInterval
            );

        postAttackHoldTime =
            Mathf.Max(
                0f,
                postAttackHoldTime
            );

        attackHitRangeMultiplier =
            Mathf.Max(
                1f,
                attackHitRangeMultiplier
            );

        maxAttackLockTime =
            Mathf.Max(
                0.2f,
                maxAttackLockTime
            );

        postSummonInvincibleTime =
            Mathf.Max(
                0f,
                postSummonInvincibleTime
            );
    }
#endif
}