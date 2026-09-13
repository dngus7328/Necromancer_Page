using UnityEngine;

public static class AugmentRuntimeEffects
{
    private static float summonMaxHealthMultiplier = 1f;
    private static float summonDamageMultiplier = 1f;
    private static float summonCooldownMultiplier = 1f;
    private static float summonManaCostMultiplier = 1f;

    public static float SummonMaxHealthMultiplier => summonMaxHealthMultiplier;
    public static float SummonDamageMultiplier => summonDamageMultiplier;
    public static float SummonCooldownMultiplier => summonCooldownMultiplier;
    public static float SummonManaCostMultiplier => summonManaCostMultiplier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        ResetRun();
    }

    public static void ResetRun()
    {
        summonMaxHealthMultiplier = 1f;
        summonDamageMultiplier = 1f;
        summonCooldownMultiplier = 1f;
        summonManaCostMultiplier = 1f;
    }

    public static void ApplyContract(AugmentContract contract)
    {
        if (contract == null)
            return;

        switch (contract.id)
        {
            case "C_T1_01":
                summonMaxHealthMultiplier *= 1.25f;
                RefreshExistingSummons();
                break;

            case "C_T1_02":
                summonDamageMultiplier *= 1.20f;
                RefreshExistingSummons();
                break;

            case "C_T1_03":
                summonCooldownMultiplier *= 0.80f;
                RefreshSummonManagerUI();
                break;

            case "C_T1_04":
                summonManaCostMultiplier *= 0.80f;
                RefreshSummonManagerUI();
                break;

            default:
                Debug.Log(
                    $"[강화계약] {contract.contractName}은 아직 실제 효과 연결 전입니다."
                );
                return;
        }

        Debug.Log(
            $"[강화계약 실제 적용] {contract.contractName}"
        );
    }

    public static float GetSummonMaxHealth(float baseValue)
    {
        return Mathf.Max(1f, baseValue * summonMaxHealthMultiplier);
    }

    public static float GetSummonDamage(float baseValue)
    {
        return Mathf.Max(0f, baseValue * summonDamageMultiplier);
    }

    public static float GetManaCost(float baseManaCost)
    {
        return Mathf.Max(0f, baseManaCost * summonManaCostMultiplier);
    }

    public static float GetCooldown(float baseCooldown)
    {
        return Mathf.Max(0.05f, baseCooldown * summonCooldownMultiplier);
    }

    private static void RefreshExistingSummons()
    {
        SummonUnitBase[] summons =
            Object.FindObjectsOfType<SummonUnitBase>();

        for (int i = 0; i < summons.Length; i++)
        {
            if (summons[i] != null)
                summons[i].RefreshAugmentStats();
        }
    }

    private static void RefreshSummonManagerUI()
    {
        if (SummonManager.Instance != null)
            SummonManager.Instance.RefreshAugmentAffectedUI();
    }
}
