using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SummonUnitBase : MonoBehaviour
{
    [Header("리벨 추종")]
    [SerializeField] protected Transform ribel;

    [Header("기본 능력치")]
    [SerializeField] protected float maxHealth = 60f;

    [Tooltip("방어력 우선 적이 참조하는 값")]
    [SerializeField] protected float defense = 0f;

    [Header("공통 이동 설정")]
    [SerializeField] protected float moveSpeed = 3.8f;
    [SerializeField] protected float stopDistance = 0.5f;
    [SerializeField] protected float followStartDistance = 0.9f;

    [Header("리벨 주변 분산 설정")]
    [SerializeField] protected float followRadius = 1.6f;
    [SerializeField] protected float followRadiusRandom = 0.35f;

    [Header("적 탐색 설정")]
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float attackRange = 0.8f;
    [SerializeField] protected float searchInterval = 0.2f;

    [Header("공격 설정")]
    [SerializeField] protected float attackDamage = 20f;
    [SerializeField] protected float attackInterval = 1f;

    protected Rigidbody2D rb;
    protected Transform currentTarget;

    protected bool isFollowingRibel;
    protected Vector2 followOffset;

    // Q 짧게 = 리벨 집결
    protected bool isRallyingToRibel;

    // Q 홀드 + 클릭 = 지정 위치 집결
    protected bool isRallyingToPoint;
    protected Vector2 rallyPoint;

    private float currentHealth;
    private float searchTimer;
    private float attackTimer;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float Defense => defense;
    public float DetectionRange => detectionRange;

    public bool IsRallyingToRibel => isRallyingToRibel;
    public bool IsRallyingToPoint => isRallyingToPoint;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        currentHealth = maxHealth;

        CreateFollowOffset();
    }

    protected virtual void Start()
    {
        FindRibel();
    }

    protected virtual void Update()
    {
        SearchTarget();
        UpdateAttackTimer();
    }

    protected virtual void FixedUpdate()
    {
        UpdateMovement();
    }

    // =========================================================
    // 피해
    // =========================================================

    public virtual void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        float finalDamage =
            Mathf.Max(1f, damage - defense);

        currentHealth -= finalDamage;

        Debug.Log(
            $"{gameObject.name} 피해 {finalDamage} | HP {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // =========================================================
    // 사망
    // =========================================================

    protected virtual void Die()
    {
        currentHealth = 0f;

        if (SummonManager.Instance != null)
        {
            SummonManager.Instance.ReleaseCapacity(1);
        }

        Destroy(gameObject);
    }

    // =========================================================
    // 리벨 찾기
    // =========================================================

    protected virtual void FindRibel()
    {
        if (ribel != null)
        {
            return;
        }

        GameObject ribelObject =
            GameObject.FindGameObjectWithTag("Player");

        if (ribelObject != null)
        {
            ribel = ribelObject.transform;
        }
    }

    // =========================================================
    // 리벨 주변 위치 생성
    // =========================================================

    protected virtual void CreateFollowOffset()
    {
        float angle =
            Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float radius =
            followRadius +
            Random.Range(
                -followRadiusRandom,
                followRadiusRandom
            );

        radius = Mathf.Max(0.1f, radius);

        followOffset =
            new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;
    }

    // =========================================================
    // Q 짧게 - 리벨 집결
    // =========================================================

    public virtual void RallyToRibel()
    {
        FindRibel();

        if (ribel == null)
        {
            return;
        }

        currentTarget = null;

        isFollowingRibel = false;

        isRallyingToPoint = false;
        isRallyingToRibel = true;
    }

    // =========================================================
    // Q 홀드 + 클릭 - 지정 위치 집결
    // =========================================================

    public virtual void RallyToPoint(Vector2 point)
    {
        currentTarget = null;

        isFollowingRibel = false;

        isRallyingToRibel = false;
        isRallyingToPoint = true;

        // 소환수마다 약간 다른 위치에 서도록
        rallyPoint =
            point +
            followOffset * 0.45f;
    }

    // =========================================================
    // 적 탐색
    // =========================================================

    protected virtual void SearchTarget()
    {
        // 집결 명령 중에는 적을 무시한다.
        if (isRallyingToRibel ||
            isRallyingToPoint)
        {
            currentTarget = null;
            return;
        }

        searchTimer -= Time.deltaTime;

        if (searchTimer > 0f)
        {
            return;
        }

        searchTimer = searchInterval;

        if (currentTarget != null)
        {
            float distance =
                Vector2.Distance(
                    transform.position,
                    currentTarget.position
                );

            if (!currentTarget.gameObject.activeInHierarchy ||
                distance > detectionRange)
            {
                currentTarget = null;
            }
        }

        if (currentTarget != null)
        {
            return;
        }

        Collider2D[] enemies =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRange,
                enemyLayer
            );

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyUnitBase enemy =
                enemies[i].GetComponentInParent<EnemyUnitBase>();

            if (enemy == null)
            {
                continue;
            }

            float distance =
                Vector2.SqrMagnitude(
                    (Vector2)enemy.transform.position -
                    rb.position
                );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy.transform;
            }
        }

        currentTarget = nearestEnemy;
    }

    // =========================================================
    // 이동 결정
    // =========================================================

    protected virtual void UpdateMovement()
    {
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
            FollowTarget();
            return;
        }

        FollowRibel();
    }

    // =========================================================
    // 리벨 집결 이동
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

        if (distance <= stopDistance)
        {
            isRallyingToRibel = false;
            isFollowingRibel = false;

            StopMovement();
            return;
        }

        MoveToward(destination);
    }

    // =========================================================
    // 지정 위치 집결 이동
    // =========================================================

    protected virtual void UpdateRallyToPoint()
    {
        float distance =
            Vector2.Distance(
                rb.position,
                rallyPoint
            );

        if (distance <= stopDistance)
        {
            isRallyingToPoint = false;

            StopMovement();
            return;
        }

        MoveToward(rallyPoint);
    }

    // =========================================================
    // 적 추적
    // =========================================================

    protected virtual void FollowTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        float distance =
            Vector2.Distance(
                rb.position,
                currentTarget.position
            );

        if (distance <= attackRange)
        {
            StopMovement();
            TryAttack();
            return;
        }

        MoveToward(currentTarget.position);
    }

    // =========================================================
    // 공격
    // =========================================================

    protected virtual void TryAttack()
    {
        if (attackTimer > 0f ||
            currentTarget == null)
        {
            return;
        }

        EnemyUnitBase enemy =
            currentTarget.GetComponent<EnemyUnitBase>();

        if (enemy == null)
        {
            currentTarget = null;
            return;
        }

        enemy.TakeDamage(attackDamage);

        attackTimer = attackInterval;
    }

    protected virtual void UpdateAttackTimer()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    // =========================================================
    // 평상시 리벨 추종
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

        if (distance <= stopDistance)
        {
            isFollowingRibel = false;
            StopMovement();
            return;
        }

        if (!isFollowingRibel)
        {
            if (distance >= followStartDistance)
            {
                isFollowingRibel = true;
            }
            else
            {
                StopMovement();
                return;
            }
        }

        MoveToward(destination);
    }

    // =========================================================
    // 이동
    // =========================================================

    protected virtual void MoveToward(Vector2 destination)
    {
        Vector2 nextPosition =
            Vector2.MoveTowards(
                rb.position,
                destination,
                moveSpeed * Time.fixedDeltaTime
            );

        rb.MovePosition(nextPosition);
    }

    protected virtual void StopMovement()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        defense = Mathf.Max(0f, defense);

        moveSpeed = Mathf.Max(0f, moveSpeed);
        stopDistance = Mathf.Max(0.05f, stopDistance);

        followStartDistance =
            Mathf.Max(
                stopDistance + 0.05f,
                followStartDistance
            );

        followRadius = Mathf.Max(0.1f, followRadius);
        followRadiusRandom = Mathf.Max(0f, followRadiusRandom);

        detectionRange = Mathf.Max(0.1f, detectionRange);

        attackRange =
            Mathf.Clamp(
                attackRange,
                0.05f,
                detectionRange
            );

        searchInterval = Mathf.Max(0.05f, searchInterval);
        attackDamage = Mathf.Max(0f, attackDamage);
        attackInterval = Mathf.Max(0.05f, attackInterval);
    }
#endif
}