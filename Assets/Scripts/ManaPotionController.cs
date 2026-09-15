using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManaPotionController : MonoBehaviour
{
    // =========================================================
    // 마나 포션
    // =========================================================

    [Header("마나 포션")]

    [Tooltip("포션 1회 사용 시 회복되는 마나")]
    [SerializeField]
    private float restoreAmount = 40f;

    [Tooltip("포션 재사용 대기시간")]
    [SerializeField]
    private float cooldown = 18f;

    [Tooltip("포션 사용 키")]
    [SerializeField]
    private KeyCode useKey = KeyCode.E;

    // =========================================================
    // 쿨타임 UI
    // =========================================================

    [Header("쿨타임 UI")]

    [Tooltip("남은 쿨타임 숫자를 표시할 TMP 텍스트")]
    [SerializeField]
    private TMP_Text cooldownText;

    [Tooltip(
        "포션 위를 덮는 쿨타임 이미지. " +
        "Image Type을 Filled / Radial 360으로 설정합니다."
    )]
    [SerializeField]
    private Image cooldownFill;

    // =========================================================
    // Runtime
    // =========================================================

    private float remainingCooldown;

    // =========================================================
    // 외부 확인
    // =========================================================

    public float RemainingCooldown =>
        Mathf.Max(
            0f,
            remainingCooldown
        );

    public float Cooldown =>
        cooldown;

    public bool IsReady =>
        remainingCooldown <= 0f;

    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        RefreshCooldownUI();
    }

    private void Update()
    {
        UpdateCooldown();

        HandleInput();

        RefreshCooldownUI();
    }

    // =========================================================
    // 입력
    // =========================================================

    private void HandleInput()
    {
        if (!Input.GetKeyDown(
                useKey
            ))
        {
            return;
        }

        TryUsePotion();
    }

    // =========================================================
    // 포션 사용
    // =========================================================

    public bool TryUsePotion()
    {
        if (!IsReady)
        {
            return false;
        }

        if (SummonManager.Instance == null)
        {
            return false;
        }

        SummonManager.Instance.AddMana(
            restoreAmount
        );

        remainingCooldown =
            cooldown;

        RefreshCooldownUI();

        Debug.Log(
            $"마나 포션 사용 | +{restoreAmount} Mana"
        );

        return true;
    }

    // =========================================================
    // 쿨타임
    // =========================================================

    private void UpdateCooldown()
    {
        if (remainingCooldown <= 0f)
        {
            remainingCooldown =
                0f;

            return;
        }

        remainingCooldown -=
            Time.deltaTime;

        if (remainingCooldown <= 0f)
        {
            remainingCooldown =
                0f;
        }
    }

    // =========================================================
    // UI 전체 갱신
    // =========================================================

    private void RefreshCooldownUI()
    {
        RefreshCooldownFill();
        RefreshCooldownText();
    }

    // =========================================================
    // 쿨타임 Fill
    // =========================================================

    private void RefreshCooldownFill()
    {
        if (cooldownFill == null)
        {
            return;
        }

        if (IsReady ||
            cooldown <= 0f)
        {
            cooldownFill.fillAmount =
                0f;

            return;
        }

        cooldownFill.fillAmount =
            Mathf.Clamp01(
                remainingCooldown /
                cooldown
            );
    }

    // =========================================================
    // 쿨타임 숫자
    // =========================================================

    private void RefreshCooldownText()
    {
        if (cooldownText == null)
        {
            return;
        }

        // 사용 가능하면 숫자 숨김
        if (IsReady)
        {
            cooldownText.text =
                "";

            return;
        }

        // 1초 초과
        // 정수 올림으로 표시
        //
        // 17.6 → 18
        // 5.2  → 6
        // 1.1  → 2
        if (remainingCooldown > 1f)
        {
            int seconds =
                Mathf.CeilToInt(
                    remainingCooldown
                );

            cooldownText.text =
                seconds.ToString();

            return;
        }

        // 1초 이하
        // 소수점 한 자리 표시
        //
        // 0.94 → 0.9
        // 0.52 → 0.5
        // 0.14 → 0.1
        float displayedTime =
            Mathf.Ceil(
                remainingCooldown *
                10f
            ) /
            10f;

        displayedTime =
            Mathf.Max(
                0.1f,
                displayedTime
            );

        cooldownText.text =
            displayedTime.ToString(
                "0.0"
            );
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        restoreAmount =
            Mathf.Max(
                0f,
                restoreAmount
            );

        cooldown =
            Mathf.Max(
                0.1f,
                cooldown
            );
    }

#endif
}