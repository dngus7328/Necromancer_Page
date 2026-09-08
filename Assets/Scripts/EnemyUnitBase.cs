using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyUnitBase : MonoBehaviour
{
    public enum TargetType
    {
        Nearest,
        SummonOnly,
        HighestDefense,
        HighestMaxHealth,
        Farthest,
        RibelOnly,
        Escape
    }

    [Header("기본 능력치")]
    [SerializeField] private float maxHealth = 50f;

    [Header("처치 보상")]
    [SerializeField] private float manaReward = 5f;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("공격")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float attackInterval = 1.2f;

    [Header("타겟 설정")]
    [SerializeField] private TargetType targetType = TargetType.Nearest;

    [SerializeField] private LayerMask summonLayer;

    [Tooltip("우선 공격 대상을 발견할 수 있는 거리")]
    [SerializeField] private float detectionRange = 7f;

    [Tooltip("현재 우선 타겟을 포기하는 거리")]
    [SerializeField] private float disengageRange = 10f;

    [SerializeField] private float searchInterval = 0.2f;

    private Rigidbody2D rb;

    private Transform ribel;
    private Transform currentTarget;

    private float currentHealth;
    private float attackTimer;
    private float searchTimer;

    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public float HealthRatio
    {
        get
        {
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                currentHealth / maxHealth
            );
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        FindRibel();

        // 생성 직후 기본 목표는 무조건 리벨
        SetRibelAsTarget();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        UpdateTimers();
        UpdateTarget();
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        UpdateMovement();
    }

    // =========================================================
    // 리벨 찾기
    // =========================================================

    private void FindRibel()
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

    private void SetRibelAsTarget()
    {
        if (ribel == null)
        {
            FindRibel();
        }

        currentTarget = ribel;
    }

    // =========================================================
    // 피해 / 사망
    // =========================================================

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0f;

        StopMovement();

        if (SummonManager.Instance != null)
        {
            SummonManager.Instance.AddMana(
                manaReward
            );
        }

        Destroy(gameObject);
    }

    // =========================================================
    // 타겟 관리
    // =========================================================

    private void UpdateTarget()
    {
        if (targetType == TargetType.Escape)
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

        // 현재 타겟이 사라짐
        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            SetRibelAsTarget();
            return;
        }

        // 리벨만 공격하는 적
        if (targetType == TargetType.RibelOnly)
        {
            SetRibelAsTarget();
            return;
        }

        // =====================================================
        // 이미 소환수를 하나 타겟으로 잡았다면
        // 함부로 다른 소환수로 갈아타지 않음
        // =====================================================

        SummonUnitBase currentSummon =
            currentTarget.GetComponent<SummonUnitBase>();

        if (currentSummon != null)
        {
            float distance =
                Vector2.Distance(
                    transform.position,
                    currentTarget.position
                );

            // 너무 멀어진 경우에만 포기
            if (distance > disengageRange)
            {
                SetRibelAsTarget();
            }
            else
            {
                return;
            }
        }

        // =====================================================
        // 현재 리벨을 쫓고 있을 때만
        // 더 우선적인 소환수 탐색
        // =====================================================

        if (currentTarget == ribel)
        {
            Transform preferredTarget =
                FindPreferredTarget();

            if (preferredTarget != null &&
                preferredTarget != ribel)
            {
                currentTarget =
                    preferredTarget;
            }
        }
    }

    // =========================================================
    // 유형별 우선 타겟
    // =========================================================

    private Transform FindPreferredTarget()
    {
        switch (targetType)
        {
            case TargetType.Nearest:
                return FindNearestSummon();

            case TargetType.SummonOnly:
                return FindNearestSummon();

            case TargetType.HighestDefense:
                return FindHighestDefenseSummon();

            case TargetType.HighestMaxHealth:
                return FindHighestMaxHealthSummon();

            case TargetType.Farthest:
                return FindFarthestSummon();
        }

        return null;
    }

    // =========================================================
    // 가장 가까운 소환수
    // =========================================================

    private Transform FindNearestSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRange,
                summonLayer
            );

        Transform bestTarget = null;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < summons.Length; i++)
        {
            SummonUnitBase summon =
                summons[i].GetComponentInParent<SummonUnitBase>();

            if (summon == null)
            {
                continue;
            }

            float distance =
                Vector2.SqrMagnitude(
                    (Vector2)summon.transform.position -
                    rb.position
                );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = summon.transform;
            }
        }

        return bestTarget;
    }

    // =========================================================
    // 방어력 높은 소환수
    // =========================================================

    private Transform FindHighestDefenseSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRange,
                summonLayer
            );

        SummonUnitBase bestSummon = null;
        float highestDefense = float.MinValue;

        for (int i = 0; i < summons.Length; i++)
        {
            SummonUnitBase summon =
                summons[i].GetComponentInParent<SummonUnitBase>();

            if (summon == null)
            {
                continue;
            }

            if (summon.Defense > highestDefense)
            {
                highestDefense = summon.Defense;
                bestSummon = summon;
            }
        }

        return bestSummon != null
            ? bestSummon.transform
            : null;
    }

    // =========================================================
    // 최대 HP 높은 소환수
    // =========================================================

    private Transform FindHighestMaxHealthSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRange,
                summonLayer
            );

        SummonUnitBase bestSummon = null;
        float highestHealth = float.MinValue;

        for (int i = 0; i < summons.Length; i++)
        {
            SummonUnitBase summon =
                summons[i].GetComponentInParent<SummonUnitBase>();

            if (summon == null)
            {
                continue;
            }

            if (summon.MaxHealth > highestHealth)
            {
                highestHealth = summon.MaxHealth;
                bestSummon = summon;
            }
        }

        return bestSummon != null
            ? bestSummon.transform
            : null;
    }

    // =========================================================
    // 가장 먼 소환수
    // =========================================================

    private Transform FindFarthestSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRange,
                summonLayer
            );

        Transform bestTarget = null;
        float farthestDistance = -1f;

        for (int i = 0; i < summons.Length; i++)
        {
            SummonUnitBase summon =
                summons[i].GetComponentInParent<SummonUnitBase>();

            if (summon == null)
            {
                continue;
            }

            float distance =
                Vector2.SqrMagnitude(
                    (Vector2)summon.transform.position -
                    rb.position
                );

            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                bestTarget = summon.transform;
            }
        }

        return bestTarget;
    }

    // =========================================================
    // 이동
    // =========================================================

    private void UpdateMovement()
    {
        if (targetType == TargetType.Escape)
        {
            StopMovement();
            return;
        }

        if (currentTarget == null)
        {
            SetRibelAsTarget();

            if (currentTarget == null)
            {
                StopMovement();
                return;
            }
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

        Vector2 nextPosition =
            Vector2.MoveTowards(
                rb.position,
                currentTarget.position,
                moveSpeed * Time.fixedDeltaTime
            );

        rb.MovePosition(nextPosition);
    }

    // =========================================================
    // 공격
    // =========================================================

    private void TryAttack()
    {
        if (attackTimer > 0f ||
            currentTarget == null)
        {
            return;
        }

        SummonUnitBase summon =
            currentTarget.GetComponent<SummonUnitBase>();

        if (summon != null)
        {
            summon.TakeDamage(
                attackDamage
            );

            attackTimer =
                attackInterval;

            return;
        }

        if (currentTarget.CompareTag("Player"))
        {
            RibelHealth health =
                currentTarget.GetComponent<RibelHealth>();

            if (health != null)
            {
                health.TakeDamage(
                    attackDamage
                );
            }

            attackTimer =
                attackInterval;
        }
    }

    private void UpdateTimers()
    {
        if (attackTimer > 0f)
        {
            attackTimer -=
                Time.deltaTime;
        }
    }

    private void StopMovement()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(1f, maxHealth);

        manaReward =
            Mathf.Max(0f, manaReward);

        moveSpeed =
            Mathf.Max(0f, moveSpeed);

        attackDamage =
            Mathf.Max(0f, attackDamage);

        attackRange =
            Mathf.Max(0.05f, attackRange);

        attackInterval =
            Mathf.Max(0.05f, attackInterval);

        detectionRange =
            Mathf.Max(0.1f, detectionRange);

        disengageRange =
            Mathf.Max(
                detectionRange,
                disengageRange
            );

        searchInterval =
            Mathf.Max(0.05f, searchInterval);
    }
#endif
}