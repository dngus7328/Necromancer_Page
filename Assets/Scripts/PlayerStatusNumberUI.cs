using TMPro;
using UnityEngine;

public class PlayerStatusNumberUI : MonoBehaviour
{
    [Header("리벨 체력")]
    [SerializeField] private RibelHealth ribelHealth;

    [Header("소환 / 마나 시스템")]
    [SerializeField] private SummonManager summonManager;

    [Header("숫자 텍스트")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text manaText;

    [Header("표시 방식")]
    [Tooltip("체력을 '현재 / 최대' 형식으로 표시")]
    [SerializeField] private bool showMaxHealth = true;

    [Tooltip("마나를 '현재 / 최대' 형식으로 표시")]
    [SerializeField] private bool showMaxMana = true;

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        UpdateTexts();
    }

    private void Update()
    {
        UpdateTexts();
    }

    // =========================================================
    // 참조 자동 탐색
    // =========================================================

    private void FindReferences()
    {
        if (ribelHealth == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                ribelHealth =
                    player.GetComponent<RibelHealth>();
            }
        }

        if (summonManager == null)
        {
            summonManager =
                SummonManager.Instance;
        }
    }

    // =========================================================
    // 텍스트 갱신
    // =========================================================

    private void UpdateTexts()
    {
        UpdateHealthText();
        UpdateManaText();
    }

    // =========================================================
    // 체력
    // =========================================================

    private void UpdateHealthText()
    {
        if (hpText == null ||
            ribelHealth == null)
        {
            return;
        }

        int current =
            Mathf.CeilToInt(
                ribelHealth.CurrentHealth
            );

        int max =
            Mathf.CeilToInt(
                ribelHealth.MaxHealth
            );

        if (showMaxHealth)
        {
            hpText.text =
                current + " / " + max;
        }
        else
        {
            hpText.text =
                current.ToString();
        }
    }

    // =========================================================
    // 마나
    // =========================================================

    private void UpdateManaText()
    {
        if (manaText == null)
        {
            return;
        }

        if (summonManager == null)
        {
            summonManager =
                SummonManager.Instance;

            if (summonManager == null)
            {
                return;
            }
        }

        int current =
            Mathf.CeilToInt(
                summonManager.CurrentMana
            );

        int max =
            Mathf.CeilToInt(
                summonManager.MaxMana
            );

        if (showMaxMana)
        {
            manaText.text =
                current + " / " + max;
        }
        else
        {
            manaText.text =
                current.ToString();
        }
    }
}