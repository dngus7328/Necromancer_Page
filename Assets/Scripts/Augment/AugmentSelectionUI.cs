using System;
using System.Collections;
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

    [Header("티어 카드 색상")]
    [Tooltip("T1 뒤쪽 카드 색상")]
    [SerializeField]
    private Color t1Color =
        new Color(
            0.62f,
            0.62f,
            0.66f,
            1f
        );

    [Tooltip("T2 뒤쪽 카드 색상")]
    [SerializeField]
    private Color t2Color =
        new Color(
            0.40f,
            0.56f,
            0.72f,
            1f
        );

    [Tooltip("T3 뒤쪽 카드 색상")]
    [SerializeField]
    private Color t3Color =
        new Color(
            0.56f,
            0.34f,
            0.78f,
            1f
        );

    [Tooltip("T4 뒤쪽 카드 색상")]
    [SerializeField]
    private Color t4Color =
        new Color(
            0.86f,
            0.66f,
            0.24f,
            1f
        );

    [Header("티어 도트 빛")]
    [Tooltip("원본 카드의 RGB는 무시하고 알파 모양만 사용해 티어 색으로 칠하는 셰이더입니다.")]
    [SerializeField]
    private Shader tierBackShader;

    [Tooltip("가장 안쪽 도트 빛 크기")]
    [Min(1f)]
    [SerializeField]
    private float pixelGlowInnerScale = 1.012f;

    [Tooltip("중간 도트 빛 크기")]
    [Min(1f)]
    [SerializeField]
    private float pixelGlowMiddleScale = 1.024f;

    [Tooltip("가장 바깥 도트 빛 크기")]
    [Min(1f)]
    [SerializeField]
    private float pixelGlowOuterScale = 1.038f;

    [Tooltip("T1 빛 전체 강도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float t1BackAlpha = 0.22f;

    [Tooltip("T2 빛 전체 강도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float t2BackAlpha = 0.34f;

    [Tooltip("T3 빛 전체 강도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float t3BackAlpha = 0.46f;

    [Tooltip("T4 빛 전체 강도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float t4BackAlpha = 0.72f;

    [Tooltip("도트 빛이 안쪽에서 바깥쪽으로 퍼졌다가 다시 줄어듭니다.")]
    [SerializeField]
    private bool pulseTierBack = true;

    [Tooltip("한 번 퍼졌다가 줄어드는 시간")]
    [Min(0.4f)]
    [SerializeField]
    private float tierPulseDuration = 1.55f;

    [Tooltip("도트 단계가 바뀌는 횟수. 6~10 정도가 도트 느낌에 잘 맞습니다.")]
    [Range(3, 16)]
    [SerializeField]
    private int pixelPulseSteps = 8;

    [Header("티어 도트 빛 세부 조절")]
    [Tooltip("빛이 가장 약할 때 안쪽 레이어 비율")]
    [Range(0f, 1.5f)]
    [SerializeField]
    private float innerGlowMin = 0.72f;

    [Tooltip("빛이 가장 강할 때 안쪽 레이어 비율")]
    [Range(0f, 1.5f)]
    [SerializeField]
    private float innerGlowMax = 1.00f;

    [Tooltip("빛이 가장 약할 때 중간 레이어 비율")]
    [Range(0f, 1.5f)]
    [SerializeField]
    private float middleGlowMin = 0.12f;

    [Tooltip("빛이 가장 강할 때 중간 레이어 비율")]
    [Range(0f, 1.5f)]
    [SerializeField]
    private float middleGlowMax = 0.68f;

    [Tooltip("빛이 가장 약할 때 바깥 레이어 비율")]
    [Range(0f, 1.5f)]
    [SerializeField]
    private float outerGlowMin = 0.02f;

    [Tooltip("빛이 가장 강할 때 바깥 레이어 비율")]
    [Range(0f, 1.5f)]
    [SerializeField]
    private float outerGlowMax = 0.38f;

    [Tooltip("T1 맥동 강도")]
    [Range(0.5f, 2f)]
    [SerializeField]
    private float t1PulseStrength = 0.85f;

    [Tooltip("T2 맥동 강도")]
    [Range(0.5f, 2f)]
    [SerializeField]
    private float t2PulseStrength = 1.00f;

    [Tooltip("T3 맥동 강도")]
    [Range(0.5f, 2f)]
    [SerializeField]
    private float t3PulseStrength = 1.15f;

    [Tooltip("T4 맥동 강도")]
    [Range(0.5f, 2.5f)]
    [SerializeField]
    private float t4PulseStrength = 1.55f;

    [Tooltip("T4만 바깥으로 더 넓게 퍼지는 정도")]
    [Range(1f, 2f)]
    [SerializeField]
    private float t4SpreadMultiplier = 1.28f;

    [Header("카드 등장 연출")]
    [Tooltip("강화계약 창을 열 때 카드가 왼쪽부터 차례로 튀어나옵니다.")]
    [SerializeField]
    private bool useCardRevealAnimation = true;

    [Tooltip("카드 한 장이 나타나는 시간")]
    [Range(0.08f, 1f)]
    [SerializeField]
    private float cardRevealDuration = 0.24f;

    [Tooltip("다음 카드가 나타나기까지의 간격")]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float cardRevealStagger = 0.08f;

    [Tooltip("등장 직후 시작 크기")]
    [Range(0.2f, 1f)]
    [SerializeField]
    private float cardRevealStartScale = 0.84f;

    [Tooltip("마지막에 살짝 튀어나오는 최대 크기")]
    [Range(1f, 1.2f)]
    [SerializeField]
    private float cardRevealOvershootScale = 1.045f;

    [Tooltip("체감이 도트 애니메이션처럼 보이도록 크기 변화를 몇 단계로 끊을지 정합니다.")]
    [Range(3, 20)]
    [SerializeField]
    private int cardRevealSteps = 9;

    [Tooltip("자기 차례가 오기 전 카드를 완전히 숨깁니다.")]
    [SerializeField]
    private bool hideUntilRevealTurn = true;

    [Header("카드 선택 연출")]
    [Tooltip("카드를 선택했을 때 선택 확인 연출을 재생합니다.")]
    [SerializeField]
    private bool useCardSelectAnimation = true;

    [Tooltip("선택 연출 전체 시간")]
    [Range(0.12f, 1.2f)]
    [SerializeField]
    private float cardSelectDuration = 0.38f;

    [Tooltip("선택한 카드가 순간적으로 커지는 최대 크기")]
    [Range(1f, 1.25f)]
    [SerializeField]
    private float selectedCardPopScale = 1.075f;

    [Tooltip("선택하지 않은 카드가 줄어드는 크기")]
    [Range(0.6f, 1f)]
    [SerializeField]
    private float unselectedCardScale = 0.90f;

    [Tooltip("선택하지 않은 카드의 최종 투명도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float unselectedCardAlpha = 0.20f;

    [Tooltip("선택한 카드도 마지막 순간 살짝 어두워지게 할지 결정합니다.")]
    [Range(0.5f, 1f)]
    [SerializeField]
    private float selectedCardEndAlpha = 1.00f;

    [Tooltip("선택 연출의 크기 변화를 몇 단계로 끊을지 정합니다.")]
    [Range(3, 20)]
    [SerializeField]
    private int cardSelectSteps = 8;

    [Tooltip("선택한 카드의 티어 빛을 연출 중 얼마나 강하게 할지 정합니다.")]
    [Range(1f, 2.5f)]
    [SerializeField]
    private float selectedGlowBoost = 1.55f;

    [Header("선택 후 도트 화면 전환")]
    [Tooltip("카드를 고른 뒤 게임 화면으로 돌아갈 때 도트 블록 전환을 사용합니다.")]
    [SerializeField]
    private bool usePixelReturnFade = true;

    [Tooltip("화면이 도트 블록으로 완전히 덮이는 시간")]
    [Range(0.05f, 1f)]
    [SerializeField]
    private float pixelFadeCoverDuration = 0.16f;

    [Tooltip("완전히 덮인 상태를 유지하는 시간")]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float pixelFadeHoldDuration = 0.06f;

    [Tooltip("게임 화면이 다시 드러나는 시간")]
    [Range(0.05f, 1f)]
    [SerializeField]
    private float pixelFadeRevealDuration = 0.20f;

    [Tooltip("도트 전환 색상")]
    [SerializeField]
    private Color pixelFadeColor =
        new Color(
            0.035f,
            0.015f,
            0.055f,
            1f
        );

    [Tooltip("1920x1080 기준 도트 블록 수. 80x45면 한 칸이 약 24px입니다.")]
    [Range(24, 160)]
    [SerializeField]
    private int pixelFadeGridWidth = 80;

    [Range(14, 90)]
    [SerializeField]
    private int pixelFadeGridHeight = 45;


    [Header("등급별 도트 전환 강도")]
    [Tooltip("T1은 짧고 담백하게, T4로 갈수록 전환을 더 강하게 만듭니다.")]
    [SerializeField]
    private bool useTierBasedPixelFade = true;

    [SerializeField]
    private Color t1FadeColor =
        new Color(
            0.34f,
            0.35f,
            0.38f,
            1f
        );

    [SerializeField]
    private Color t2FadeColor =
        new Color(
            0.24f,
            0.31f,
            0.39f,
            1f
        );

    [SerializeField]
    private Color t3FadeColor =
        new Color(
            0.34f,
            0.16f,
            0.46f,
            1f
        );

    [SerializeField]
    private Color t4FadeColor =
        new Color(
            0.56f,
            0.43f,
            0.18f,
            1f
        );


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

    private PixelFadeOverlay pixelFadeOverlay;

    private bool isOpen;
    private int selectionCount;
    private float previousTimeScale = 1f;

    private Coroutine cardRevealCoroutine;
    private Coroutine cardSelectCoroutine;

    private Vector3[] cardBaseScales;
    private bool isSelecting;

    private AugmentTier[] displayedTiers;

    private class PixelGlowLayers
    {
        public Image inner;
        public Image middle;
        public Image outer;

        public Material innerMaterial;
        public Material middleMaterial;
        public Material outerMaterial;
    }

    private readonly Dictionary<Button, PixelGlowLayers> tierBackLayers =
        new Dictionary<Button, PixelGlowLayers>();

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

        if (tierBackShader == null)
        {
            tierBackShader =
                Shader.Find(
                    "UI/TierBackSolidTint"
                );
        }

        if (tierBackShader == null)
        {
            Debug.LogError(
                "[강화계약] UI/TierBackSolidTint 셰이더를 찾지 못했습니다. TierBackSolidTint.shader 파일을 Assets 폴더에 넣어주세요."
            );
        }

        SetupTierBackCards();

        displayedTiers =
            new AugmentTier[
                choiceSlots != null
                    ? choiceSlots.Length
                    : 0
            ];

        cardBaseScales =
            new Vector3[
                choiceSlots != null
                    ? choiceSlots.Length
                    : 0
            ];

        CacheCardBaseScales();

        if (panel != null)
            panel.SetActive(false);

        if (usePixelReturnFade)
        {
            EnsurePixelFadeOverlay();
        }

        Debug.Log($"[강화계약] 데이터 {allContracts.Count}개 로드 완료.");
    }

    private void Update()
    {
        if (enableTestKey &&
            Input.GetKeyDown(
                testKey
            ))
        {
            OpenSelection();
        }

        if (isOpen)
        {
            UpdateTierBackPulse();
        }
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

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isOpen = true;

        panel.SetActive(true);

        RefreshUI();

        StartCardRevealAnimation();

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

        Debug.Log(
            $"[강화계약] 현재 덱 전용 이름: {string.Join(", ", deck)}"
        );
    }

    private static string NormalizeSummonName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Trim();
    }

    private HashSet<string> GetCurrentDeckSummonNames()
    {
        HashSet<string> result =
            new HashSet<string>();

        if (SummonManager.Instance == null)
        {
            return result;
        }

        for (int i = 0; i < 8; i++)
        {
            SummonData data =
                SummonManager.Instance
                    .GetSummonDataFromSlot(i);

            if (data == null ||
                string.IsNullOrWhiteSpace(
                    data.SummonName
                ))
            {
                continue;
            }

            string normalized =
                NormalizeSummonName(
                    data.SummonName
                );

            if (!string.IsNullOrEmpty(
                    normalized
                ))
            {
                result.Add(
                    normalized
                );
            }
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

            if (contract.scope ==
                AugmentScope.SummonExclusive)
            {
                string normalizedContractName =
                    NormalizeSummonName(
                        contract.summonName
                    );

                if (!deck.Contains(
                        normalizedContractName
                    ))
                {
                    continue;
                }
            }

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

        if (contract.scope ==
            AugmentScope.SummonExclusive)
        {
            string normalizedContractName =
                NormalizeSummonName(
                    contract.summonName
                );

            if (!deck.Contains(
                    normalizedContractName
                ))
            {
                return false;
            }
        }

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
        if (!isOpen ||
            isSelecting ||
            index < 0 ||
            index >= currentChoices.Count)
        {
            return;
        }

        if (!useCardSelectAnimation)
        {
            CommitSelectedChoice(
                index
            );

            return;
        }

        StopCardRevealAnimation(
            false
        );

        cardSelectCoroutine =
            StartCoroutine(
                CardSelectRoutine(
                    index
                )
            );
    }

    private void CommitSelectedChoice(
        int index)
    {
        if (index < 0 ||
            index >= currentChoices.Count)
        {
            return;
        }

        AugmentContract selected =
            currentChoices[index];

        acquiredIds.Add(
            selected.id
        );

        selectionCount++;

        if (selected.scope ==
            AugmentScope.SummonExclusive)
        {
            if (!exclusiveCounts.ContainsKey(
                    selected.summonName
                ))
            {
                exclusiveCounts[
                    selected.summonName
                ] = 0;
            }

            exclusiveCounts[
                selected.summonName
            ]++;
        }

        Debug.Log(
            $"[강화계약 획득] {selected.tier} / {selected.contractName} / {selected.effectText}"
        );

        StartCoroutine(
            CommitSelectedChoiceRoutine(
                selected
            )
        );
    }

    private IEnumerator CommitSelectedChoiceRoutine(
        AugmentContract selected)
    {
        if (selected == null)
        {
            CloseSelection();
            yield break;
        }

        if (!usePixelReturnFade)
        {
            ApplyContract(
                selected
            );

            CloseSelection();
            yield break;
        }

        EnsurePixelFadeOverlay();

        if (pixelFadeOverlay == null)
        {
            ApplyContract(
                selected
            );

            CloseSelection();
            yield break;
        }

        bool blackReached =
            false;

        GetTierFadeSettings(
            selected.tier,
            out float coverDuration,
            out float holdDuration,
            out float revealDuration,
            out Color fadeColor,
            out int gridWidth,
            out int gridHeight,
            out PixelFadePattern fadePattern
        );

        pixelFadeOverlay.PlayTransition(
            coverDuration,
            holdDuration,
            revealDuration,
            fadeColor,
            gridWidth,
            gridHeight,
            fadePattern,
            () =>
            {
                ApplyContract(
                    selected
                );

                CloseSelection();

                blackReached =
                    true;
            }
        );

        while (!blackReached)
        {
            yield return null;
        }

        while (pixelFadeOverlay != null &&
               pixelFadeOverlay.IsPlaying)
        {
            yield return null;
        }
    }

    private void GetTierFadeSettings(
        AugmentTier tier,
        out float coverDuration,
        out float holdDuration,
        out float revealDuration,
        out Color fadeColor,
        out int gridWidth,
        out int gridHeight,
        out PixelFadePattern fadePattern)
    {
        coverDuration =
            pixelFadeCoverDuration;

        holdDuration =
            pixelFadeHoldDuration;

        revealDuration =
            pixelFadeRevealDuration;

        fadeColor =
            pixelFadeColor;

        gridWidth =
            pixelFadeGridWidth;

        gridHeight =
            pixelFadeGridHeight;

        // 등급이 달라도 전환 방식은 항상 동일하게 유지합니다.
        // 차이는 색 / 속도 / 블록 크기 / 완전 덮임 유지시간만 둡니다.
        fadePattern =
            PixelFadePattern.Scatter;

        if (!useTierBasedPixelFade)
        {
            return;
        }

        switch (tier)
        {
            case AugmentTier.T1:
                coverDuration =
                    0.12f;

                holdDuration =
                    0.02f;

                revealDuration =
                    0.15f;

                fadeColor =
                    t1FadeColor;

                gridWidth =
                    88;

                gridHeight =
                    50;
                break;

            case AugmentTier.T2:
                coverDuration =
                    0.15f;

                holdDuration =
                    0.04f;

                revealDuration =
                    0.18f;

                fadeColor =
                    t2FadeColor;

                gridWidth =
                    80;

                gridHeight =
                    45;
                break;

            case AugmentTier.T3:
                coverDuration =
                    0.19f;

                holdDuration =
                    0.07f;

                revealDuration =
                    0.23f;

                fadeColor =
                    t3FadeColor;

                gridWidth =
                    68;

                gridHeight =
                    38;
                break;

            case AugmentTier.T4:
                coverDuration =
                    0.27f;

                holdDuration =
                    0.12f;

                revealDuration =
                    0.32f;

                fadeColor =
                    t4FadeColor;

                gridWidth =
                    56;

                gridHeight =
                    32;
                break;
        }
    }

    private void EnsurePixelFadeOverlay()
    {
        if (pixelFadeOverlay != null)
        {
            return;
        }

        pixelFadeOverlay =
            PixelFadeOverlay.GetOrCreate(
                transform
            );
    }

    private IEnumerator CardSelectRoutine(
        int selectedIndex)
    {
        isSelecting =
            true;

        SetAllChoiceButtonsInteractable(
            false
        );

        CanvasGroup[] groups =
            EnsureChoiceCanvasGroups();

        float duration =
            Mathf.Max(
                0.12f,
                cardSelectDuration
            );

        float elapsed =
            0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            int steps =
                Mathf.Max(
                    3,
                    cardSelectSteps
                );

            float steppedT =
                Mathf.Round(
                    t *
                    (steps - 1)
                ) /
                (steps - 1);

            for (int i = 0;
                 i < choiceSlots.Length;
                 i++)
            {
                ChoiceSlot slot =
                    choiceSlots[i];

                if (slot == null ||
                    slot.button == null ||
                    !slot.button.gameObject.activeSelf)
                {
                    continue;
                }

                Vector3 baseScale =
                    GetCardBaseScale(
                        i
                    );

                if (i == selectedIndex)
                {
                    float scaleMultiplier =
                        EvaluateSelectedCardScale(
                            steppedT
                        );

                    slot.button.transform.localScale =
                        baseScale *
                        scaleMultiplier;

                    if (groups != null &&
                        i < groups.Length &&
                        groups[i] != null)
                    {
                        groups[i].alpha =
                            Mathf.Lerp(
                                1f,
                                selectedCardEndAlpha,
                                steppedT
                            );
                    }

                    if (i < displayedTiers.Length)
                    {
                        // 선택한 카드만 티어 빛이 잠깐 더 강해집니다.
                        float boostPulse =
                            Mathf.Sin(
                                steppedT *
                                Mathf.PI
                            );

                        ApplyTierBackWithBoost(
                            slot.button,
                            displayedTiers[i],
                            Mathf.Clamp01(
                                0.55f +
                                boostPulse *
                                0.45f
                            ),
                            Mathf.Lerp(
                                1f,
                                selectedGlowBoost,
                                boostPulse
                            )
                        );
                    }
                }
                else
                {
                    float scaleMultiplier =
                        Mathf.Lerp(
                            1f,
                            unselectedCardScale,
                            steppedT
                        );

                    slot.button.transform.localScale =
                        baseScale *
                        scaleMultiplier;

                    if (groups != null &&
                        i < groups.Length &&
                        groups[i] != null)
                    {
                        groups[i].alpha =
                            Mathf.Lerp(
                                1f,
                                unselectedCardAlpha,
                                steppedT
                            );
                    }
                }
            }

            yield return null;
        }

        cardSelectCoroutine =
            null;

        CommitSelectedChoice(
            selectedIndex
        );
    }

    private float EvaluateSelectedCardScale(
        float t)
    {
        // 앞부분에서 빠르게 커지고,
        // 뒷부분에서 원래 크기로 살짝 정착합니다.
        const float peakPoint =
            0.58f;

        if (t <= peakPoint)
        {
            float localT =
                Mathf.Clamp01(
                    t /
                    peakPoint
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - localT,
                    3f
                );

            return Mathf.Lerp(
                1f,
                selectedCardPopScale,
                eased
            );
        }

        float settleT =
            Mathf.InverseLerp(
                peakPoint,
                1f,
                t
            );

        return Mathf.Lerp(
            selectedCardPopScale,
            1f,
            settleT
        );
    }

    private CanvasGroup[] EnsureChoiceCanvasGroups()
    {
        if (choiceSlots == null)
        {
            return null;
        }

        CanvasGroup[] result =
            new CanvasGroup[
                choiceSlots.Length
            ];

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null)
            {
                continue;
            }

            CanvasGroup group =
                slot.button.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group =
                    slot.button.gameObject
                        .AddComponent<CanvasGroup>();
            }

            group.alpha =
                1f;

            group.interactable =
                true;

            group.blocksRaycasts =
                true;

            result[i] =
                group;
        }

        return result;
    }

    private void SetAllChoiceButtonsInteractable(
        bool interactable)
    {
        if (choiceSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null)
            {
                continue;
            }

            slot.button.interactable =
                interactable;
        }
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
            {
                slot.button.gameObject.SetActive(
                    active
                );

                if (!active)
                {
                    SetTierBackEnabled(
                        slot.button,
                        false
                    );
                }
            }

            if (!active)
            {
                continue;
            }

            AugmentContract contract = currentChoices[i];

            string target =
                contract.scope == AugmentScope.Common
                    ? "공용"
                    : contract.summonName;

            // 티어 문자는 표시하지 않는다.
            if (slot.typeText != null)
            {
                PrepareTextComponent(
                    slot.typeText
                );

                ApplyResponsiveText(
                    slot.typeText,
                    target
                );
            }

            // 카드 내부 색은 그대로 유지하고
            // 등급 색은 프레임 바깥에서만 은은하게 보이게 합니다.
            if (slot.button != null)
            {
                if (slot.button.image != null)
                {
                    slot.button.image.color =
                        Color.white;
                }

                displayedTiers[i] =
                    contract.tier;

                ApplyTierBack(
                    slot.button,
                    contract.tier,
                    1f
                );
            }

            if (slot.nameText != null)
            {
                PrepareTextComponent(
                    slot.nameText
                );

                ApplyResponsiveText(
                    slot.nameText,
                    SanitizeText(
                        contract.contractName
                    )
                );
            }

            if (slot.descriptionText != null)
            {
                PrepareTextComponent(
                    slot.descriptionText
                );

                string description =
                    SanitizeText(
                        contract.effectText
                    );

                ApplyResponsiveText(
                    slot.descriptionText,
                    description
                );
            }
        }
    }

    // =========================================================
    // 언어가 바뀌어도 어절 중간이 잘리지 않는 반응형 줄바꿈
    //
    // 계약 데이터 자체에는 줄바꿈을 저장하지 않습니다.
    // 카드에 표시할 때 현재 TMP 영역의 실제 너비를 측정해서
    // 공백 위치에만 줄바꿈을 만들어 넣습니다.
    //
    // 한국어 / 영어처럼 공백으로 어절이 나뉘는 언어:
    //   현재 카드 너비에 맞춰 공백에서만 자동 줄바꿈합니다.
    //
    // 일본어 / 중국어처럼 공백이 거의 없는 언어:
    //   TMP 기본 CJK 줄바꿈을 사용합니다.
    // =========================================================

    private void PrepareTextComponent(
        TMP_Text textComponent)
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.richText =
            true;
    }

    private void ApplyResponsiveText(
        TMP_Text textComponent,
        string rawText)
    {
        if (textComponent == null)
        {
            return;
        }

        string cleaned =
            NormalizeWrappingText(
                rawText
            );

        if (string.IsNullOrEmpty(
                cleaned
            ))
        {
            textComponent.text =
                string.Empty;

            return;
        }

        // 일본어 / 중국어는 TMP 기본 CJK 줄바꿈 규칙을 사용합니다.
        if (ContainsCjkWithoutHangul(
                cleaned
            ))
        {
            textComponent.enableWordWrapping =
                true;

            textComponent.text =
                cleaned;

            return;
        }

        // 한국어 / 영어는 우리가 공백 기준으로 직접 줄을 나눕니다.
        if (cleaned.Contains(
                " "
            ))
        {
            textComponent.enableWordWrapping =
                false;

            textComponent.text =
                WrapAtWhitespace(
                    textComponent,
                    cleaned
                );

            return;
        }

        // 공백이 전혀 없는 짧은 제목 등은 그대로 표시합니다.
        textComponent.enableWordWrapping =
            false;

        textComponent.text =
            cleaned;
    }

    private string WrapAtWhitespace(
        TMP_Text textComponent,
        string text)
    {
        if (textComponent == null ||
            string.IsNullOrWhiteSpace(
                text
            ))
        {
            return text;
        }

        // 레이아웃 계산을 최신 상태로 맞춥니다.
        Canvas.ForceUpdateCanvases();

        float availableWidth =
            textComponent.rectTransform.rect.width -
            textComponent.margin.x -
            textComponent.margin.z;

        // 아직 레이아웃이 잡히지 않은 경우에는
        // TMP 기본 줄바꿈으로 돌려보내기 위해 원문을 반환합니다.
        if (availableWidth <= 1f)
        {
            textComponent.enableWordWrapping =
                true;

            return text;
        }

        string[] words =
            text.Split(
                new[]
                {
                    ' '
                },
                StringSplitOptions.RemoveEmptyEntries
            );

        System.Text.StringBuilder result =
            new System.Text.StringBuilder();

        string currentLine =
            string.Empty;

        for (int i = 0;
             i < words.Length;
             i++)
        {
            string word =
                words[i];

            string candidate =
                string.IsNullOrEmpty(
                    currentLine
                )
                    ? word
                    : currentLine +
                      " " +
                      word;

            float candidateWidth =
                textComponent.GetPreferredValues(
                    candidate
                ).x;

            if (!string.IsNullOrEmpty(
                    currentLine
                ) &&
                candidateWidth >
                availableWidth)
            {
                if (result.Length >
                    0)
                {
                    result.Append(
                        '\n'
                    );
                }

                result.Append(
                    currentLine
                );

                currentLine =
                    word;
            }
            else
            {
                currentLine =
                    candidate;
            }
        }

        if (!string.IsNullOrEmpty(
                currentLine
            ))
        {
            if (result.Length >
                0)
            {
                result.Append(
                    '\n'
                );
            }

            result.Append(
                currentLine
            );
        }

        return result.ToString();
    }

    private string NormalizeWrappingText(
        string text)
    {
        if (string.IsNullOrEmpty(
                text
            ))
        {
            return string.Empty;
        }

        string cleaned =
            text
                .Replace(
                    "\r\n",
                    " "
                )
                .Replace(
                    "\n",
                    " "
                )
                .Replace(
                    "\r",
                    " "
                )
                // 이전 Word Joiner 실험이 문자열에 남아 있어도 제거
                .Replace(
                    "\u2060",
                    ""
                );

        while (cleaned.Contains(
                   "  "
               ))
        {
            cleaned =
                cleaned.Replace(
                    "  ",
                    " "
                );
        }

        return cleaned.Trim();
    }

    private bool ContainsCjkWithoutHangul(
        string text)
    {
        bool hasHangul =
            false;

        bool hasCjk =
            false;

        for (int i = 0;
             i < text.Length;
             i++)
        {
            char c =
                text[i];

            if ((c >= '\uAC00' &&
                 c <= '\uD7A3') ||
                (c >= '\u1100' &&
                 c <= '\u11FF') ||
                (c >= '\u3130' &&
                 c <= '\u318F'))
            {
                hasHangul =
                    true;

                continue;
            }

            if ((c >= '\u4E00' &&
                 c <= '\u9FFF') ||
                (c >= '\u3040' &&
                 c <= '\u309F') ||
                (c >= '\u30A0' &&
                 c <= '\u30FF'))
            {
                hasCjk =
                    true;
            }
        }

        return hasCjk &&
               !hasHangul;
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

    private void SetupTierBackCards()
    {
        if (choiceSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null)
            {
                continue;
            }

            EnsurePixelGlowLayers(
                slot.button
            );

            SetTierBackEnabled(
                slot.button,
                false
            );
        }
    }

    private PixelGlowLayers EnsurePixelGlowLayers(
        Button button)
    {
        if (button == null)
        {
            return null;
        }

        if (tierBackLayers.TryGetValue(
                button,
                out PixelGlowLayers cached
            ) &&
            cached != null &&
            cached.inner != null &&
            cached.middle != null &&
            cached.outer != null)
        {
            SyncPixelGlowTransforms(
                button,
                cached
            );

            return cached;
        }

        Image sourceImage =
            button.image;

        if (sourceImage == null)
        {
            sourceImage =
                button.GetComponent<Image>();
        }

        Transform parent =
            button.transform.parent;

        if (sourceImage == null ||
            parent == null)
        {
            return null;
        }

        PixelGlowLayers layers =
            new PixelGlowLayers();

        // 바깥 → 중간 → 안쪽 순서로 먼저 생성해서
        // 원본 카드 바로 뒤에 3단계 실루엣이 겹치도록 합니다.
        layers.outer =
            CreatePixelGlowImage(
                button,
                sourceImage,
                "_TierGlow_Outer",
                out layers.outerMaterial
            );

        layers.middle =
            CreatePixelGlowImage(
                button,
                sourceImage,
                "_TierGlow_Middle",
                out layers.middleMaterial
            );

        layers.inner =
            CreatePixelGlowImage(
                button,
                sourceImage,
                "_TierGlow_Inner",
                out layers.innerMaterial
            );

        tierBackLayers[button] =
            layers;

        SyncPixelGlowTransforms(
            button,
            layers
        );

        return layers;
    }

    private Image CreatePixelGlowImage(
        Button button,
        Image sourceImage,
        string suffix,
        out Material createdMaterial)
    {
        createdMaterial =
            null;

        Transform parent =
            button.transform.parent;

        GameObject glowObject =
            new GameObject(
                button.name + suffix,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );

        glowObject.transform.SetParent(
            parent,
            false
        );

        LayoutElement layoutElement =
            glowObject.GetComponent<LayoutElement>();

        layoutElement.ignoreLayout =
            true;

        Image glowImage =
            glowObject.GetComponent<Image>();

        glowImage.sprite =
            sourceImage.sprite;

        glowImage.type =
            sourceImage.type;

        glowImage.preserveAspect =
            sourceImage.preserveAspect;

        glowImage.fillCenter =
            sourceImage.fillCenter;

        glowImage.fillMethod =
            sourceImage.fillMethod;

        glowImage.fillOrigin =
            sourceImage.fillOrigin;

        glowImage.fillClockwise =
            sourceImage.fillClockwise;

        glowImage.fillAmount =
            sourceImage.fillAmount;

        glowImage.raycastTarget =
            false;

        if (tierBackShader != null)
        {
            createdMaterial =
                new Material(
                    tierBackShader
                );

            createdMaterial.name =
                button.name +
                suffix +
                "_Material";

            glowImage.material =
                createdMaterial;
        }
        else
        {
            glowImage.material =
                sourceImage.material;
        }

        return glowImage;
    }

    private void SyncPixelGlowTransforms(
        Button button,
        PixelGlowLayers layers,
        float spreadMultiplier = 1f)
    {
        if (button == null ||
            layers == null)
        {
            return;
        }

        RectTransform sourceRect =
            button.transform as RectTransform;

        if (sourceRect == null)
        {
            return;
        }

        SyncOneGlowTransform(
            sourceRect,
            layers.outer,
            GetSpreadScale(
                pixelGlowOuterScale,
                spreadMultiplier
            )
        );

        SyncOneGlowTransform(
            sourceRect,
            layers.middle,
            GetSpreadScale(
                pixelGlowMiddleScale,
                spreadMultiplier
            )
        );

        SyncOneGlowTransform(
            sourceRect,
            layers.inner,
            GetSpreadScale(
                pixelGlowInnerScale,
                spreadMultiplier
            )
        );

        // 원본 카드 바로 뒤에 세 겹을 정렬합니다.
        int sourceIndex =
            button.transform.GetSiblingIndex();

        if (layers.outer != null)
        {
            layers.outer.rectTransform.SetSiblingIndex(
                Mathf.Max(
                    0,
                    sourceIndex - 3
                )
            );
        }

        if (layers.middle != null)
        {
            layers.middle.rectTransform.SetSiblingIndex(
                Mathf.Max(
                    0,
                    sourceIndex - 2
                )
            );
        }

        if (layers.inner != null)
        {
            layers.inner.rectTransform.SetSiblingIndex(
                Mathf.Max(
                    0,
                    sourceIndex - 1
                )
            );
        }
    }

    private float GetSpreadScale(
        float baseScale,
        float spreadMultiplier)
    {
        float extra =
            Mathf.Max(
                0f,
                baseScale - 1f
            );

        return 1f +
               extra *
               Mathf.Max(
                   1f,
                   spreadMultiplier
               );
    }

    private void SyncOneGlowTransform(
        RectTransform sourceRect,
        Image glowImage,
        float scale)
    {
        if (sourceRect == null ||
            glowImage == null)
        {
            return;
        }

        RectTransform glowRect =
            glowImage.rectTransform;

        glowRect.anchorMin =
            sourceRect.anchorMin;

        glowRect.anchorMax =
            sourceRect.anchorMax;

        glowRect.pivot =
            sourceRect.pivot;

        glowRect.anchoredPosition =
            sourceRect.anchoredPosition;

        glowRect.sizeDelta =
            sourceRect.sizeDelta;

        glowRect.localRotation =
            sourceRect.localRotation;

        glowRect.localScale =
            sourceRect.localScale *
            scale;
    }

    private void ApplyTierBack(
        Button button,
        AugmentTier tier,
        float pulseValue = 0f)
    {
        PixelGlowLayers layers =
            EnsurePixelGlowLayers(
                button
            );

        if (layers == null)
        {
            return;
        }

        Color tierColor =
            GetTierColor(
                tier
            );

        float baseAlpha =
            GetTierBackAlpha(
                tier
            );

        float pulseStrength =
            GetTierPulseStrength(
                tier
            );

        float spreadMultiplier =
            tier == AugmentTier.T4
                ? t4SpreadMultiplier
                : 1f;

        // 각 티어마다 맥동 강도를 다르게 적용합니다.
        // T4는 기본적으로 중간/바깥 레이어가 더 강하고 더 멀리 퍼집니다.
        float innerAlpha =
            baseAlpha *
            Mathf.Lerp(
                innerGlowMin,
                innerGlowMax,
                pulseValue
            );

        float middleAlpha =
            baseAlpha *
            Mathf.Lerp(
                middleGlowMin,
                middleGlowMax,
                pulseValue
            ) *
            pulseStrength;

        float outerAlpha =
            baseAlpha *
            Mathf.Lerp(
                outerGlowMin,
                outerGlowMax,
                pulseValue
            ) *
            pulseStrength;

        ApplySolidTierColor(
            layers.inner,
            layers.innerMaterial,
            tierColor,
            innerAlpha
        );

        ApplySolidTierColor(
            layers.middle,
            layers.middleMaterial,
            tierColor,
            middleAlpha
        );

        ApplySolidTierColor(
            layers.outer,
            layers.outerMaterial,
            tierColor,
            outerAlpha
        );

        SetLayerActive(
            layers.inner,
            true
        );

        SetLayerActive(
            layers.middle,
            true
        );

        SetLayerActive(
            layers.outer,
            true
        );

        SyncPixelGlowTransforms(
            button,
            layers,
            spreadMultiplier
        );
    }

    private void ApplyTierBackWithBoost(
        Button button,
        AugmentTier tier,
        float pulseValue,
        float boost)
    {
        PixelGlowLayers layers =
            EnsurePixelGlowLayers(
                button
            );

        if (layers == null)
        {
            return;
        }

        Color tierColor =
            GetTierColor(
                tier
            );

        float baseAlpha =
            GetTierBackAlpha(
                tier
            );

        float pulseStrength =
            GetTierPulseStrength(
                tier
            ) *
            Mathf.Max(
                1f,
                boost
            );

        float spreadMultiplier =
            tier == AugmentTier.T4
                ? t4SpreadMultiplier
                : 1f;

        spreadMultiplier *=
            Mathf.Lerp(
                1f,
                1.12f,
                Mathf.Clamp01(
                    boost - 1f
                )
            );

        float innerAlpha =
            baseAlpha *
            Mathf.Lerp(
                innerGlowMin,
                innerGlowMax,
                pulseValue
            ) *
            Mathf.Lerp(
                1f,
                1.15f,
                Mathf.Clamp01(
                    boost - 1f
                )
            );

        float middleAlpha =
            baseAlpha *
            Mathf.Lerp(
                middleGlowMin,
                middleGlowMax,
                pulseValue
            ) *
            pulseStrength;

        float outerAlpha =
            baseAlpha *
            Mathf.Lerp(
                outerGlowMin,
                outerGlowMax,
                pulseValue
            ) *
            pulseStrength;

        ApplySolidTierColor(
            layers.inner,
            layers.innerMaterial,
            tierColor,
            innerAlpha
        );

        ApplySolidTierColor(
            layers.middle,
            layers.middleMaterial,
            tierColor,
            middleAlpha
        );

        ApplySolidTierColor(
            layers.outer,
            layers.outerMaterial,
            tierColor,
            outerAlpha
        );

        SetLayerActive(
            layers.inner,
            true
        );

        SetLayerActive(
            layers.middle,
            true
        );

        SetLayerActive(
            layers.outer,
            true
        );

        SyncPixelGlowTransforms(
            button,
            layers,
            spreadMultiplier
        );
    }

    private float GetTierPulseStrength(
        AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.T1:
                return t1PulseStrength;

            case AugmentTier.T2:
                return t2PulseStrength;

            case AugmentTier.T3:
                return t3PulseStrength;

            case AugmentTier.T4:
                return t4PulseStrength;

            default:
                return 1f;
        }
    }

    private void ApplySolidTierColor(
        Image image,
        Material material,
        Color tierColor,
        float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color solidColor =
            new Color(
                tierColor.r,
                tierColor.g,
                tierColor.b,
                Mathf.Clamp01(
                    alpha
                )
            );

        image.color =
            Color.white;

        if (material != null)
        {
            material.SetColor(
                "_TintColor",
                solidColor
            );
        }
        else
        {
            image.color =
                solidColor;
        }
    }

    private void SetLayerActive(
        Image image,
        bool enabled)
    {
        if (image == null)
        {
            return;
        }

        image.enabled =
            enabled;

        image.gameObject.SetActive(
            enabled
        );
    }

    private void SetTierBackEnabled(
        Button button,
        bool enabled)
    {
        if (button == null)
        {
            return;
        }

        PixelGlowLayers layers =
            EnsurePixelGlowLayers(
                button
            );

        if (layers == null)
        {
            return;
        }

        SetLayerActive(
            layers.inner,
            enabled
        );

        SetLayerActive(
            layers.middle,
            enabled
        );

        SetLayerActive(
            layers.outer,
            enabled
        );
    }

    private float GetTierBackAlpha(
        AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.T1:
                return t1BackAlpha;

            case AugmentTier.T2:
                return t2BackAlpha;

            case AugmentTier.T3:
                return t3BackAlpha;

            case AugmentTier.T4:
                return t4BackAlpha;

            default:
                return 0f;
        }
    }

    private void UpdateTierBackPulse()
    {
        if (choiceSlots == null ||
            displayedTiers == null)
        {
            return;
        }

        float pulseValue =
            0f;

        if (pulseTierBack)
        {
            float duration =
                Mathf.Max(
                    0.4f,
                    tierPulseDuration
                );

            float raw =
                (Mathf.Sin(
                    Time.unscaledTime /
                    duration *
                    Mathf.PI *
                    2f
                ) +
                1f) *
                0.5f;

            // 부드러운 값 그대로 쓰지 않고 단계화해서
            // 도트 애니메이션처럼 퍼졌다 줄어들게 합니다.
            int steps =
                Mathf.Max(
                    3,
                    pixelPulseSteps
                );

            pulseValue =
                Mathf.Round(
                    raw *
                    (steps - 1)
                ) /
                (steps - 1);
        }

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null ||
                !slot.button.gameObject.activeSelf ||
                i >= displayedTiers.Length)
            {
                continue;
            }

            ApplyTierBack(
                slot.button,
                displayedTiers[i],
                pulseValue
            );
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

    private void CacheCardBaseScales()
    {
        if (choiceSlots == null ||
            cardBaseScales == null)
        {
            return;
        }

        for (int i = 0;
             i < choiceSlots.Length &&
             i < cardBaseScales.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null)
            {
                cardBaseScales[i] =
                    Vector3.one;

                continue;
            }

            cardBaseScales[i] =
                slot.button.transform.localScale;
        }
    }

    private void StartCardRevealAnimation()
    {
        StopCardRevealAnimation(
            false
        );

        if (!useCardRevealAnimation)
        {
            ResetCardRevealState();
            return;
        }

        cardRevealCoroutine =
            StartCoroutine(
                CardRevealRoutine()
            );
    }

    private IEnumerator CardRevealRoutine()
    {
        if (choiceSlots == null)
        {
            yield break;
        }

        // 먼저 전부 자기 차례 전 상태로 둡니다.
        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null ||
                !slot.button.gameObject.activeSelf)
            {
                continue;
            }

            slot.button.interactable =
                false;

            Vector3 baseScale =
                GetCardBaseScale(
                    i
                );

            float initialScale =
                hideUntilRevealTurn
                    ? 0.001f
                    : cardRevealStartScale;

            slot.button.transform.localScale =
                baseScale *
                initialScale;
        }

        yield return null;

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null ||
                !slot.button.gameObject.activeSelf)
            {
                continue;
            }

            Vector3 baseScale =
                GetCardBaseScale(
                    i
                );

            slot.button.transform.localScale =
                baseScale *
                cardRevealStartScale;

            float elapsed =
                0f;

            float duration =
                Mathf.Max(
                    0.08f,
                    cardRevealDuration
                );

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                int steps =
                    Mathf.Max(
                        3,
                        cardRevealSteps
                    );

                float steppedT =
                    Mathf.Round(
                        t *
                        (steps - 1)
                    ) /
                    (steps - 1);

                float scaleMultiplier =
                    EvaluateRevealScale(
                        steppedT
                    );

                slot.button.transform.localScale =
                    baseScale *
                    scaleMultiplier;

                yield return null;
            }

            slot.button.transform.localScale =
                baseScale;

            slot.button.interactable =
                true;

            if (cardRevealStagger > 0f &&
                i < choiceSlots.Length - 1)
            {
                float wait =
                    0f;

                while (wait <
                       cardRevealStagger)
                {
                    wait +=
                        Time.unscaledDeltaTime;

                    yield return null;
                }
            }
        }

        cardRevealCoroutine =
            null;
    }

    private float EvaluateRevealScale(
        float t)
    {
        // 75% 지점까지 살짝 크게 튀어나온 뒤
        // 마지막 25%에서 원래 크기로 정착합니다.
        const float overshootPoint =
            0.75f;

        if (t <= overshootPoint)
        {
            float localT =
                Mathf.Clamp01(
                    t /
                    overshootPoint
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - localT,
                    3f
                );

            return Mathf.Lerp(
                cardRevealStartScale,
                cardRevealOvershootScale,
                eased
            );
        }

        float settleT =
            Mathf.InverseLerp(
                overshootPoint,
                1f,
                t
            );

        return Mathf.Lerp(
            cardRevealOvershootScale,
            1f,
            settleT
        );
    }

    private Vector3 GetCardBaseScale(
        int index)
    {
        if (cardBaseScales == null ||
            index < 0 ||
            index >= cardBaseScales.Length ||
            cardBaseScales[index] ==
            Vector3.zero)
        {
            return Vector3.one;
        }

        return cardBaseScales[index];
    }

    private void StopCardRevealAnimation(
        bool reset)
    {
        if (cardRevealCoroutine != null)
        {
            StopCoroutine(
                cardRevealCoroutine
            );

            cardRevealCoroutine =
                null;
        }

        if (reset)
        {
            ResetCardRevealState();
        }
    }

    private void ResetCardRevealState()
    {
        if (choiceSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null)
            {
                continue;
            }

            slot.button.transform.localScale =
                GetCardBaseScale(
                    i
                );

            slot.button.interactable =
                true;
        }
    }

    private void StopCardSelectAnimation(
        bool reset)
    {
        if (cardSelectCoroutine != null)
        {
            StopCoroutine(
                cardSelectCoroutine
            );

            cardSelectCoroutine =
                null;
        }

        isSelecting =
            false;

        if (!reset ||
            choiceSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < choiceSlots.Length;
             i++)
        {
            ChoiceSlot slot =
                choiceSlots[i];

            if (slot == null ||
                slot.button == null)
            {
                continue;
            }

            slot.button.transform.localScale =
                GetCardBaseScale(
                    i
                );

            slot.button.interactable =
                true;

            CanvasGroup group =
                slot.button.GetComponent<CanvasGroup>();

            if (group != null)
            {
                group.alpha =
                    1f;

                group.interactable =
                    true;

                group.blocksRaycasts =
                    true;
            }
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

    private void CloseSelection()
    {
        isOpen = false;

        StopCardRevealAnimation(
            true
        );

        StopCardSelectAnimation(
            true
        );

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

    private void OnDestroy()
    {
        StopCardRevealAnimation(
            false
        );

        StopCardSelectAnimation(
            false
        );

        foreach (KeyValuePair<Button, PixelGlowLayers> pair
                 in tierBackLayers)
        {
            PixelGlowLayers layers =
                pair.Value;

            if (layers == null)
            {
                continue;
            }

            DestroyRuntimeMaterial(
                layers.innerMaterial
            );

            DestroyRuntimeMaterial(
                layers.middleMaterial
            );

            DestroyRuntimeMaterial(
                layers.outerMaterial
            );
        }

        tierBackLayers.Clear();
    }

    private void DestroyRuntimeMaterial(
        Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(
                material
            );
        }
        else
        {
            DestroyImmediate(
                material
            );
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
        focusedSummonWeight =
            Mathf.Max(
                1f,
                focusedSummonWeight
            );

        t1BackAlpha =
            Mathf.Clamp01(
                t1BackAlpha
            );

        t2BackAlpha =
            Mathf.Clamp01(
                t2BackAlpha
            );

        t3BackAlpha =
            Mathf.Clamp01(
                t3BackAlpha
            );

        t4BackAlpha =
            Mathf.Clamp01(
                t4BackAlpha
            );

        pixelGlowInnerScale =
            Mathf.Max(
                1f,
                pixelGlowInnerScale
            );

        pixelGlowMiddleScale =
            Mathf.Max(
                pixelGlowInnerScale,
                pixelGlowMiddleScale
            );

        pixelGlowOuterScale =
            Mathf.Max(
                pixelGlowMiddleScale,
                pixelGlowOuterScale
            );

        tierPulseDuration =
            Mathf.Max(
                0.4f,
                tierPulseDuration
            );

        pixelPulseSteps =
            Mathf.Clamp(
                pixelPulseSteps,
                3,
                16
            );

        innerGlowMin =
            Mathf.Max(
                0f,
                innerGlowMin
            );

        innerGlowMax =
            Mathf.Max(
                innerGlowMin,
                innerGlowMax
            );

        middleGlowMin =
            Mathf.Max(
                0f,
                middleGlowMin
            );

        middleGlowMax =
            Mathf.Max(
                middleGlowMin,
                middleGlowMax
            );

        outerGlowMin =
            Mathf.Max(
                0f,
                outerGlowMin
            );

        outerGlowMax =
            Mathf.Max(
                outerGlowMin,
                outerGlowMax
            );

        t1PulseStrength =
            Mathf.Clamp(
                t1PulseStrength,
                0.5f,
                2f
            );

        t2PulseStrength =
            Mathf.Clamp(
                t2PulseStrength,
                0.5f,
                2f
            );

        t3PulseStrength =
            Mathf.Clamp(
                t3PulseStrength,
                0.5f,
                2f
            );

        t4PulseStrength =
            Mathf.Clamp(
                t4PulseStrength,
                0.5f,
                2.5f
            );

        t4SpreadMultiplier =
            Mathf.Clamp(
                t4SpreadMultiplier,
                1f,
                2f
            );

        cardRevealDuration =
            Mathf.Max(
                0.08f,
                cardRevealDuration
            );

        cardRevealStagger =
            Mathf.Max(
                0f,
                cardRevealStagger
            );

        cardRevealStartScale =
            Mathf.Clamp(
                cardRevealStartScale,
                0.2f,
                1f
            );

        cardRevealOvershootScale =
            Mathf.Max(
                1f,
                cardRevealOvershootScale
            );

        cardRevealSteps =
            Mathf.Clamp(
                cardRevealSteps,
                3,
                20
            );

        cardSelectDuration =
            Mathf.Max(
                0.12f,
                cardSelectDuration
            );

        selectedCardPopScale =
            Mathf.Max(
                1f,
                selectedCardPopScale
            );

        unselectedCardScale =
            Mathf.Clamp(
                unselectedCardScale,
                0.6f,
                1f
            );

        unselectedCardAlpha =
            Mathf.Clamp01(
                unselectedCardAlpha
            );

        selectedCardEndAlpha =
            Mathf.Clamp(
                selectedCardEndAlpha,
                0.5f,
                1f
            );

        cardSelectSteps =
            Mathf.Clamp(
                cardSelectSteps,
                3,
                20
            );

        selectedGlowBoost =
            Mathf.Clamp(
                selectedGlowBoost,
                1f,
                2.5f
            );


        pixelFadeCoverDuration =
            Mathf.Max(
                0.05f,
                pixelFadeCoverDuration
            );

        pixelFadeHoldDuration =
            Mathf.Max(
                0f,
                pixelFadeHoldDuration
            );

        pixelFadeRevealDuration =
            Mathf.Max(
                0.05f,
                pixelFadeRevealDuration
            );

        pixelFadeGridWidth =
            Mathf.Clamp(
                pixelFadeGridWidth,
                24,
                160
            );

        pixelFadeGridHeight =
            Mathf.Clamp(
                pixelFadeGridHeight,
                14,
                90
            );
    }
#endif
}
