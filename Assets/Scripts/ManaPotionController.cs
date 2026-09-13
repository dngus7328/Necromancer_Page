using TMPro;
using UnityEngine;

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
    // 쿨타임 숫자 UI
    // =========================================================

    [Header("쿨타임 숫자 UI")]

    [Tooltip("남은 쿨타임 숫자를 표시할 TMP 텍스트")]
    [SerializeField]
    private TMP_Text cooldownText;

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
        RefreshCooldownText();
    }

    private void Update()
    {
        UpdateCooldown();

        HandleInput();

        RefreshCooldownText();
    }

    // =========================================================
    // 입력
    // =========================================================

    private void HandleInput()
    {
        if (!Input.GetKeyDown(useKey))
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

        RefreshCooldownText();

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
    // 쿨타임 숫자 표시
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

        // -----------------------------------------------------
        // 1초 초과
        // 정수로 표시
        //
        // 예:
        // 17.6 → 18
        // 5.2  → 6
        // 1.1  → 2
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 1초 이하
        // 소수점 한 자리로 표시
        //
        // 0.94 → 0.9
        // 0.52 → 0.5
        // 0.14 → 0.1
        //
        // 0.0은 표시하지 않고
        // 쿨타임 종료와 동시에 사라짐
        // -----------------------------------------------------

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