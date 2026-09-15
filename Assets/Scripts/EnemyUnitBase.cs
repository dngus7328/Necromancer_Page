using System.Collections.Generic;
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

    // =========================================================
    // 기본 능력치
    // =========================================================

    [Header("기본 능력치")]

    [SerializeField]
    private float maxHealth = 50f;

    [Header("처치 보상")]

    [SerializeField]
    private float manaReward = 5f;

    // =========================================================
    // 이동
    // =========================================================

    [Header("이동")]

    [SerializeField]
    private float moveSpeed = 3f;

    // =========================================================
    // 공격
    // =========================================================

    [Header("공격")]

    [SerializeField]
    private float attackDamage = 10f;

    [Tooltip(
        "적과 대상의 Collider 표면 사이가 " +
        "이 거리 이하가 되면 공격을 시작합니다."
    )]
    [SerializeField]
    private float attackRange = 0.15f;

    [Tooltip(
        "공격 시작 후 대상이 이 거리 안에 있으면 " +
        "공격 상태를 유지합니다."
    )]
    [SerializeField]
    private float attackKeepRange = 0.35f;

    [Tooltip("한 번 공격한 뒤 다음 공격까지의 시간")]
    [SerializeField]
    private float attackInterval = 1.2f;

    // =========================================================
    // 접촉 피해
    // =========================================================

    [Header("접촉 피해")]

    [Tooltip(
        "켜면 적의 본체 Collider가 리벨의 Collider와 닿았을 때 " +
        "약한 접촉 피해를 줍니다."
    )]
    [SerializeField]
    private bool useContactDamage = true;

    [SerializeField]
    private float contactDamage = 3f;

    [Tooltip(
        "계속 겹쳐 있어도 이 시간마다 한 번만 접촉 피해를 시도합니다. " +
        "리벨의 피격 무적 시간도 그대로 적용됩니다."
    )]
    [SerializeField]
    private float contactDamageInterval = 0.6f;

    // =========================================================
    // 실제 근접 공격 히트박스
    // =========================================================

    [Header("근접 공격 히트박스")]

    [Tooltip(
        "공격 프레임 순간 실제로 피해를 주는 범위입니다.\n\n" +
        "X = 공격 방향으로의 길이\n" +
        "Y = 공격 범위의 폭"
    )]
    [SerializeField]
    private Vector2 attackHitboxSize =
        new Vector2(1.0f, 0.8f);

    [Tooltip(
        "적 중심에서 공격 방향 쪽으로 " +
        "히트박스를 얼마나 앞에 배치할지 정합니다."
    )]
    [SerializeField]
    private float attackHitboxForwardOffset = 0.55f;

    [Tooltip(
        "공격 판정에 검사할 Layer입니다.\n" +
        "Everything으로 두어도 코드에서 리벨과 소환수만 피해를 받습니다."
    )]
    [SerializeField]
    private LayerMask attackTargetMask = ~0;

    // =========================================================
    // 타깃
    // =========================================================

    [Header("타깃 설정")]

    [SerializeField]
    private TargetType targetType =
        TargetType.Nearest;

    [SerializeField]
    private LayerMask summonLayer;

    [SerializeField]
    private float detectionRange = 7f;

    [SerializeField]
    private float disengageRange = 10f;

    [SerializeField]
    private float searchInterval = 0.2f;

    // =========================================================
    // 비주얼
    // =========================================================

    [Header("비주얼")]

    [SerializeField]
    private EnemyVisualController visualController;

    // =========================================================
    // 디버그 표시
    // =========================================================

    [Header("디버그 표시")]

    [Tooltip(
        "Scene 뷰에서 실제 근접 공격 히트박스를 표시합니다."
    )]
    [SerializeField]
    private bool showAttackHitbox = true;

    [Tooltip(
        "켜면 이 적을 선택하지 않아도 " +
        "Scene 뷰에서 공격 히트박스가 계속 표시됩니다."
    )]
    [SerializeField]
    private bool alwaysShowAttackHitbox = false;

    [Tooltip(
        "공격을 시작하는 거리도 Scene 뷰에 표시합니다."
    )]
    [SerializeField]
    private bool showAttackStartRange = false;

    [Tooltip(
        "소환수를 탐지하는 범위를 Scene 뷰에 표시합니다."
    )]
    [SerializeField]
    private bool showDetectionRange = false;

    // =========================================================
    // Runtime
    // =========================================================

    private Rigidbody2D rb;

    private Transform ribel;
    private Transform currentTarget;

    private Collider2D bodyCollider;

    private SpriteRenderer bodySpriteRenderer;

    private RibelHealth ribelHealth;
    private Collider2D ribelCollider;

    private float currentHealth;

    private float attackTimer;
    private float searchTimer;
    private float contactDamageTimer;

    private bool isDead;

    private bool isAttackMode;
    private bool isAttacking;

    private RigidbodyConstraints2D normalConstraints;

    // 공격을 시작했을 때의 실제 공격 방향
    private Vector2 attackDirection =
        Vector2.down;

    // =========================================================
    // Property
    // =========================================================

    public float CurrentHealth =>
        currentHealth;

    public float MaxHealth =>
        maxHealth;

    public bool IsDead =>
        isDead;

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

        // 본체에 Collider가 없다면
        // 자식에서 Trigger가 아닌 Collider를 찾음
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

        if (visualController == null)
        {
            visualController =
                GetComponentInChildren<EnemyVisualController>();
        }

        if (visualController != null)
        {
            bodySpriteRenderer =
                visualController
                    .GetComponentInChildren<SpriteRenderer>();
        }

        if (bodySpriteRenderer == null)
        {
            bodySpriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
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

        UpdateContactDamage();

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

    private void LateUpdate()
    {
        UpdateYSorting();
    }

    // =========================================================
    // Collider 바닥 기준 앞뒤 정렬
    // =========================================================

    private void UpdateYSorting()
    {
        if (bodySpriteRenderer == null ||
            bodyCollider == null ||
            !bodyCollider.enabled)
        {
            return;
        }

        float bottomY =
            bodyCollider.bounds.min.y;

        bodySpriteRenderer.sortingOrder =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    -bottomY * 100f
                ),
                -32000,
                32000
            );
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
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (ribelObject != null)
        {
            ribel =
                ribelObject.transform;

            ribelHealth =
                ribelObject.GetComponent<RibelHealth>();

            if (ribelHealth == null)
            {
                ribelHealth =
                    ribelObject
                        .GetComponentInChildren<RibelHealth>();
            }

            ribelCollider =
                FindNonTriggerCollider(
                    ribelObject.transform
                );
        }
    }

    private Collider2D FindNonTriggerCollider(
        Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Collider2D collider =
            root.GetComponent<Collider2D>();

        if (collider != null &&
            !collider.isTrigger)
        {
            return collider;
        }

        Collider2D[] colliders =
            root.GetComponentsInChildren<Collider2D>();

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            if (colliders[i] != null &&
                !colliders[i].isTrigger)
            {
                return colliders[i];
            }
        }

        return null;
    }

    // =========================================================
    // 리벨 접촉 피해
    // =========================================================

    private void UpdateContactDamage()
    {
        if (!useContactDamage ||
            contactDamage <= 0f ||
            bodyCollider == null ||
            !bodyCollider.enabled)
        {
            return;
        }

        if (contactDamageTimer > 0f)
        {
            contactDamageTimer -=
                Time.deltaTime;

            if (contactDamageTimer < 0f)
            {
                contactDamageTimer =
                    0f;
            }
        }

        if (ribel == null ||
            ribelHealth == null ||
            ribelCollider == null)
        {
            FindRibel();
        }

        if (ribelHealth == null ||
            ribelCollider == null ||
            !ribelCollider.enabled ||
            contactDamageTimer > 0f)
        {
            return;
        }

        ColliderDistance2D result =
            bodyCollider.Distance(
                ribelCollider
            );

        bool isTouching =
            result.isOverlapped ||
            result.distance <= 0.001f;

        if (!isTouching)
        {
            return;
        }

        ribelHealth.TakeDamage(
            contactDamage
        );

        contactDamageTimer =
            contactDamageInterval;
    }

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
        isAttackMode =
            false;

        UnlockAttackPosition();
    }

    // =========================================================
    // 공격 중 위치 고정
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

        if (visualController != null)
        {
            visualController.SetMoving(
                false
            );
        }
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

        isAttacking =
            false;

        ExitAttackMode();

        StopMovement();

        DisableColliders();

        if (SummonManager.Instance != null)
        {
            SummonManager.Instance.AddMana(
                manaReward
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

    private void DisableColliders()
    {
        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>();

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled =
                    false;
            }
        }
    }

    // =========================================================
    // 타깃까지의 실제 Collider 거리
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
    // 타깃 갱신
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

        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            SetRibelAsTarget();

            return;
        }

        // 공격 중에는 타깃 변경 금지
        if (isAttackMode ||
            isAttacking)
        {
            float centerDistance =
                Vector2.Distance(
                    rb.position,
                    currentTarget.position
                );

            if (centerDistance >
                disengageRange)
            {
                isAttacking =
                    false;

                ExitAttackMode();

                SetRibelAsTarget();
            }

            return;
        }

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
    // 선호 타깃 찾기
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

    // =========================================================
    // 방어력이 가장 높은 소환수
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
    // 최대 체력이 가장 높은 소환수
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

        if (isAttacking)
        {
            LockAttackPosition();

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

        Vector2 toTarget =
            (Vector2)currentTarget.position -
            rb.position;

        float distance =
            GetTargetDistance();

        // =====================================================
        // 이미 공격 모드인 경우
        // =====================================================

        if (isAttackMode)
        {
            LockAttackPosition();

            if (visualController != null &&
                toTarget.sqrMagnitude >
                0.0001f)
            {
                visualController
                    .SetCombatFacingDirection(
                        toTarget
                    );
            }

            if (distance <=
                attackKeepRange)
            {
                TryAttack();

                return;
            }

            ExitAttackMode();
        }

        // =====================================================
        // 공격 시작 거리 진입
        // =====================================================

        if (distance <=
            attackRange)
        {
            EnterAttackMode();

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

        // =====================================================
        // 추적 이동
        // =====================================================

        UnlockAttackPosition();

        if (visualController != null &&
            toTarget.sqrMagnitude >
            0.0001f)
        {
            visualController.SetFacingDirection(
                toTarget
            );

            visualController.SetMoving(
                true
            );
        }

        float moveStep =
            moveSpeed *
            Time.fixedDeltaTime;

        // Collider 표면 기준 공격 거리보다 안쪽으로
        // 한 프레임에 지나쳐 들어가지 않도록 이동량 제한
        float allowedMove =
            Mathf.Max(
                0f,
                distance -
                attackRange
            );

        moveStep =
            Mathf.Min(
                moveStep,
                allowedMove
            );

        if (moveStep <= 0.0001f ||
            toTarget.sqrMagnitude <= 0.0001f)
        {
            StopMovement();

            return;
        }

        Vector2 nextPosition =
            rb.position +
            toTarget.normalized *
            moveStep;

        rb.MovePosition(
            nextPosition
        );
    }

    // =========================================================
    // 공격 시작
    // =========================================================

    private void TryAttack()
    {
        if (!isAttackMode ||
            isAttacking ||
            attackTimer > 0f ||
            currentTarget == null)
        {
            return;
        }

        Vector2 toTarget =
            (Vector2)currentTarget.position -
            rb.position;

        // 공격 시작 시점의 방향을 저장
        if (toTarget.sqrMagnitude >
            0.0001f)
        {
            attackDirection =
                toTarget.normalized;
        }

        if (visualController != null)
        {
            visualController
                .SetCombatFacingDirection(
                    attackDirection
                );
        }

        LockAttackPosition();

        isAttacking =
            true;

        attackTimer =
            attackInterval;

        // 공격 애니메이션의 실제 타격 프레임에서
        // ApplyAttackHitbox 호출
        if (visualController != null)
        {
            visualController.PlayAttack(
                ApplyAttackHitbox,
                FinishAttack
            );
        }
        else
        {
            ApplyAttackHitbox();

            FinishAttack();
        }
    }

    // =========================================================
    // 실제 공격 히트박스 생성
    // =========================================================

    private void ApplyAttackHitbox()
    {
        if (isDead)
        {
            return;
        }

        if (attackDirection.sqrMagnitude <
            0.0001f)
        {
            attackDirection =
                Vector2.down;
        }

        Vector2 direction =
            attackDirection.normalized;

        // 적 중심에서 공격 방향 앞으로 이동
        Vector2 hitboxCenter =
            rb.position +
            direction *
            attackHitboxForwardOffset;

        // 오른쪽 방향을 0도로 두고 회전
        float hitboxAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                hitboxCenter,
                attackHitboxSize,
                hitboxAngle,
                attackTargetMask
            );

        // Collider가 여러 개인 캐릭터가
        // 여러 번 피해 받는 것을 방지
        HashSet<int> damagedSummons =
            new HashSet<int>();

        HashSet<int> damagedRibels =
            new HashSet<int>();

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider2D hit =
                hits[i];

            if (hit == null)
            {
                continue;
            }

            // 자기 자신 무시
            if (hit.transform ==
                    transform ||
                hit.transform.IsChildOf(
                    transform
                ))
            {
                continue;
            }

            // =================================================
            // 소환수 판정
            // =================================================

            SummonUnitBase summon =
                hit.GetComponentInParent<SummonUnitBase>();

            if (summon != null &&
                !summon.IsDead)
            {
                int id =
                    summon.GetInstanceID();

                if (!damagedSummons.Contains(
                        id))
                {
                    damagedSummons.Add(
                        id
                    );

                    summon.TakeDamage(
                        attackDamage
                    );
                }

                continue;
            }

            // =================================================
            // 리벨 판정
            // =================================================

            RibelHealth ribelHealth =
                hit.GetComponentInParent<RibelHealth>();

            if (ribelHealth != null)
            {
                int id =
                    ribelHealth.GetInstanceID();

                if (!damagedRibels.Contains(
                        id))
                {
                    damagedRibels.Add(
                        id
                    );

                    ribelHealth.TakeDamage(
                        attackDamage
                    );
                }
            }
        }
    }

    // =========================================================
    // 공격 종료
    // =========================================================

    private void FinishAttack()
    {
        if (isDead)
        {
            return;
        }

        isAttacking =
            false;

        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            ExitAttackMode();

            SetRibelAsTarget();

            return;
        }

        if (GetTargetDistance() >
            attackKeepRange)
        {
            ExitAttackMode();
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

            attackTimer =
                Mathf.Max(
                    0f,
                    attackTimer
                );
        }
    }

    // =========================================================
    // 이동 정지
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

        if (visualController != null)
        {
            visualController.SetMoving(
                false
            );
        }
    }

    // =========================================================
    // Scene 뷰 디버그
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!alwaysShowAttackHitbox)
        {
            return;
        }

        DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (alwaysShowAttackHitbox)
        {
            return;
        }

        DrawDebugGizmos();
    }

    // =========================================================
    // 디버그 범위 그리기
    // =========================================================

    private void DrawDebugGizmos()
    {
        // =====================================================
        // 실제 공격 히트박스
        // =====================================================

        if (showAttackHitbox)
        {
            DrawAttackHitboxGizmo();
        }

        // =====================================================
        // 공격 시작 거리
        // =====================================================

        if (showAttackStartRange)
        {
            Gizmos.color =
                new Color(
                    1f,
                    0.7f,
                    0.1f,
                    0.8f
                );

            Gizmos.DrawWireSphere(
                transform.position,
                attackRange
            );
        }

        // =====================================================
        // 탐지 범위
        // =====================================================

        if (showDetectionRange)
        {
            Gizmos.color =
                new Color(
                    0.2f,
                    0.7f,
                    1f,
                    0.5f
                );

            Gizmos.DrawWireSphere(
                transform.position,
                detectionRange
            );
        }
    }

    // =========================================================
    // 실제 공격 히트박스 Gizmo
    // =========================================================

    private void DrawAttackHitboxGizmo()
    {
        Vector2 direction =
            attackDirection;

        // 플레이하지 않을 때는
        // 기본적으로 오른쪽 공격 방향으로 미리보기
        if (!Application.isPlaying ||
            direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                Vector2.right;
        }

        direction.Normalize();

        Vector2 center =
            (Vector2)transform.position +
            direction *
            attackHitboxForwardOffset;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Gizmos.color =
            new Color(
                1f,
                0.15f,
                0.15f,
                0.9f
            );

        Gizmos.matrix =
            Matrix4x4.TRS(
                center,
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                ),
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            attackHitboxSize
        );

        Gizmos.matrix =
            previousMatrix;
    }

#if UNITY_EDITOR

    // =========================================================
    // Inspector 값 보호
    // =========================================================

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

        contactDamage =
            Mathf.Max(
                0f,
                contactDamage
            );

        contactDamageInterval =
            Mathf.Max(
                0.05f,
                contactDamageInterval
            );

        attackHitboxSize.x =
            Mathf.Max(
                0.01f,
                attackHitboxSize.x
            );

        attackHitboxSize.y =
            Mathf.Max(
                0.01f,
                attackHitboxSize.y
            );

        attackHitboxForwardOffset =
            Mathf.Max(
                0f,
                attackHitboxForwardOffset
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