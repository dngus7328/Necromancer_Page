using System;
using System.Collections.Generic;
using UnityEngine;

public static class AugmentRuntimeEffects
{
    private static float summonMaxHealthMultiplier = 1f;
    private static float summonDamageMultiplier = 1f;
    private static float summonCooldownMultiplier = 1f;
    private static float summonManaCostMultiplier = 1f;
    private static float manaRecoveryMultiplier = 1f;

    private static readonly HashSet<string> activeContractIds =
        new HashSet<string>();

    private static readonly Dictionary<string, AugmentContract> activeContracts =
        new Dictionary<string, AugmentContract>();

    public static float SummonMaxHealthMultiplier =>
        summonMaxHealthMultiplier;

    public static float SummonDamageMultiplier =>
        summonDamageMultiplier;

    public static float SummonCooldownMultiplier =>
        summonCooldownMultiplier;

    public static float SummonManaCostMultiplier =>
        summonManaCostMultiplier;

    public static float ManaRecoveryMultiplier =>
        manaRecoveryMultiplier;

    public static int ActiveContractCount =>
        activeContractIds.Count;

    public static event Action<AugmentContract> ContractApplied;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
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
        manaRecoveryMultiplier = 1f;

        activeContractIds.Clear();
        activeContracts.Clear();

        ContractApplied = null;
    }

    public static void ApplyContract(
        AugmentContract contract)
    {
        if (contract == null ||
            string.IsNullOrWhiteSpace(
                contract.id
            ))
        {
            return;
        }

        if (activeContractIds.Contains(
                contract.id
            ))
        {
            return;
        }

        activeContractIds.Add(
            contract.id
        );

        activeContracts[
            contract.id
        ] =
            contract;

        bool refreshStats =
            false;

        bool refreshManager =
            false;

        switch (contract.id)
        {
            case "C_T1_01":
                summonMaxHealthMultiplier *=
                    1.25f;

                refreshStats =
                    true;
                break;

            case "C_T1_02":
                summonDamageMultiplier *=
                    1.20f;

                refreshStats =
                    true;
                break;

            case "C_T1_03":
                summonCooldownMultiplier *=
                    0.80f;

                refreshManager =
                    true;
                break;

            case "C_T1_04":
                summonManaCostMultiplier *=
                    0.80f;

                refreshManager =
                    true;
                break;

            case "C_T3_06":
                manaRecoveryMultiplier *=
                    1.50f;
                break;
        }

        // 전용 계약은 반드시 활성 상태로 저장됩니다.
        // 실제 스탯에 직접 영향을 주는 전용 계약도 여기서 다시 계산합니다.
        if (contract.scope ==
            AugmentScope.SummonExclusive)
        {
            refreshStats =
                true;
        }

        if (refreshStats)
        {
            RefreshAffectedSummons(
                contract
            );
        }

        if (refreshManager)
        {
            RefreshSummonManagerUI();
        }

        ContractApplied?.Invoke(
            contract
        );

        Debug.Log(
            $"[강화계약 실제 등록] {contract.id} / {contract.contractName} / {contract.scope} / 대상:{contract.summonName}"
        );
    }

    public static bool HasContract(
        string contractId)
    {
        if (string.IsNullOrWhiteSpace(
                contractId
            ))
        {
            return false;
        }

        return activeContractIds.Contains(
            contractId
        );
    }

    public static bool HasSummonContract(
        string summonName,
        AugmentTier tier)
    {
        if (string.IsNullOrWhiteSpace(
                summonName
            ))
        {
            return false;
        }

        string safeName =
            summonName
                .Trim()
                .Replace(
                    " ",
                    "_"
                );

        return HasContract(
            $"S_{safeName}_{tier}"
        );
    }

    public static AugmentContract GetActiveContract(
        string contractId)
    {
        if (string.IsNullOrWhiteSpace(
                contractId
            ))
        {
            return null;
        }

        if (activeContracts.TryGetValue(
                contractId,
                out AugmentContract contract
            ))
        {
            return contract;
        }

        return null;
    }

    public static List<AugmentContract> GetActiveContracts()
    {
        return new List<AugmentContract>(
            activeContracts.Values
        );
    }

    public static float GetSummonMaxHealth(
        float baseValue,
        string summonName)
    {
        float multiplier =
            summonMaxHealthMultiplier;

        // 해골병 T1 "닳지 않는 뼈"
        // 문서에 명시된 최대 체력 +25%만 정확히 적용.
        if (IsSummon(
                summonName,
                "해골병"
            ) &&
            HasSummonContract(
                "해골병",
                AugmentTier.T1
            ))
        {
            multiplier *=
                1.25f;
        }

        return Mathf.Max(
            1f,
            baseValue *
            multiplier
        );
    }

    public static float GetSummonMaxHealth(
        float baseValue)
    {
        return GetSummonMaxHealth(
            baseValue,
            string.Empty
        );
    }

    public static float GetSummonDamage(
        float baseValue,
        string summonName)
    {
        return Mathf.Max(
            0f,
            baseValue *
            summonDamageMultiplier
        );
    }

    public static float GetSummonDamage(
        float baseValue)
    {
        return GetSummonDamage(
            baseValue,
            string.Empty
        );
    }

    public static float GetSummonMoveSpeed(
        float baseValue,
        string summonName)
    {
        return Mathf.Max(
            0.1f,
            baseValue
        );
    }

    public static float GetSummonAttackInterval(
        float baseValue,
        string summonName)
    {
        return Mathf.Max(
            0.05f,
            baseValue
        );
    }

    public static float GetManaCost(
        float baseManaCost)
    {
        return Mathf.Max(
            0f,
            baseManaCost *
            summonManaCostMultiplier
        );
    }

    public static float GetCooldown(
        float baseCooldown)
    {
        return Mathf.Max(
            0.05f,
            baseCooldown *
            summonCooldownMultiplier
        );
    }

    public static float GetManaRecoveryAmount(
        float baseAmount)
    {
        return Mathf.Max(
            0f,
            baseAmount *
            manaRecoveryMultiplier
        );
    }

    private static void RefreshAffectedSummons(
        AugmentContract contract)
    {
        SummonUnitBase[] summons =
            UnityEngine.Object
                .FindObjectsOfType<SummonUnitBase>();

        for (int i = 0;
             i < summons.Length;
             i++)
        {
            SummonUnitBase summon =
                summons[i];

            if (summon == null ||
                summon.IsDead)
            {
                continue;
            }

            if (!IsAffected(
                    summon,
                    contract
                ))
            {
                continue;
            }

            summon.RefreshAugmentStats();
        }
    }

    private static bool IsAffected(
        SummonUnitBase summon,
        AugmentContract contract)
    {
        if (summon == null ||
            contract == null)
        {
            return false;
        }

        if (contract.scope ==
            AugmentScope.Common)
        {
            return true;
        }

        if (contract.scope !=
            AugmentScope.SummonExclusive)
        {
            return false;
        }

        return Normalize(
                   summon.SummonName
               ) ==
               Normalize(
                   contract.summonName
               );
    }

    private static bool IsSummon(
        string currentName,
        string expectedName)
    {
        return Normalize(
                   currentName
               ) ==
               Normalize(
                   expectedName
               );
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value
            ))
        {
            return string.Empty;
        }

        return value
            .Replace(
                " ",
                string.Empty
            )
            .Replace(
                "_",
                string.Empty
            )
            .Trim();
    }

    private static void RefreshSummonManagerUI()
    {
        if (SummonManager.Instance != null)
        {
            SummonManager.Instance
                .RefreshAugmentAffectedUI();
        }
    }
}
