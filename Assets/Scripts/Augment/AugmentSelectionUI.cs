using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AugmentSelectionUI : MonoBehaviour
{
    [Serializable]
    public class ChoiceSlot
    {
        public Button button;

        [FormerlySerializedAs("tierText")]
        public TMP_Text typeText;

        public TMP_Text nameText;

        [FormerlySerializedAs("effectText")]
        public TMP_Text descriptionText;
    }

    [Serializable]
    public class TierWeight
    {
        [Range(0f, 100f)] public float t1 = 45f;
        [Range(0f, 100f)] public float t2 = 35f;
        [Range(0f, 100f)] public float t3 = 17f;
        [Range(0f, 100f)] public float t4 = 3f;
    }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private ChoiceSlot[] choiceSlots = new ChoiceSlot[3];

    [Header("한글 폰트")]
    [Tooltip("한글 글리프가 들어있는 TMP Font Asset 하나만 연결하세요. 3개 카드의 모든 텍스트에 자동 적용됩니다.")]
    [SerializeField] private TMP_FontAsset koreanFont;

    [Header("티어 테두리 색상")]
    [Tooltip("T1 테두리 색상")]
    [SerializeField] private Color t1Color = new Color(0.72f, 0.72f, 0.72f, 1f);

    [Tooltip("T2 테두리 색상")]
    [SerializeField] private Color t2Color = new Color(0.48f, 0.62f, 0.72f, 1f);

    [Tooltip("T3 테두리 색상")]
    [SerializeField] private Color t3Color = new Color(0.52f, 0.40f, 0.66f, 1f);

    [Tooltip("T4 테두리 색상")]
    [SerializeField] private Color t4Color = new Color(0.72f, 0.60f, 0.34f, 1f);

    [Tooltip("카드 바깥 테두리 두께")]
    [SerializeField] private Vector2 tierBorderDistance = new Vector2(3f, -3f);

    [Header("테스트")]
    [SerializeField] private bool enableTestKey = true;
    [SerializeField] private KeyCode testKey = KeyCode.F6;

    [Header("전용/공용")]
    [Range(0f, 1f)]
    [SerializeField] private float thirdCardExclusiveChance = 0.65f;

    [Header("1~7회차 티어 확률")]
    [Tooltip("처음부터 T1~T4 전부 등장 가능. 하드 해금 제한 없음.")]
    [SerializeField] private TierWeight[] tierWeights = new TierWeight[7];

    [Header("T4 소프트 보정")]
    [Range(0f, 1f)]
    [SerializeField] private float oneT4Multiplier = 0.35f;

    [Range(0f, 1f)]
    [SerializeField] private float twoT4Multiplier = 0f;

    [Header("전용 집중 보정")]
    [SerializeField] private float focusedSummonWeight = 1.4f;

    private readonly List<AugmentContract> allContracts = new List<AugmentContract>();
    private readonly List<AugmentContract> currentChoices = new List<AugmentContract>();
    private readonly HashSet<string> acquiredIds = new HashSet<string>();
    private readonly Dictionary<string, int> exclusiveCounts = new Dictionary<string, int>();

    private bool isOpen;
    private int selectionCount;
    private float previousTimeScale = 1f;

    public bool IsOpen => isOpen;
    public int SelectionCount => selectionCount;

    private void Reset()
    {
        SetupDefaultTierWeights();
    }

    private void Awake()
    {
        SetupDefaultTierWeightsIfNeeded();

        allContracts.Clear();
        allContracts.AddRange(AugmentContractCatalog.CreateAll());

        SetupButtons();
        ApplyKoreanFontToAllSlots();
        SetupTierOutlines();

        if (panel != null)
            panel.SetActive(false);

        Debug.Log($"[강화계약] 데이터 {allContracts.Count}개 로드 완료.");
    }

    private void Update()
    {
        if (enableTestKey && Input.GetKeyDown(testKey))
            OpenSelection();
    }

    private void SetupDefaultTierWeightsIfNeeded()
    {
        if (tierWeights == null || tierWeights.Length != 7)
        {
            SetupDefaultTierWeights();
            return;
        }

        for (int i = 0; i < tierWeights.Length; i++)
        {
            if (tierWeights[i] == null)
            {
                SetupDefaultTierWeights();
                return;
            }
        }
    }

    private void SetupDefaultTierWeights()
    {
        tierWeights = new TierWeight[7];
        tierWeights[0] = MakeWeights(45f, 35f, 17f, 3f);
        tierWeights[1] = MakeWeights(42f, 35f, 18f, 5f);
        tierWeights[2] = MakeWeights(38f, 35f, 20f, 7f);
        tierWeights[3] = MakeWeights(32f, 35f, 23f, 10f);
        tierWeights[4] = MakeWeights(27f, 33f, 26f, 14f);
        tierWeights[5] = MakeWeights(20f, 30f, 30f, 20f);
        tierWeights[6] = MakeWeights(10f, 25f, 35f, 30f);
    }

    private TierWeight MakeWeights(float t1, float t2, float t3, float t4)
    {
        return new TierWeight { t1 = t1, t2 = t2, t3 = t3, t4 = t4 };
    }

    private void SetupButtons()
    {
        if (choiceSlots == null)
            return;

        for (int i = 0; i < choiceSlots.Length; i++)
        {
            int index = i;
            ChoiceSlot slot = choiceSlots[i];

            if (slot == null || slot.button == null)
                continue;

            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => SelectChoice(index));
        }
    }

    public void OpenSelection()
    {
        if (isOpen)
            return;

        if (panel == null)
        {
            Debug.LogError("[강화계약] Panel이 연결되어 있지 않습니다.");
            return;
        }

        if (SummonManager.Instance == null)
        {
            Debug.LogError("[강화계약] SummonManager가 없습니다.");
            return;
        }

        currentChoices.Clear();
        BuildThreeChoices();

        if (currentChoices.Count == 0)
        {
            Debug.LogWarning("[강화계약] 표시 가능한 계약이 없습니다.");
            return;
        }

        RefreshUI();

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isOpen = true;
        panel.SetActive(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void BuildThreeChoices()
    {
        HashSet<string> deck = GetCurrentDeckSummonNames();

        AugmentContract exclusive = PickContract(AugmentScope.SummonExclusive, deck, currentChoices);
        if (exclusive != null)
            currentChoices.Add(exclusive);

        AugmentContract common = PickContract(AugmentScope.Common, deck, currentChoices);
        if (common != null)
            currentChoices.Add(common);

        AugmentScope preferred = UnityEngine.Random.value < thirdCardExclusiveChance
            ? AugmentScope.SummonExclusive
            : AugmentScope.Common;

        AugmentContract third = PickContract(preferred, deck, currentChoices);

        if (third == null)
        {
            third = PickContract(
                preferred == AugmentScope.Common
                    ? AugmentScope.SummonExclusive
                    : AugmentScope.Common,
                deck,
                currentChoices);
        }

        if (third != null)
            currentChoices.Add(third);

        while (currentChoices.Count < 3)
        {
            AugmentContract fallback = PickAnyContract(deck, currentChoices);
            if (fallback == null)
                break;

            currentChoices.Add(fallback);
        }

        Shuffle(currentChoices);
    }

    private HashSet<string> GetCurrentDeckSummonNames()
    {
        HashSet<string> result = new HashSet<string>();

        for (int i = 0; i < 8; i++)
        {
            SummonData data = SummonManager.Instance.GetSummonDataFromSlot(i);

            if (data == null || string.IsNullOrWhiteSpace(data.SummonName))
                continue;

            result.Add(data.SummonName.Trim());
        }

        return result;
    }

    private AugmentContract PickContract(
        AugmentScope scope,
        HashSet<string> deck,
        List<AugmentContract> alreadyChosen)
    {
        List<WeightedCandidate> candidates = new List<WeightedCandidate>();

        for (int i = 0; i < allContracts.Count; i++)
        {
            AugmentContract contract = allContracts[i];

            if (!CanAppear(contract, scope, deck, alreadyChosen))
                continue;

            float weight = GetTierWeight(contract.tier);

            if (contract.scope == AugmentScope.SummonExclusive &&
                exclusiveCounts.TryGetValue(contract.summonName, out int count) &&
                count >= 2)
            {
                weight *= focusedSummonWeight;
            }

            if (contract.tier == AugmentTier.T4)
            {
                int t4Count = GetAcquiredTierCount(AugmentTier.T4);
                if (t4Count == 1) weight *= oneT4Multiplier;
                else if (t4Count >= 2) weight *= twoT4Multiplier;
            }

            if (weight > 0f)
                candidates.Add(new WeightedCandidate(contract, weight));
        }

        return PickWeighted(candidates);
    }

    private AugmentContract PickAnyContract(
        HashSet<string> deck,
        List<AugmentContract> alreadyChosen)
    {
        List<WeightedCandidate> candidates = new List<WeightedCandidate>();

        for (int i = 0; i < allContracts.Count; i++)
        {
            AugmentContract contract = allContracts[i];

            if (acquiredIds.Contains(contract.id))
                continue;

            if (ContainsId(alreadyChosen, contract.id))
                continue;

            if (contract.scope == AugmentScope.SummonExclusive &&
                !deck.Contains(contract.summonName))
                continue;

            float weight = GetTierWeight(contract.tier);

            if (contract.tier == AugmentTier.T4)
            {
                int t4Count = GetAcquiredTierCount(AugmentTier.T4);
                if (t4Count == 1) weight *= oneT4Multiplier;
                else if (t4Count >= 2) weight *= twoT4Multiplier;
            }

            if (weight > 0f)
                candidates.Add(new WeightedCandidate(contract, weight));
        }

        return PickWeighted(candidates);
    }

    private bool CanAppear(
        AugmentContract contract,
        AugmentScope requiredScope,
        HashSet<string> deck,
        List<AugmentContract> alreadyChosen)
    {
        if (contract == null || contract.scope != requiredScope)
            return false;

        if (acquiredIds.Contains(contract.id) || ContainsId(alreadyChosen, contract.id))
            return false;

        if (contract.scope == AugmentScope.SummonExclusive &&
            !deck.Contains(contract.summonName))
            return false;

        return true;
    }

    private float GetTierWeight(AugmentTier tier)
    {
        int index = Mathf.Clamp(selectionCount, 0, tierWeights.Length - 1);
        TierWeight w = tierWeights[index];

        switch (tier)
        {
            case AugmentTier.T1: return w.t1;
            case AugmentTier.T2: return w.t2;
            case AugmentTier.T3: return w.t3;
            case AugmentTier.T4: return w.t4;
            default: return 0f;
        }
    }

    private void SelectChoice(int index)
    {
        if (!isOpen || index < 0 || index >= currentChoices.Count)
            return;

        AugmentContract selected = currentChoices[index];

        acquiredIds.Add(selected.id);
        selectionCount++;

        if (selected.scope == AugmentScope.SummonExclusive)
        {
            if (!exclusiveCounts.ContainsKey(selected.summonName))
                exclusiveCounts[selected.summonName] = 0;

            exclusiveCounts[selected.summonName]++;
        }

        Debug.Log(
            $"[강화계약 획득] {selected.tier} / {selected.contractName} / {selected.effectText}");

        ApplyContract(selected);
        CloseSelection();
    }

    private void ApplyContract(AugmentContract contract)
    {
        AugmentRuntimeEffects.ApplyContract(
            contract
        );
    }

    private void RefreshUI()
    {
        for (int i = 0; i < choiceSlots.Length; i++)
        {
            ChoiceSlot slot = choiceSlots[i];
            if (slot == null)
                continue;

            bool active = i < currentChoices.Count;

            if (slot.button != null)
                slot.button.gameObject.SetActive(active);

            if (!active)
                continue;

            AugmentContract contract = currentChoices[i];

            string target =
                contract.scope == AugmentScope.Common
                    ? "공용"
                    : contract.summonName;

            // 티어 문자는 표시하지 않는다.
            if (slot.typeText != null)
                slot.typeText.text = target;

            Color tierColor = GetTierColor(contract.tier);

            // 카드 내부는 원래 색 유지.
            // 티어는 카드 바깥 테두리 색으로만 표시한다.
            if (slot.button != null)
            {
                if (slot.button.image != null)
                    slot.button.image.color = Color.white;

                Outline outline =
                    slot.button.GetComponent<Outline>();

                if (outline == null)
                    outline = slot.button.gameObject.AddComponent<Outline>();

                outline.effectColor = tierColor;
                outline.effectDistance = tierBorderDistance;
                outline.useGraphicAlpha = true;
                outline.enabled = true;
            }

            if (slot.nameText != null)
                slot.nameText.text = SanitizeText(contract.contractName);

            if (slot.descriptionText != null)
                slot.descriptionText.text = MakePoliteDescription(SanitizeText(contract.effectText));
        }
    }

    private void ApplyKoreanFontToAllSlots()
    {
        if (koreanFont == null ||
            choiceSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot = choiceSlots[i];

            if (slot == null)
                continue;

            if (slot.typeText != null)
                slot.typeText.font = koreanFont;

            if (slot.nameText != null)
                slot.nameText.font = koreanFont;

            if (slot.descriptionText != null)
                slot.descriptionText.font = koreanFont;
        }
    }

    private void SetupTierOutlines()
    {
        if (choiceSlots == null)
            return;

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot = choiceSlots[i];

            if (slot == null ||
                slot.button == null)
            {
                continue;
            }

            Outline outline =
                slot.button.GetComponent<Outline>();

            if (outline == null)
                outline =
                    slot.button.gameObject
                        .AddComponent<Outline>();

            outline.effectDistance = tierBorderDistance;
            outline.useGraphicAlpha = true;
        }
    }

    private Color GetTierColor(AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.T1:
                return t1Color;

            case AugmentTier.T2:
                return t2Color;

            case AugmentTier.T3:
                return t3Color;

            case AugmentTier.T4:
                return t4Color;

            default:
                return Color.white;
        }
    }

    private string SanitizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("×", "x")
            .Replace("·", "/")
            .Replace("–", "-")
            .Replace("—", "-")
            .Replace("→", "->")
            .Replace("“", "\"")
            .Replace("”", "\"")
            .Replace("‘", "'")
            .Replace("’", "'");
    }

    private string MakePoliteDescription(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        List<string> parts = SplitDescriptionSentences(text);
        List<string> result = new List<string>();

        for (int i = 0; i < parts.Count; i++)
        {
            string part = parts[i].Trim();

            if (string.IsNullOrEmpty(part))
                continue;

            result.Add(FormalizeSentence(part));
        }

        return string.Join(" ", result);
    }

    private List<string> SplitDescriptionSentences(string text)
    {
        List<string> parts = new List<string>();
        int start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '.')
                continue;

            bool decimalPoint =
                i > 0 &&
                i + 1 < text.Length &&
                char.IsDigit(text[i - 1]) &&
                char.IsDigit(text[i + 1]);

            if (decimalPoint)
                continue;

            string part = text.Substring(start, i - start).Trim();

            if (!string.IsNullOrEmpty(part))
                parts.Add(part);

            start = i + 1;
        }

        if (start < text.Length)
        {
            string last = text.Substring(start).Trim();

            if (!string.IsNullOrEmpty(last))
                parts.Add(last);
        }

        return parts;
    }

    private string FormalizeSentence(string sentence)
    {
        string s = sentence.Trim().TrimEnd('.');

        if (string.IsNullOrEmpty(s))
            return string.Empty;

        string[] alreadyFormal =
        {
            "합니다", "됩니다", "입니다", "않습니다", "줍니다",
            "얻습니다", "시킵니다", "유지합니다", "부여합니다",
            "회복합니다", "생성합니다", "증가합니다", "감소합니다",
            "초기화됩니다", "적용됩니다", "무시합니다", "부활합니다"
        };

        for (int i = 0; i < alreadyFormal.Length; i++)
        {
            if (s.EndsWith(alreadyFormal[i]))
                return s + ".";
        }

        if (s.EndsWith("내부 쿨"))
            return s.Substring(0, s.Length - "내부 쿨".Length).TrimEnd() + "내부 쿨타임이 적용됩니다.";

        if (s.EndsWith("쿨"))
            return s.Substring(0, s.Length - "쿨".Length).TrimEnd() + "쿨타임이 적용됩니다.";

        if (s.EndsWith("회복")) return s + "합니다.";
        if (s.EndsWith("획득")) return s + "합니다.";
        if (s.EndsWith("복구")) return s + "합니다.";
        if (s.EndsWith("생성")) return s + "합니다.";
        if (s.EndsWith("재지정")) return s + "합니다.";
        if (s.EndsWith("재출현")) return s + "합니다.";
        if (s.EndsWith("전환")) return s + "됩니다.";
        if (s.EndsWith("집중")) return s + "됩니다.";
        if (s.EndsWith("완화")) return s + "됩니다.";
        if (s.EndsWith("초기화")) return s + "됩니다.";
        if (s.EndsWith("면역")) return s + "을 얻습니다.";
        if (s.EndsWith("부활")) return s + "합니다.";
        if (s.EndsWith("복귀")) return s + "합니다.";
        if (s.EndsWith("연쇄")) return s + "됩니다.";
        if (s.EndsWith("속박")) return s + "시킵니다.";
        if (s.EndsWith("경직")) return s + "시킵니다.";
        if (s.EndsWith("밀침")) return s.Substring(0, s.Length - 2) + "밀어냅니다.";
        if (s.EndsWith("무시")) return s + "합니다.";
        if (s.EndsWith("증가")) return s + "합니다.";
        if (s.EndsWith("감소")) return s + "합니다.";
        if (s.EndsWith("둔화")) return s + "시킵니다.";
        if (s.EndsWith("기절")) return s + "시킵니다.";
        if (s.EndsWith("처형")) return s + "합니다.";
        if (s.EndsWith("관통")) return s + "합니다.";
        if (s.EndsWith("재매복")) return s + "합니다.";
        if (s.EndsWith("재기")) return s + "합니다.";
        if (s.EndsWith("설치")) return s + "합니다.";
        if (s.EndsWith("봉쇄")) return s + "합니다.";
        if (s.EndsWith("표식")) return s + "을 부여합니다.";
        if (s.EndsWith("감염")) return s + "시킵니다.";
        if (s.EndsWith("반격")) return s + "합니다.";
        if (s.EndsWith("저장")) return s + "합니다.";
        if (s.EndsWith("방지")) return s + "합니다.";
        if (s.EndsWith("공격")) return s + "합니다.";
        if (s.EndsWith("피해")) return s + "를 줍니다.";

        // 수치형 설명은 내용을 줄이지 않고 원문 수치를 그대로 보여준 뒤
        // 완전한 문장으로 마무리합니다.
        if (s.Contains("+") || s.Contains("-%") || s.Contains("-"))
            return s + "의 효과가 적용됩니다.";

        return s + "합니다.";
    }

    private void CloseSelection()
    {
        isOpen = false;
        currentChoices.Clear();

        if (panel != null)
            panel.SetActive(false);

        Time.timeScale = previousTimeScale;
    }

    private int GetAcquiredTierCount(AugmentTier tier)
    {
        int count = 0;

        for (int i = 0; i < allContracts.Count; i++)
        {
            AugmentContract contract = allContracts[i];
            if (contract.tier == tier && acquiredIds.Contains(contract.id))
                count++;
        }

        return count;
    }

    private bool ContainsId(List<AugmentContract> list, string id)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].id == id)
                return true;
        }

        return false;
    }

    private void Shuffle(List<AugmentContract> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            AugmentContract temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private AugmentContract PickWeighted(List<WeightedCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        float total = 0f;
        for (int i = 0; i < candidates.Count; i++)
            total += Mathf.Max(0f, candidates[i].weight);

        if (total <= 0f)
            return candidates[UnityEngine.Random.Range(0, candidates.Count)].contract;

        float roll = UnityEngine.Random.Range(0f, total);

        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= candidates[i].weight;
            if (roll <= 0f)
                return candidates[i].contract;
        }

        return candidates[candidates.Count - 1].contract;
    }

    private class WeightedCandidate
    {
        public AugmentContract contract;
        public float weight;

        public WeightedCandidate(AugmentContract contract, float weight)
        {
            this.contract = contract;
            this.weight = weight;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (choiceSlots == null || choiceSlots.Length != 3)
            Array.Resize(ref choiceSlots, 3);

        if (tierWeights == null || tierWeights.Length != 7)
            SetupDefaultTierWeights();

        thirdCardExclusiveChance = Mathf.Clamp01(thirdCardExclusiveChance);
        oneT4Multiplier = Mathf.Clamp01(oneT4Multiplier);
        twoT4Multiplier = Mathf.Clamp01(twoT4Multiplier);
        focusedSummonWeight = Mathf.Max(1f, focusedSummonWeight);
    }
}
#endif