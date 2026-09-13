using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SummonUnitBase : MonoBehaviour
{
    [Header("리벨")]
    [SerializeField]
    protected Transform ribel;

    [Header("기본 능력치")]
    [SerializeField]
    protected float maxHealth = 60f;

    [SerializeField]
    protected float defense = 0f;

    [Header("이동")]
    [SerializeField]
    protected float moveSpeed = 3.8f;

    [SerializeField]
    protected float stopDistance = 0.5f;

    [SerializeField]
    protected float followStartDistance = 0.9f;

    [Header("리벨 주변 분산")]
    [SerializeField]
    protected float followRadius = 1.6f;

    [SerializeField]
    protected float followRadiusRandom = 0.35f;

    [Header("적 탐색")]
    [SerializeField]
    protected LayerMask enemyLayer;

    [SerializeField]
    protected float detectionRange = 5f;

    [SerializeField]
    protected float searchInterval = 0.2f;

    [Header("공격 거리")]
    [SerializeField]
    protected float attackRange = 0.22f;

    [SerializeField]
    protected float attackKeepRange = 0.38f;

    [Header("공격")]
    [SerializeField]
    protected float attackDamage = 20f;

    [SerializeField]
    protected float attackInterval = 1f;

    [SerializeField]
    protected float postAttackHoldTime = 0.18f;

    [SerializeField]
    protected float attackHitRangeMultiplier = 1.15f;

    [Header("공격 안전장치")]
    [SerializeField]
    protected float maxAttackLockTime = 1.5f;

    [Header("소환 보호")]
    [SerializeField]
    protected float postSummonInvincibleTime = 0.5f;

    [Header("비주얼")]
    [SerializeField]
    protected SummonVisualController visualController;

    // =========================================================
    // Runtime
    // =========================================================

    protected Rigidbody2D rb;

    protected Transform currentTarget;

    protected bool isFollowingRibel;

    protected Vector2 followOffset;

    protected bool isRallyingToRibel;

    protected bool isRallyingToPoint;

    protected Vector2 rallyPoint;

    private Collider2D bodyCollider;

    private float currentHealth;

    // 강화계약 적용 전 원본 능력치
    private float baseMaxHealth;

    private float baseAttackDamage;

    private float searchTimer;

    private float attackTimer;

    private float postAttackHoldTimer;

    private float attackLockTimer;

    private bool isDead;

    private bool isAttacking;

    private bool isInAttackMode;

    private bool isSummoning;

    private float summonInvincibleTimer;

    private Collider2D[] cachedColliders;

    private bool[] colliderOriginalStates;

    private SummonSpawnGroup summonGroup;

    private SummonData summonData;

    private SummonBodySize summonBodySize =
        SummonBodySize.Medium;

    // =========================================================
    // Property
    // =========================================================

    public float CurrentHealth =>
        currentHealth;

    public float MaxHealth =>
        maxHealth;

    public float Defense =>
        defense;

    public float DetectionRange =>
        detectionRange;

    public bool IsRallyingToRibel =>
        isRallyingToRibel;

    public bool IsRallyingToPoint =>
        isRallyingToPoint;

    public bool IsDead =>
        isDead;

    public bool IsSummoning =>
        isSummoning;

    public SummonBodySize SummonBodySize =>
        summonBodySize;

    public int GroupAliveMembers =>
        summonGroup != null
            ? summonGroup.AliveMembers
            : 1;

    public int GroupTotalMembers =>
        summonGroup != null
            ? summonGroup.TotalMembers
            : 1;

    public bool IsSummonInvincible =>
        isSummoning ||
        summonInvincibleTimer > 0f;

    // =========================================================
    // Unity
    // =========================================================

    protected virtual void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        if (visualController == null)
        {
            visualController =
                GetComponentInChildren<SummonVisualController>();
        }

        bodyCollider =
            GetComponent<Collider2D>();

        if (bodyCollider == null)
        {
            Collider2D[] colliders =
                GetComponentsInChildren<Collider2D>();

            for (int i = 0;
                 i < colliders.Length;
                 i++)
            {
                if (colliders[i] != null &&
                    !colliders[i].isTrigger)
                {
                    bodyCollider =
                        colliders[i];

                    break;
                }
            }
        }

        baseMaxHealth =
            maxHealth;

        baseAttackDamage =
            attackDamage;

        maxHealth =
            AugmentRuntimeEffects.GetSummonMaxHealth(
                baseMaxHealth
            );

        attackDamage =
            AugmentRuntimeEffects.GetSummonDamage(
                baseAttackDamage
            );

        currentHealth =
            maxHealth;

        CacheColliders();

        CreateFollowOffset();
    }

    public void RefreshAugmentStats()
    {
        float previousMaxHealth =
            Mathf.Max(1f, maxHealth);

        float healthRatio =
            currentHealth > 0f
                ? Mathf.Clamp01(currentHealth / previousMaxHealth)
                : 0f;

        maxHealth =
            AugmentRuntimeEffects.GetSummonMaxHealth(
                baseMaxHealth
            );

        attackDamage =
            AugmentRuntimeEffects.GetSummonDamage(
                baseAttackDamage
            );

        if (!isDead && currentHealth > 0f)
        {
            currentHealth =
                Mathf.Clamp(
                    maxHealth * healthRatio,
                    1f,
                    maxHealth
                );
        }
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
    // ★ Manager가 소환 직후 호출
    // =========================================================

    public void InitializeSummon(
        SummonSpawnGroup group,
        SummonData data)
    {
        summonGroup =
            group;

        summonData =
            data;

        if (summonData != null)
        {
            summonBodySize =
                summonData.SummonBodySize;
        }

        if (visualController != null)
        {
            visualController.ApplySummonData(
                summonData
            );
        }
    }

    // 이전 호출 호환
    public void InitializeSummonGroup(
        SummonSpawnGroup group,
        SummonBodySize bodySize)
    {
        summonGroup =
            group;

        summonBodySize =
            bodySize;
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

        isSummoning =
            true;

        summonInvincibleTimer =
            0f;

        currentTarget =
            null;

        ResetAttackState();

        isRallyingToRibel =
            false;

        isRallyingToPoint =
            false;

        StopMovement();

        SetCollidersEnabled(
            false
        );

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

        isSummoning =
            false;

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
            summonInvincibleTimer =
                0f;

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
    // 거리
    // =========================================================

    private float GetTargetDistance()
    {
        if (currentTarget == null)
        {
            return Mathf.Infinity;
        }

        Collider2D targetCollider =
            currentTarget.GetComponent<Collider2D>();

        if (targetCollider == null)
        {
            targetCollider =
                currentTarget
                    .GetComponentInChildren<Collider2D>();
        }

        if (bodyCollider != null &&
            targetCollider != null &&
            bodyCollider.enabled &&
            targetCollider.enabled)
        {
            ColliderDistance2D result =
                bodyCollider.Distance(
                    targetCollider
                );

            return Mathf.Max(
                0f,
                result.distance
            );
        }

        return Vector2.Distance(
            rb.position,
            currentTarget.position
        );
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
            currentHealth =
                0f;

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

        isDead =
            true;

        currentHealth =
            0f;

        currentTarget =
            null;

        ResetAttackState();

        isSummoning =
            false;

        summonInvincibleTimer =
            0f;

        StopMovement();

        HideHealthBarsImmediately();

        SetCollidersEnabled(
            false
        );

        // 그룹의 마지막 개체가 죽었을 때만
        // 용량 반환
        if (summonGroup != null)
        {
            summonGroup.NotifyMemberDied();
        }
        else if (SummonManager.Instance != null)
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

    private void HideHealthBarsImmediately()
    {
        SummonHealthBar[] healthBars =
            GetComponentsInChildren<SummonHealthBar>(
                true
            );

        for (int i = 0;
             i < healthBars.Length;
             i++)
        {
            if (healthBars[i] == null)
            {
                continue;
            }

            healthBars[i]
                .gameObject
                .SetActive(
                    false
                );
        }
    }

    private void DestroyAfterDeath()
    {
        Destroy(
            gameObject
        );
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

        isFollowingRibel =
            false;

        isRallyingToPoint =
            false;

        isRallyingToRibel =
            true;
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

        isFollowingRibel =
            false;

        isRallyingToRibel =
            false;

        isRallyingToPoint =
            true;

        rallyPoint =
            point +
            followOffset *
            0.45f;
    }

    private void CancelCombatState()
    {
        currentTarget =
            null;

        ResetAttackState();
    }

    private void ResetAttackState()
    {
        isAttacking =
            false;

        isInAttackMode =
            false;

        attackTimer =
            0f;

        postAttackHoldTimer =
            0f;

        attackLockTimer =
            0f;
    }

    // =========================================================
    // 공격 Safety
    // =========================================================

    private void UpdateAttackSafety()
    {
        if (!isAttacking)
        {
            attackLockTimer =
                0f;

            return;
        }

        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            ForceReleaseAttack();

            return;
        }

        attackLockTimer +=
            Time.deltaTime;

        if (attackLockTimer >=
            maxAttackLockTime)
        {
            ForceReleaseAttack();
        }
    }

    private void ForceReleaseAttack()
    {
        isAttacking =
            false;

        attackLockTimer =
            0f;

        attackTimer =
            Mathf.Max(
                attackTimer,
                attackInterval
            );

        postAttackHoldTimer =
            0f;

        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget =
                null;

            isInAttackMode =
                false;

            return;
        }

        if (GetTargetDistance() >
            attackKeepRange)
        {
            isInAttackMode =
                false;
        }
    }

    // =========================================================
    // Target
    // =========================================================

    protected virtual void SearchTarget()
    {
        if (isRallyingToRibel ||
            isRallyingToPoint)
        {
            currentTarget =
                null;

            isInAttackMode =
                false;

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
                currentTarget =
                    null;

                isInAttackMode =
                    false;

                isAttacking =
                    false;

                attackLockTimer =
                    0f;
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

        Transform nearest =
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

                nearest =
                    enemy.transform;
            }
        }

        currentTarget =
            nearest;

        isInAttackMode =
            false;
    }

    // =========================================================
    // Movement
    // =========================================================

    protected virtual void UpdateMovement()
    {
        if (isAttacking ||
            postAttackHoldTimer > 0f)
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

        if (Vector2.Distance(
                rb.position,
                destination) <=
            stopDistance)
        {
            isRallyingToRibel =
                false;

            isFollowingRibel =
                false;

            StopMovement();

            return;
        }

        MoveToward(
            destination
        );
    }

    protected virtual void UpdateRallyToPoint()
    {
        if (Vector2.Distance(
                rb.position,
                rallyPoint) <=
            stopDistance)
        {
            isRallyingToPoint =
                false;

            StopMovement();

            return;
        }

        MoveToward(
            rallyPoint
        );
    }

    // =========================================================
    // Attack AI
    // =========================================================

    protected virtual void FollowOrAttackTarget()
    {
        if (currentTarget == null)
        {
            isInAttackMode =
                false;

            return;
        }

        Vector2 toTarget =
            (Vector2)currentTarget.position -
            rb.position;

        float distance =
            GetTargetDistance();

        if (isInAttackMode)
        {
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
                visualController
                    .SetCombatFacingDirection(
                        toTarget
                    );
            }

            TryAttack();

            return;
        }

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
                visualController
                    .SetCombatFacingDirection(
                        toTarget
                    );
            }

            TryAttack();

            return;
        }

        MoveToward(
            currentTarget.position
        );
    }

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
            currentTarget =
                null;

            isInAttackMode =
                false;

            return;
        }

        Vector2 toTarget =
            (Vector2)currentTarget.position -
            rb.position;

        if (visualController != null &&
            toTarget.sqrMagnitude >
            0.0001f)
        {
            visualController
                .SetCombatFacingDirection(
                    toTarget
                );
        }

        isAttacking =
            true;

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

    protected virtual void ApplyAttackDamage()
    {
        if (isDead ||
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

        float validHitRange =
            attackKeepRange *
            attackHitRangeMultiplier;

        if (GetTargetDistance() >
            validHitRange)
        {
            return;
        }

        enemy.TakeDamage(
            attackDamage
        );
    }

    protected virtual void FinishAttack()
    {
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
    // Timer
    // =========================================================

    protected virtual void UpdateTimers()
    {
        if (attackTimer > 0f)
        {
            attackTimer -=
                Time.deltaTime;

            attackTimer =
                Mathf.Max(
                    0f,
                    attackTimer
                );
        }

        if (postAttackHoldTimer > 0f)
        {
            postAttackHoldTimer -=
                Time.deltaTime;

            postAttackHoldTimer =
                Mathf.Max(
                    0f,
                    postAttackHoldTimer
                );
        }
    }

    // =========================================================
    // Follow
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
            visualController
                .SetFacingDirection(
                    direction
                );

            visualController
                .SetMoving(
                    true
                );
        }

        Vector2 next =
            Vector2.MoveTowards(
                rb.position,
                destination,
                moveSpeed *
                Time.fixedDeltaTime
            );

        rb.MovePosition(
            next
        );
    }

    protected virtual void StopMovement()
    {
        if (rb != null)
        {
            rb.velocity =
                Vector2.zero;

            rb.angularVelocity =
                0f;
        }

        if (visualController != null)
        {
            visualController
                .SetMoving(
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
                stopDistance +
                0.05f,
                followStartDistance
            );

        detectionRange =
            Mathf.Max(
                0.1f,
                detectionRange
            );

        attackRange =
            Mathf.Clamp(
                attackRange,
                0.01f,
                detectionRange
            );

        attackKeepRange =
            Mathf.Clamp(
                attackKeepRange,
                attackRange,
                detectionRange
            );

        attackInterval =
            Mathf.Max(
                0.05f,
                attackInterval
            );

        maxAttackLockTime =
            Mathf.Max(
                0.2f,
                maxAttackLockTime
            );
    }

#endif
}