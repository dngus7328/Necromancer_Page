using System.Collections;
using UnityEngine;

public class RibelHealth : MonoBehaviour
{
    // =========================================================
    // 체력
    // =========================================================

    [Header("체력")]
    [SerializeField]
    private float maxHealth = 100f;

    // =========================================================
    // 저체력 화면 효과
    // =========================================================

    [Header("저체력 화면 효과")]
    [Tooltip(
        "Canvas의 LowHealthOverlay에 붙어있는 " +
        "LowHealthScreenEffect를 연결합니다. " +
        "비워두면 자동으로 찾습니다."
    )]
    [SerializeField]
    private LowHealthScreenEffect lowHealthScreenEffect;

    // =========================================================
    // 피격 연출
    // =========================================================

    [Header("피격 연출")]

    [Tooltip("피격 직후 아주 짧게 뜸을 들이는 시간")]
    [SerializeField]
    private float hitPauseTime = 0.08f;

    [Tooltip("붉은 색으로 유지되는 시간")]
    [SerializeField]
    private float redFlashTime = 0.12f;

    [Tooltip("피격 후 전체 무적 시간")]
    [SerializeField]
    private float invincibleTime = 0.8f;

    // =========================================================
    // 피격 색상
    // =========================================================

    [Header("피격 색상")]
    [SerializeField]
    private Color hitColor =
        new Color(
            1f,
            0.2f,
            0.2f,
            1f
        );

    // =========================================================
    // 무적 표시
    // =========================================================

    [Header("무적 표시")]

    [Tooltip("무적 중 깜빡이는 간격")]
    [SerializeField]
    private float blinkInterval = 0.08f;

    [Tooltip("깜빡일 때 투명도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float blinkAlpha = 0.35f;

    // =========================================================
    // Runtime
    // =========================================================

    private float currentHealth;

    private bool isInvincible;

    private bool isDead;

    private SpriteRenderer spriteRenderer;

    private RibelController ribelController;

    private Color originalColor;

    // =========================================================
    // 외부 확인
    // =========================================================

    public float CurrentHealth =>
        currentHealth;

    public float MaxHealth =>
        maxHealth;

    public bool IsInvincible =>
        isInvincible;

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
        currentHealth =
            maxHealth;

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        ribelController =
            GetComponent<RibelController>();

        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }

        FindLowHealthScreenEffect();
    }

    private void Start()
    {
        RefreshLowHealthEffect();
    }

    // =========================================================
    // 피해
    // =========================================================

    public void TakeDamage(
        float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        if (isInvincible)
        {
            return;
        }

        currentHealth -=
            damage;

        if (currentHealth < 0f)
        {
            currentHealth =
                0f;
        }

        // 체력이 줄어든 즉시
        // 화면 위험 효과 갱신
        RefreshLowHealthEffect();

        Debug.Log(
            $"리벨 피해 {damage} | " +
            $"HP {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();

            return;
        }

        StartCoroutine(
            HitRoutine()
        );
    }

    // =========================================================
    // 피격 + 무적 연출
    // =========================================================

    private IEnumerator HitRoutine()
    {
        isInvincible =
            true;

        // -----------------------------------------------------
        // 1. 피격 모션
        // -----------------------------------------------------

        if (ribelController != null)
        {
            ribelController
                .PlayHitMotion();
        }

        // -----------------------------------------------------
        // 2. 짧은 피격 텀
        // -----------------------------------------------------

        if (hitPauseTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    hitPauseTime
                );
        }

        // -----------------------------------------------------
        // 3. 붉은 색 표시
        // -----------------------------------------------------

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                hitColor;
        }

        if (redFlashTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    redFlashTime
                );
        }

        // -----------------------------------------------------
        // 4. 원래 색 복귀
        // -----------------------------------------------------

        RestoreOriginalColor();

        // -----------------------------------------------------
        // 5. 남은 무적 시간 동안 깜빡임
        // -----------------------------------------------------

        float remainingInvincibleTime =
            invincibleTime -
            hitPauseTime -
            redFlashTime;

        if (remainingInvincibleTime >
            0f)
        {
            yield return
                StartCoroutine(
                    BlinkRoutine(
                        remainingInvincibleTime
                    )
                );
        }

        // -----------------------------------------------------
        // 6. 무적 종료
        // -----------------------------------------------------

        RestoreOriginalColor();

        isInvincible =
            false;
    }

    // =========================================================
    // 무적 깜빡임
    // =========================================================

    private IEnumerator BlinkRoutine(
        float duration)
    {
        if (spriteRenderer == null)
        {
            yield return
                new WaitForSeconds(
                    duration
                );

            yield break;
        }

        float elapsed =
            0f;

        bool transparent =
            false;

        while (elapsed <
               duration)
        {
            transparent =
                !transparent;

            Color color =
                originalColor;

            color.a =
                transparent
                    ? blinkAlpha
                    : originalColor.a;

            spriteRenderer.color =
                color;

            float waitTime =
                Mathf.Min(
                    blinkInterval,
                    duration -
                    elapsed
                );

            if (waitTime <= 0f)
            {
                break;
            }

            yield return
                new WaitForSeconds(
                    waitTime
                );

            elapsed +=
                waitTime;
        }
    }

    // =========================================================
    // 원래 색 복구
    // =========================================================

    private void RestoreOriginalColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
        }
    }

    // =========================================================
    // 회복
    // =========================================================

    public void Heal(
        float amount)
    {
        if (amount <= 0f ||
            isDead)
        {
            return;
        }

        currentHealth +=
            amount;

        if (currentHealth >
            maxHealth)
        {
            currentHealth =
                maxHealth;
        }

        // 회복 즉시 화면 위험 효과도 감소
        RefreshLowHealthEffect();
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

        isInvincible =
            true;

        currentHealth =
            0f;

        // HP 0 상태를 화면 효과에 전달
        RefreshLowHealthEffect();

        StopAllCoroutines();

        RestoreOriginalColor();

        if (ribelController != null)
        {
            ribelController
                .PlayDeathMotion();
        }

        Debug.Log(
            "리벨 사망"
        );

        // 게임오버는 추후 연결
    }

    // =========================================================
    // 저체력 효과 찾기
    // =========================================================

    private void FindLowHealthScreenEffect()
    {
        if (lowHealthScreenEffect != null)
        {
            return;
        }

        lowHealthScreenEffect =
            FindObjectOfType
                <LowHealthScreenEffect>(
                    true
                );
    }

    // =========================================================
    // 저체력 화면 효과 갱신
    // =========================================================

    private void RefreshLowHealthEffect()
    {
        if (lowHealthScreenEffect == null)
        {
            FindLowHealthScreenEffect();
        }

        if (lowHealthScreenEffect == null)
        {
            return;
        }

        lowHealthScreenEffect.SetHealth(
            currentHealth,
            maxHealth
        );
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(
                1f,
                maxHealth
            );

        hitPauseTime =
            Mathf.Max(
                0f,
                hitPauseTime
            );

        redFlashTime =
            Mathf.Max(
                0f,
                redFlashTime
            );

        invincibleTime =
            Mathf.Max(
                0f,
                invincibleTime
            );

        blinkInterval =
            Mathf.Max(
                0.02f,
                blinkInterval
            );

        blinkAlpha =
            Mathf.Clamp01(
                blinkAlpha
            );
    }

#endif
}