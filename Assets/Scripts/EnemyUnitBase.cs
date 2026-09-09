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

    [Tooltip("Collider 표면끼리 이 거리 안에 들어오면 공격 시작")]
    [SerializeField] private float attackRange = 0.15f;

    [Tooltip("공격을 시작한 뒤 이 거리까지는 움직이지 않고 계속 공격")]
    [SerializeField] private float attackKeepRange = 0.35f;

    [SerializeField] private float attackInterval = 1.2f;

    [Header("타깃 설정")]
    [SerializeField]
    private TargetType targetType =
        TargetType.Nearest;

    [SerializeField] private LayerMask summonLayer;

    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float disengageRange = 10f;
    [SerializeField] private float searchInterval = 0.2f;

    private Rigidbody2D rb;

    private Transform ribel;
    private Transform currentTarget;

    private Collider2D bodyCollider;

    private float currentHealth;
    private float attackTimer;
    private float searchTimer;

    private bool isDead;
    private bool isAttackMode;

    // 공격 중 위치를 잠갔다가
    // 원래 Rigidbody 설정으로 정확히 되돌리기 위한 값
    private RigidbodyConstraints2D normalConstraints;

    public float CurrentHealth =>
        currentHealth;

    public float MaxHealth =>
        maxHealth;

    public float HealthRatio
    {
        get
        {
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                currentHealth /
                maxHealth
            );
        }
    }

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        // Inspector에 원래 설정돼 있던 Constraints 기억
        // 예: Freeze Rotation Z
        normalConstraints =
            rb.constraints;

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

        currentHealth =
            maxHealth;
    }

    private void Start()
    {
        FindRibel();
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
            StopMovement();
            return;
        }

        UpdateMovement();
    }

    // =========================================================
    // 리벨
    // =========================================================

    private void FindRibel()
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

    private void SetRibelAsTarget()
    {
        // 다른 공격 상태에서 빠져나오는 경우
        // 위치 잠금도 반드시 해제
        ExitAttackMode();

        if (ribel == null)
        {
            FindRibel();
        }

        currentTarget =
            ribel;
    }

    // =========================================================
    // 공격 모드 시작 / 종료
    // =========================================================

    private void EnterAttackMode()
    {
        if (isAttackMode)
        {
            LockAttackPosition();
            return;
        }

        isAttackMode =
            true;

        LockAttackPosition();
    }

    private void ExitAttackMode()
    {
        if (!isAttackMode)
        {
            // 혹시 이전 상태가 꼬여 Constraints만 남아 있어도
            // 원래 값으로 복구
            UnlockAttackPosition();
            return;
        }

        isAttackMode =
            false;

        UnlockAttackPosition();
    }

    // =========================================================
    // 공격 중 위치 완전 고정
    // =========================================================

    private void LockAttackPosition()
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;

        rb.constraints =
            normalConstraints |
            RigidbodyConstraints2D.FreezePositionX |
            RigidbodyConstraints2D.FreezePositionY |
            RigidbodyConstraints2D.FreezeRotation;
    }

    private void UnlockAttackPosition()
    {
        if (rb == null)
        {
            return;
        }

        rb.constraints =
            normalConstraints;

        rb.velocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;
    }

    // =========================================================
    // 피해
    // =========================================================

    public void TakeDamage(
        float damage)
    {
        if (isDead ||
            damage <= 0f)
        {
            return;
        }

        currentHealth -=
            damage;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;

            Die();
        }
    }

    // =========================================================
    // 사망
    // =========================================================

    private void Die()
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

        ExitAttackMode();

        StopMovement();

        if (SummonManager.Instance != null)
        {
            SummonManager.Instance.AddMana(
                manaReward
            );
        }

        Destroy(
            gameObject
        );
    }

    // =========================================================
    // 실제 Collider 표면 사이 거리
    // =========================================================

    private float GetTargetDistance()
    {
        if (currentTarget == null)
        {
            return Mathf.Infinity;
        }

        Collider2D targetCollider =
            currentTarget
                .GetComponent<Collider2D>();

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

        // Collider를 못 찾은 경우만 중심 거리 사용
        return Vector2.Distance(
            rb.position,
            currentTarget.position
        );
    }

    // =========================================================
    // 타깃 관리
    // =========================================================

    private void UpdateTarget()
    {
        if (targetType ==
            TargetType.Escape)
        {
            currentTarget =
                null;

            ExitAttackMode();

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

        // 현재 타깃이 사라졌으면 리벨로 복귀
        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            SetRibelAsTarget();
            return;
        }

        // =====================================================
        // 공격 중에는 타깃 절대 변경 안 함
        // =====================================================

        if (isAttackMode)
        {
            float centerDistance =
                Vector2.Distance(
                    rb.position,
                    currentTarget.position
                );

            // 정말 멀리 도망간 경우에만
            // 공격 모드 해제
            if (centerDistance >
                disengageRange)
            {
                ExitAttackMode();
                SetRibelAsTarget();
            }

            return;
        }

        // =====================================================
        // 리벨만 공격하는 적
        // =====================================================

        if (targetType ==
            TargetType.RibelOnly)
        {
            if (currentTarget !=
                ribel)
            {
                SetRibelAsTarget();
            }

            return;
        }

        // =====================================================
        // 이미 소환수를 타깃으로 잡고 있다면 유지
        // =====================================================

        SummonUnitBase currentSummon =
            currentTarget
                .GetComponent<SummonUnitBase>();

        if (currentSummon == null)
        {
            currentSummon =
                currentTarget
                    .GetComponentInChildren<SummonUnitBase>();
        }

        if (currentSummon != null)
        {
            float distance =
                Vector2.Distance(
                    rb.position,
                    currentTarget.position
                );

            if (distance >
                disengageRange)
            {
                SetRibelAsTarget();
            }

            return;
        }

        // =====================================================
        // 리벨을 쫓는 중이면
        // 특성에 맞는 소환수 확인
        // =====================================================

        if (currentTarget ==
            ribel)
        {
            Transform preferredTarget =
                FindPreferredTarget();

            if (preferredTarget != null &&
                preferredTarget != ribel)
            {
                currentTarget =
                    preferredTarget;

                ExitAttackMode();
            }
        }
    }

    // =========================================================
    // 타깃 선택
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

    private Transform FindNearestSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                rb.position,
                detectionRange,
                summonLayer
            );

        Transform bestTarget =
            null;

        float bestDistance =
            Mathf.Infinity;

        for (int i = 0;
             i < summons.Length;
             i++)
        {
            SummonUnitBase summon =
                summons[i]
                    .GetComponentInParent<SummonUnitBase>();

            if (summon == null ||
                summon.IsDead)
            {
                continue;
            }

            float distance =
                Vector2.SqrMagnitude(
                    (Vector2)summon.transform.position -
                    rb.position
                );

            if (distance <
                bestDistance)
            {
                bestDistance =
                    distance;

                bestTarget =
                    summon.transform;
            }
        }

        return bestTarget;
    }

    private Transform FindHighestDefenseSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                rb.position,
                detectionRange,
                summonLayer
            );

        SummonUnitBase bestSummon =
            null;

        float highestDefense =
            float.MinValue;

        for (int i = 0;
             i < summons.Length;
             i++)
        {
            SummonUnitBase summon =
                summons[i]
                    .GetComponentInParent<SummonUnitBase>();

            if (summon == null ||
                summon.IsDead)
            {
                continue;
            }

            if (summon.Defense >
                highestDefense)
            {
                highestDefense =
                    summon.Defense;

                bestSummon =
                    summon;
            }
        }

        return bestSummon != null
            ? bestSummon.transform
            : null;
    }

    private Transform FindHighestMaxHealthSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                rb.position,
                detectionRange,
                summonLayer
            );

        SummonUnitBase bestSummon =
            null;

        float highestHealth =
            float.MinValue;

        for (int i = 0;
             i < summons.Length;
             i++)
        {
            SummonUnitBase summon =
                summons[i]
                    .GetComponentInParent<SummonUnitBase>();

            if (summon == null ||
                summon.IsDead)
            {
                continue;
            }

            if (summon.MaxHealth >
                highestHealth)
            {
                highestHealth =
                    summon.MaxHealth;

                bestSummon =
                    summon;
            }
        }

        return bestSummon != null
            ? bestSummon.transform
            : null;
    }

    private Transform FindFarthestSummon()
    {
        Collider2D[] summons =
            Physics2D.OverlapCircleAll(
                rb.position,
                detectionRange,
                summonLayer
            );

        Transform bestTarget =
            null;

        float farthestDistance =
            -1f;

        for (int i = 0;
             i < summons.Length;
             i++)
        {
            SummonUnitBase summon =
                summons[i]
                    .GetComponentInParent<SummonUnitBase>();

            if (summon == null ||
                summon.IsDead)
            {
                continue;
            }

            float distance =
                Vector2.SqrMagnitude(
                    (Vector2)summon.transform.position -
                    rb.position
                );

            if (distance >
                farthestDistance)
            {
                farthestDistance =
                    distance;

                bestTarget =
                    summon.transform;
            }
        }

        return bestTarget;
    }

    // =========================================================
    // 이동 / 공격
    // =========================================================

    private void UpdateMovement()
    {
        if (targetType ==
            TargetType.Escape)
        {
            ExitAttackMode();

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
            GetTargetDistance();

        // =====================================================
        // 이미 공격 모드
        // =====================================================

        if (isAttackMode)
        {
            // 공격 중에는 매 FixedUpdate마다
            // 물리적으로 위치를 다시 잠금
            LockAttackPosition();

            // Keep Range 안이면
            // 절대로 MovePosition 호출하지 않음
            if (distance <=
                attackKeepRange)
            {
                TryAttack();
                return;
            }

            // 상대가 확실히 멀어졌음
            ExitAttackMode();
        }

        // =====================================================
        // 공격 시작
        // =====================================================

        if (distance <=
            attackRange)
        {
            EnterAttackMode();

            TryAttack();
            return;
        }

        // =====================================================
        // 사거리 밖일 때만 이동
        // =====================================================

        UnlockAttackPosition();

        Vector2 nextPosition =
            Vector2.MoveTowards(
                rb.position,
                currentTarget.position,
                moveSpeed *
                Time.fixedDeltaTime
            );

        rb.MovePosition(
            nextPosition
        );
    }

    // =========================================================
    // 공격
    // =========================================================

    private void TryAttack()
    {
        if (!isAttackMode ||
            attackTimer > 0f ||
            currentTarget == null)
        {
            return;
        }

        // 공격 중에는 반드시 고정
        LockAttackPosition();

        SummonUnitBase summon =
            currentTarget
                .GetComponent<SummonUnitBase>();

        if (summon == null)
        {
            summon =
                currentTarget
                    .GetComponentInChildren<SummonUnitBase>();
        }

        if (summon != null)
        {
            summon.TakeDamage(
                attackDamage
            );

            attackTimer =
                attackInterval;

            return;
        }

        RibelHealth health =
            currentTarget
                .GetComponent<RibelHealth>();

        if (health == null)
        {
            health =
                currentTarget
                    .GetComponentInChildren<RibelHealth>();
        }

        if (health != null)
        {
            health.TakeDamage(
                attackDamage
            );

            attackTimer =
                attackInterval;
        }
    }

    // =========================================================
    // 타이머
    // =========================================================

    private void UpdateTimers()
    {
        if (attackTimer > 0f)
        {
            attackTimer -=
                Time.deltaTime;

            if (attackTimer < 0f)
            {
                attackTimer =
                    0f;
            }
        }
    }

    // =========================================================
    // 정지
    // =========================================================

    private void StopMovement()
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(
                1f,
                maxHealth
            );

        manaReward =
            Mathf.Max(
                0f,
                manaReward
            );

        moveSpeed =
            Mathf.Max(
                0f,
                moveSpeed
            );

        attackDamage =
            Mathf.Max(
                0f,
                attackDamage
            );

        attackRange =
            Mathf.Max(
                0.01f,
                attackRange
            );

        attackKeepRange =
            Mathf.Max(
                attackRange,
                attackKeepRange
            );

        attackInterval =
            Mathf.Max(
                0.05f,
                attackInterval
            );

        detectionRange =
            Mathf.Max(
                0.1f,
                detectionRange
            );

        disengageRange =
            Mathf.Max(
                detectionRange,
                disengageRange
            );

        searchInterval =
            Mathf.Max(
                0.05f,
                searchInterval
            );
    }
#endif
}