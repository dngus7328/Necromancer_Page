using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyUnitBase : MonoBehaviour
{
    public enum TargetType
    {
        RibelOnly,
        Nearest,
        SummonOnly,
        HighestDefense,
        HighestMaxHealth,
        Farthest,
        Escape
    }

    // =========================================================
    // 기본 능력치
    // =========================================================

    [Header("기본 능력치")]
    [SerializeField]
    private float maxHealth = 50f;

    [Header("처치 보상")]
    [SerializeField]
    private float manaReward = 5f;

    [Header("이동")]
    [SerializeField]
    private float moveSpeed = 3f;

    // =========================================================
    // 공격
    // =========================================================

    [Header("공격")]
    [SerializeField]
    private float attackDamage = 10f;

    [Tooltip("Collider 표면 사이 거리가 이 값 이하이면 공격 시작")]
    [SerializeField]
    private float attackRange = 0.15f;

    [Tooltip("공격 시작 후 이 거리까지는 움직이지 않고 계속 공격")]
    [SerializeField]
    private float attackKeepRange = 0.35f;

    [SerializeField]
    private float attackInterval = 1.2f;

    // =========================================================
    // 타깃
    // =========================================================

    [Header("타깃 설정")]

    [Tooltip(
        "기본 적은 RibelOnly를 사용합니다. " +
        "다른 값은 특수 타깃 규칙을 가진 적에게만 사용합니다."
    )]
    [SerializeField]
    private TargetType targetType =
        TargetType.RibelOnly;

    [Tooltip("소환수 레이어")]
    [SerializeField]
    private LayerMask summonLayer;

    [Tooltip("특수 적이 소환수를 탐색하는 거리")]
    [SerializeField]
    private float detectionRange = 7f;

    [Tooltip("현재 타깃이 이 거리보다 멀어지면 타깃을 다시 판단")]
    [SerializeField]
    private float disengageRange = 10f;

    [Tooltip("타깃 재검사 간격")]
    [SerializeField]
    private float searchInterval = 0.2f;

    // =========================================================
    // Runtime
    // =========================================================

    private Rigidbody2D rb;

    private Transform ribel;

    private Transform currentTarget;

    private Collider2D bodyCollider;

    private float currentHealth;

    private float attackTimer;

    private float searchTimer;

    private bool isDead;

    private bool isAttackMode;

    private RigidbodyConstraints2D normalConstraints;

    // =========================================================
    // 외부 확인
    // =========================================================

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

        SetInitialTarget();
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

    // =========================================================
    // 초기 타깃
    // =========================================================

    private void SetInitialTarget()
    {
        if (targetType ==
            TargetType.Escape)
        {
            currentTarget =
                null;

            return;
        }

        if (targetType ==
            TargetType.RibelOnly)
        {
            SetRibelAsTarget();

            return;
        }

        Transform preferred =
            FindPreferredTarget();

        if (preferred != null)
        {
            currentTarget =
                preferred;
        }
        else
        {
            SetRibelAsTarget();
        }
    }

    // =========================================================
    // 리벨 타깃
    // =========================================================

    private void SetRibelAsTarget()
    {
        ExitAttackMode();

        if (ribel == null)
        {
            FindRibel();
        }

        currentTarget =
            ribel;
    }

    // =========================================================
    // 공격 모드
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
            UnlockAttackPosition();

            return;
        }

        isAttackMode =
            false;

        UnlockAttackPosition();
    }

    // =========================================================
    // 공격 위치 고정
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
            currentHealth =
                0f;

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
    // Collider 표면 거리
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
        // -----------------------------------------------------
        // 도주형
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 리벨이 없는 경우 다시 찾기
        // -----------------------------------------------------

        if (ribel == null)
        {
            FindRibel();
        }

        // -----------------------------------------------------
        // 공격 중에는 타깃 변경 금지
        // -----------------------------------------------------

        if (isAttackMode)
        {
            if (currentTarget == null ||
                !currentTarget.gameObject.activeInHierarchy)
            {
                ExitAttackMode();

                SelectTargetAgain();

                return;
            }

            float centerDistance =
                Vector2.Distance(
                    rb.position,
                    currentTarget.position
                );

            if (centerDistance >
                disengageRange)
            {
                ExitAttackMode();

                SelectTargetAgain();
            }

            return;
        }

        // =====================================================
        // 기본 적
        // 리벨만 추적
        // =====================================================

        if (targetType ==
            TargetType.RibelOnly)
        {
            if (currentTarget != ribel)
            {
                SetRibelAsTarget();
            }

            return;
        }

        // =====================================================
        // 특수 적
        // =====================================================

        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            SelectTargetAgain();

            return;
        }

        SummonUnitBase currentSummon =
            FindSummonComponent(
                currentTarget
            );

        // -----------------------------------------------------
        // 이미 소환수를 잡았다면 유지
        // -----------------------------------------------------

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
                SelectTargetAgain();
            }

            return;
        }

        // -----------------------------------------------------
        // 현재 리벨을 보고 있지만
        // 특수 타깃 조건에 맞는 소환수가 있으면 교체
        // -----------------------------------------------------

        if (currentTarget ==
            ribel)
        {
            Transform preferredTarget =
                FindPreferredTarget();

            if (preferredTarget != null)
            {
                currentTarget =
                    preferredTarget;

                ExitAttackMode();
            }
        }
    }

    // =========================================================
    // 타깃 재선택
    // =========================================================

    private void SelectTargetAgain()
    {
        if (targetType ==
            TargetType.RibelOnly)
        {
            SetRibelAsTarget();

            return;
        }

        Transform preferredTarget =
            FindPreferredTarget();

        if (preferredTarget != null)
        {
            currentTarget =
                preferredTarget;

            return;
        }

        // SummonOnly는 소환수가 없으면
        // 리벨을 공격하지 않음
        if (targetType ==
            TargetType.SummonOnly)
        {
            currentTarget =
                null;

            return;
        }

        SetRibelAsTarget();
    }

    // =========================================================
    // 현재 Transform이 소환수인지 확인
    // =========================================================

    private SummonUnitBase FindSummonComponent(
        Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return null;
        }

        SummonUnitBase summon =
            targetTransform
                .GetComponent<SummonUnitBase>();

        if (summon == null)
        {
            summon =
                targetTransform
                    .GetComponentInChildren<SummonUnitBase>();
        }

        if (summon == null)
        {
            summon =
                targetTransform
                    .GetComponentInParent<SummonUnitBase>();
        }

        return summon;
    }

    // =========================================================
    // 특수 타깃 선택
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
                    (Vector2)summon
                        .transform.position -
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

    // =========================================================
    // 방어력 가장 높은 소환수
    // =========================================================

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

    // =========================================================
    // 최대 HP 가장 높은 소환수
    // =========================================================

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

    // =========================================================
    // 가장 먼 소환수
    // =========================================================

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
                    (Vector2)summon
                        .transform.position -
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
            SelectTargetAgain();

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
            LockAttackPosition();

            if (distance <=
                attackKeepRange)
            {
                TryAttack();

                return;
            }

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
        // 타깃 쪽으로 이동
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

        LockAttackPosition();

        SummonUnitBase summon =
            FindSummonComponent(
                currentTarget
            );

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