using System.Collections;
using UnityEngine;

public class RibelHealth : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private float maxHealth = 100f;

    [Header("피격 연출")]
    [Tooltip("피격 직후 아주 짧게 뜸을 들이는 시간")]
    [SerializeField] private float hitPauseTime = 0.08f;

    [Tooltip("붉은 색으로 유지되는 시간")]
    [SerializeField] private float redFlashTime = 0.12f;

    [Tooltip("피격 후 전체 무적 시간")]
    [SerializeField] private float invincibleTime = 0.8f;

    [Header("피격 색상")]
    [SerializeField]
    private Color hitColor =
        new Color(1f, 0.2f, 0.2f, 1f);

    [Header("무적 표시")]
    [Tooltip("무적 중 깜빡이는 간격")]
    [SerializeField] private float blinkInterval = 0.08f;

    [Tooltip("깜빡일 때 투명도")]
    [Range(0f, 1f)]
    [SerializeField] private float blinkAlpha = 0.35f;

    private float currentHealth;

    private bool isInvincible;
    private bool isDead;

    private SpriteRenderer spriteRenderer;
    private RibelController ribelController;

    private Color originalColor;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;

    private void Awake()
    {
        currentHealth = maxHealth;

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        ribelController =
            GetComponent<RibelController>();

        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }
    }

    // =========================================================
    // 피해
    // =========================================================

    public void TakeDamage(float damage)
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

        currentHealth -= damage;

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        Debug.Log(
            $"리벨 피해 {damage} | HP {currentHealth}/{maxHealth}"
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
        isInvincible = true;

        // -----------------------------------------
        // 1. 피격 모션
        // -----------------------------------------

        if (ribelController != null)
        {
            ribelController.PlayHitMotion();
        }

        // -----------------------------------------
        // 2. 잠깐 뜸
        // -----------------------------------------

        if (hitPauseTime > 0f)
        {
            yield return new WaitForSeconds(
                hitPauseTime
            );
        }

        // -----------------------------------------
        // 3. 붉게 표시
        // -----------------------------------------

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                hitColor;
        }

        if (redFlashTime > 0f)
        {
            yield return new WaitForSeconds(
                redFlashTime
            );
        }

        // -----------------------------------------
        // 4. 원래 색 복귀
        // -----------------------------------------

        RestoreOriginalColor();

        // -----------------------------------------
        // 5. 남은 무적 시간 동안 깜빡임
        // -----------------------------------------

        float remainingInvincibleTime =
            invincibleTime -
            hitPauseTime -
            redFlashTime;

        if (remainingInvincibleTime > 0f)
        {
            yield return StartCoroutine(
                BlinkRoutine(
                    remainingInvincibleTime
                )
            );
        }

        // -----------------------------------------
        // 6. 무적 종료
        // -----------------------------------------

        RestoreOriginalColor();

        isInvincible = false;
    }

    // =========================================================
    // 무적 깜빡임
    // =========================================================

    private IEnumerator BlinkRoutine(
        float duration)
    {
        if (spriteRenderer == null)
        {
            yield return new WaitForSeconds(
                duration
            );

            yield break;
        }

        float elapsed = 0f;

        bool transparent = false;

        while (elapsed < duration)
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
                    duration - elapsed
                );

            if (waitTime <= 0f)
            {
                break;
            }

            yield return new WaitForSeconds(
                waitTime
            );

            elapsed += waitTime;
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

    public void Heal(float amount)
    {
        if (amount <= 0f ||
            isDead)
        {
            return;
        }

        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
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

        isDead = true;
        isInvincible = true;

        currentHealth = 0f;

        StopAllCoroutines();

        RestoreOriginalColor();

        if (ribelController != null)
        {
            ribelController.PlayDeathMotion();
        }

        Debug.Log("리벨 사망");

        // 게임오버는 나중에 연결
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