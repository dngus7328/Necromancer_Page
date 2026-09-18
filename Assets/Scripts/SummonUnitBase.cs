using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody2D))]
public class SummonUnitBase : MonoBehaviour
{
    // =========================================================
    // 리벨
    // =========================================================

    [Header("리벨")]
    [SerializeField]
    protected Transform ribel;

    // =========================================================
    // 기본 능력치
    // =========================================================

    [Header("기본 능력치")]
    [SerializeField]
    protected float maxHealth = 60f;

    [SerializeField]
    protected float defense = 0f;

    // =========================================================
    // 이동
    // =========================================================

    [Header("이동")]
    [SerializeField]
    protected float moveSpeed = 3.8f;

    [SerializeField]
    protected float stopDistance = 0.5f;

    [SerializeField]
    protected float followStartDistance = 0.9f;

    // =========================================================
    // 리벨 주변 분산
    // =========================================================

    [Header("리벨 주변 분산")]
    [SerializeField]
    protected float followRadius = 1.6f;

    [SerializeField]
    protected float followRadiusRandom = 0.35f;

    // =========================================================
    // 적 탐색
    // =========================================================

    [Header("적 탐색")]
    [SerializeField]
    protected LayerMask enemyLayer;

    [SerializeField]
    protected float detectionRange = 5f;

    [SerializeField]
    protected float searchInterval = 0.2f;

    // =========================================================
    // 공격 거리
    // =========================================================

    [Header("공격 거리")]
    [SerializeField]
    protected float attackRange = 0.22f;

    [SerializeField]
    protected float attackKeepRange = 0.38f;

    // =========================================================
    // 공격
    // =========================================================

    [Header("공격")]
    [SerializeField]
    protected float attackDamage = 20f;

    [SerializeField]
    protected float attackInterval = 1f;

    [SerializeField]
    protected float postAttackHoldTime = 0.18f;

    [SerializeField]
    protected float attackHitRangeMultiplier = 1.15f;

    // =========================================================
    // 공격 이펙트
    // =========================================================

    [Header("공격 이펙트")]
    [SerializeField]
    protected AttackEffectEmitter attackEffectEmitter;

    // =========================================================
    // 공격 안전장치
    // =========================================================

    [Header("공격 안전장치")]
    [SerializeField]
    protected float maxAttackLockTime = 1.5f;

    // =========================================================
    // 소환 보호
    // =========================================================

    [Header("소환 보호")]
    [SerializeField]
    protected float postSummonInvincibleTime = 0.5f;

    // =========================================================
    // 비주얼
    // =========================================================

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

    protected Vector2 attackDirection =
        Vector2.right;

    private Collider2D bodyCollider;

    [SerializeField]
    private SpriteRenderer bodySpriteRenderer;

    private SortingGroup renderSortingGroup;

    // 소환 등장 중에는 Collider를 잠시 끄기 때문에
    // 마지막으로 유효했던 발 위치를 기억해서 정렬에 사용합니다.
    private float lastValidFeetY;

    private bool hasValidFeetY;

    private float currentHealth;

    private float baseMaxHealth;

    private float baseAttackDamage;

    // =========================================================
    // 보호막 Runtime
    // =========================================================

    private float currentShield;

    private float shieldTimer;

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

    public float CurrentShield =>
        currentShield;

    public float ShieldRatio =>
        maxHealth > 0f
            ? Mathf.Clamp01(
                currentShield /
                maxHealth
            )
            : 0f;

    public bool HasShield =>
        currentShield > 0f &&
        shieldTimer > 0f;

    public float ShieldTimeRemaining =>
        Mathf.Max(
            0f,
            shieldTimer
        );

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

    // =========================================================
    // 소환수 이름
    // =========================================================

    public string SummonName
    {
        get
        {
            if (summonData != null &&
                !string.IsNullOrWhiteSpace(
                    summonData.SummonName
                ))
            {
                return summonData.SummonName;
            }

            return gameObject.name;
        }
    }

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

        if (attackEffectEmitter == null)
        {
            attackEffectEmitter =
                GetComponentInChildren<AttackEffectEmitter>();
        }

        bodyCollider =
            GetComponent<Collider2D>();

        if (bodyCollider == null ||
            bodyCollider.isTrigger)
        {
            Collider2D[] colliders =
                GetComponentsInChildren<Collider2D>();

            bodyCollider = null;

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

        if (bodySpriteRenderer == null)
        {
            bodySpriteRenderer =
                FindBodySpriteRenderer();
        }

        renderSortingGroup =
            GetComponent<SortingGroup>();

        if (renderSortingGroup == null)
        {
            renderSortingGroup =
                gameObject.AddComponent<SortingGroup>();
        }

        if (bodySpriteRenderer != null)
        {
            renderSortingGroup.sortingLayerID =
                bodySpriteRenderer.sortingLayerID;
        }

        CacheCurrentFeetY();

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

        currentShield =
            0f;

        shieldTimer =
            0f;

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

        UpdateShield();

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

    protected virtual void LateUpdate()
    {
        UpdateYSorting();
    }

    // =========================================================
    // 실제 본체 SpriteRenderer 찾기
    // =========================================================

    private SpriteRenderer FindBodySpriteRenderer()
    {
        Transform searchRoot =
            visualController != null
                ? visualController.transform
                : transform;

        SpriteRenderer direct =
            searchRoot.GetComponent<SpriteRenderer>();

        if (IsBodySpriteRenderer(direct))
        {
            return direct;
        }

        SpriteRenderer[] renderers =
            searchRoot.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (IsBodySpriteRenderer(
                    renderers[i]))
            {
                return renderers[i];
            }
        }

        return null;
    }

    private bool IsBodySpriteRenderer(
        SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        string objectName =
            renderer.gameObject.name.ToLowerInvariant();

        if (objectName.Contains("shadow") ||
            objectName.Contains("hp") ||
            objectName.Contains("health") ||
            objectName.Contains("bar") ||
            objectName.Contains("marker") ||
            objectName.Contains("range") ||
            objectName.Contains("minimap") ||
            objectName.Contains("effect") ||
            objectName.Contains("preview"))
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // Collider 바닥 기준 앞뒤 정렬
    // =========================================================

    private void CacheCurrentFeetY()
    {
        if (bodyCollider == null ||
            !bodyCollider.enabled)
        {
            return;
        }

        lastValidFeetY =
            bodyCollider.bounds.min.y;

        hasValidFeetY =
            true;
    }

    private void UpdateYSorting()
    {
        if (renderSortingGroup == null)
        {
            return;
        }

        float feetY;

        if (bodyCollider != null &&
            bodyCollider.enabled)
        {
            feetY =
                bodyCollider.bounds.min.y;

            lastValidFeetY =
                feetY;

            hasValidFeetY =
                true;
        }
        else if (hasValidFeetY)
        {
            // 소환 등장 중 Collider가 꺼져 있어도
            // 등장 직전의 실제 발 위치로 계속 정렬합니다.
            feetY =
                lastValidFeetY;
        }
        else
        {
            // 아주 예외적인 경우의 안전장치
            feetY =
                transform.position.y;
        }

        renderSortingGroup.sortingOrder =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    -feetY * 1000f
                ),
                -32000,
                32000
            );
    }

    // =========================================================
    // 강화 능력치 갱신
    // =========================================================

    public void RefreshAugmentStats()
    {
        if (isDead)
        {
            return;
        }

        float previousMaxHealth =
            Mathf.Max(
                1f,
                maxHealth
            );

        float previousAttackDamage =
            attackDamage;

        float healthRatio =
            currentHealth > 0f
                ? Mathf.Clamp01(
                    currentHealth /
                    previousMaxHealth
                )
                : 0f;

        maxHealth =
            AugmentRuntimeEffects.GetSummonMaxHealth(
                baseMaxHealth
            );

        attackDamage =
            AugmentRuntimeEffects.GetSummonDamage(
                baseAttackDamage
            );

        if (currentHealth > 0f)
        {
            currentHealth =
                Mathf.Clamp(
                    maxHealth *
                    healthRatio,
                    0f,
                    maxHealth
                );
        }

        currentShield =
            Mathf.Clamp(
                currentShield,
                0f,
                maxHealth
            );

        bool statIncreased =
            maxHealth >
            previousMaxHealth +
            0.001f ||
            attackDamage >
            previousAttackDamage +
            0.001f;

        if (statIncreased &&
            visualController != null)
        {
            visualController.PlayStatUpEffect();
        }
    }

    // =========================================================
    // Manager 초기화
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

        // Collider를 끄기 직전 현재 발 위치를 저장합니다.
        // 그래서 소환 애니메이션 중에도 Y 정렬이 흔들리지 않습니다.
        CacheCurrentFeetY();

        UpdateYSorting();

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
    // 보호막
    // =========================================================

    public virtual void AddShield(
        float amount,
        float duration)
    {
        if (isDead ||
            amount <= 0f ||
            duration <= 0f)
        {
            return;
        }

        currentShield =
            Mathf.Max(
                currentShield,
                amount
            );

        shieldTimer =
            Mathf.Max(
                shieldTimer,
                duration
            );
    }

    public virtual void AddShieldByMaxHealthPercent(
        float percent,
        float duration)
    {
        if (percent <= 0f)
        {
            return;
        }

        AddShield(
            maxHealth * percent,
            duration
        );
    }

    public virtual void ClearShield()
    {
        currentShield =
            0f;

        shieldTimer =
            0f;
    }

    private void UpdateShield()
    {
        if (currentShield <= 0f)
        {
            currentShield =
                0f;

            shieldTimer =
                0f;

            return;
        }

        shieldTimer -=
            Time.deltaTime;

        if (shieldTimer <= 0f)
        {
            ClearShield();
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

        if (currentShield > 0f)
        {
            float absorbedDamage =
                Mathf.Min(
                    currentShield,
                    finalDamage
                );

            currentShield -=
                absorbedDamage;

            finalDamage -=
                absorbedDamage;

            if (currentShield <= 0f)
            {
                currentShield =
                    0f;

                shieldTimer =
                    0f;
            }
        }

        if (finalDamage <= 0f)
        {
            if (visualController != null)
            {
                visualController.PlayHit();
            }

            return;
        }

        currentHealth -=
            finalDamage;

        if (currentHealth <= 0.0001f)
        {
            currentHealth =
                0f;

            ClearShield();

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

        ClearShield();

        currentTarget =
            null;

        ResetAttackState();

        isSummoning =
            false;

        summonInvincibleTimer =
            0f;

        StopMovement();

        SetCollidersEnabled(
            false
        );

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
    // 공격 안전장치
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
    // 적 탐색
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

            if (enemy == null ||
                enemy.IsDead)
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
    // 이동
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
    // 공격 AI
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

    // =========================================================
    // 공격 시작
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

        if (enemy == null ||
            enemy.IsDead)
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

        if (toTarget.sqrMagnitude >
            0.0001f)
        {
            attackDirection =
                toTarget.normalized;
        }

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

    // =========================================================
    // 실제 공격
    // =========================================================

    protected virtual void ApplyAttackDamage()
    {
        if (isDead)
        {
            return;
        }

        // =====================================================
        // 소환수 공격 이펙트
        // =====================================================

        if (attackEffectEmitter != null)
        {
            Vector2 direction =
                attackDirection;

            if (direction.sqrMagnitude <
                0.0001f)
            {
                direction =
                    Vector2.right;
            }

            attackEffectEmitter.PlayEffect(
                direction,
                AttackEffectEmitter.OwnerType.Summon
            );
        }

        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
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

        if (enemy == null ||
            enemy.IsDead)
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

    // =========================================================
    // 공격 종료
    // =========================================================

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

    // =========================================================
    // 정지
    // =========================================================

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

    // =========================================================
    // Inspector 값 보호
    // =========================================================

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

        attackHitRangeMultiplier =
            Mathf.Max(
                0.1f,
                attackHitRangeMultiplier
            );

        maxAttackLockTime =
            Mathf.Max(
                0.2f,
                maxAttackLockTime
            );
    }

#endif
}