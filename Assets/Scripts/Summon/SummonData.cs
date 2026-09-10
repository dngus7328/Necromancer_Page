using UnityEngine;

[CreateAssetMenu(
    fileName = "NewSummonData",
    menuName = "Necromancer Page/Summon Data"
)]
public class SummonData : ScriptableObject
{
    // =========================================================
    // 기본 정보
    // =========================================================

    [Header("기본 정보")]
    [SerializeField]
    private string summonName;

    // =========================================================
    // HUD 슬롯
    // =========================================================

    [Header("HUD 슬롯")]

    [Tooltip(
        "해당 소환수가 장착됐을 때 HUD 슬롯에 표시할 카드 프리팹"
    )]
    [SerializeField]
    private GameObject slotVisualPrefab;

    // =========================================================
    // 실제 소환수
    // =========================================================

    [Header("실제 소환수")]
    [SerializeField]
    private GameObject summonPrefab;

    // =========================================================
    // 배치 프리뷰
    // =========================================================

    [Header("배치 프리뷰")]
    [SerializeField]
    private Sprite previewSprite;

    [Min(0.1f)]
    [SerializeField]
    private float previewCircleScale = 1f;

    // =========================================================
    // 소환진
    // =========================================================

    [Header("소환진")]
    [SerializeField]
    private Sprite summonCircleSprite;

    // =========================================================
    // 소환 구성
    // =========================================================

    [Header("소환 구성")]
    [SerializeField]
    private SummonBodySize summonBodySize =
        SummonBodySize.Medium;

    [Min(1)]
    [SerializeField]
    private int spawnCount = 1;

    // =========================================================
    // 비용
    // =========================================================

    [Header("소환 비용")]

    [Min(0f)]
    [SerializeField]
    private float manaCost = 20f;

    [Min(0)]
    [SerializeField]
    private int capacityCost = 1;

    [Header("쿨타임")]

    [Min(0f)]
    [SerializeField]
    private float cooldown = 8f;

    // =========================================================
    // 이동 형태
    // =========================================================

    [Header("이동 형태")]
    [SerializeField]
    private SummonMovementType movementType =
        SummonMovementType.Ground;

    // =========================================================
    // 공중 / 부유
    // =========================================================

    [Header("공중 / 부유 높이")]

    [Min(0f)]
    [SerializeField]
    private float hoverHeight = 0.35f;

    [Min(0f)]
    [SerializeField]
    private float hoverAmplitude = 0.06f;

    [Min(0f)]
    [SerializeField]
    private float hoverSpeed = 2.4f;

    // =========================================================
    // 그림자
    // =========================================================

    [Header("공중형 그림자")]

    [SerializeField]
    private Sprite shadowSprite;

    [SerializeField]
    private Vector2 shadowOffset =
        new Vector2(0f, -0.05f);

    [Min(0.01f)]
    [SerializeField]
    private float shadowBaseScale = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float shadowBaseAlpha = 0.35f;

    [Range(0f, 1f)]
    [SerializeField]
    private float shadowScaleVariation = 0.12f;

    [Range(0f, 1f)]
    [SerializeField]
    private float shadowAlphaVariation = 0.1f;

    // =========================================================
    // 공격 몸동작
    // =========================================================

    [Header("공격 몸동작")]

    [SerializeField]
    private SummonAttackMotionType attackMotionType =
        SummonAttackMotionType.None;

    [Min(0f)]
    [SerializeField]
    private float attackBackstepDistance = 0.2f;

    [Min(0f)]
    [SerializeField]
    private float attackForwardDistance = 0.4f;

    [Min(0.01f)]
    [SerializeField]
    private float attackBackstepTime = 0.12f;

    [Min(0.01f)]
    [SerializeField]
    private float attackForwardTime = 0.10f;

    [Min(0.01f)]
    [SerializeField]
    private float attackReturnTime = 0.16f;

    // =========================================================
    // 사망 연출
    // =========================================================

    [Header("사망 연출")]

    [SerializeField]
    private SummonDeathMotionType deathMotionType =
        SummonDeathMotionType.Ground;

    [Min(0.01f)]
    [SerializeField]
    private float fallDeathTime = 0.32f;

    [Min(0f)]
    [SerializeField]
    private float floatingDeathShake = 0.08f;

    // =========================================================
    // Properties
    // =========================================================

    public string SummonName =>
        summonName;

    public GameObject SlotVisualPrefab =>
        slotVisualPrefab;

    public GameObject SummonPrefab =>
        summonPrefab;

    public Sprite PreviewSprite =>
        previewSprite;

    public float PreviewCircleScale =>
        Mathf.Max(
            0.1f,
            previewCircleScale
        );

    public Sprite SummonCircleSprite =>
        summonCircleSprite;

    public SummonBodySize SummonBodySize =>
        summonBodySize;

    public int SpawnCount =>
        Mathf.Max(
            1,
            spawnCount
        );

    public float ManaCost =>
        Mathf.Max(
            0f,
            manaCost
        );

    public int CapacityCost =>
        Mathf.Max(
            0,
            capacityCost
        );

    public float Cooldown =>
        Mathf.Max(
            0f,
            cooldown
        );

    public SummonMovementType MovementType =>
        movementType;

    public float HoverHeight =>
        hoverHeight;

    public float HoverAmplitude =>
        hoverAmplitude;

    public float HoverSpeed =>
        hoverSpeed;

    public Sprite ShadowSprite =>
        shadowSprite;

    public Vector2 ShadowOffset =>
        shadowOffset;

    public float ShadowBaseScale =>
        shadowBaseScale;

    public float ShadowBaseAlpha =>
        shadowBaseAlpha;

    public float ShadowScaleVariation =>
        shadowScaleVariation;

    public float ShadowAlphaVariation =>
        shadowAlphaVariation;

    public SummonAttackMotionType AttackMotionType =>
        attackMotionType;

    public float AttackBackstepDistance =>
        attackBackstepDistance;

    public float AttackForwardDistance =>
        attackForwardDistance;

    public float AttackBackstepTime =>
        attackBackstepTime;

    public float AttackForwardTime =>
        attackForwardTime;

    public float AttackReturnTime =>
        attackReturnTime;

    public SummonDeathMotionType DeathMotionType =>
        deathMotionType;

    public float FallDeathTime =>
        fallDeathTime;

    public float FloatingDeathShake =>
        floatingDeathShake;

#if UNITY_EDITOR

    private void OnValidate()
    {
        spawnCount =
            Mathf.Max(
                1,
                spawnCount
            );

        previewCircleScale =
            Mathf.Max(
                0.1f,
                previewCircleScale
            );

        manaCost =
            Mathf.Max(
                0f,
                manaCost
            );

        capacityCost =
            Mathf.Max(
                0,
                capacityCost
            );

        cooldown =
            Mathf.Max(
                0f,
                cooldown
            );

        hoverHeight =
            Mathf.Max(
                0f,
                hoverHeight
            );

        hoverAmplitude =
            Mathf.Max(
                0f,
                hoverAmplitude
            );

        hoverSpeed =
            Mathf.Max(
                0f,
                hoverSpeed
            );

        shadowBaseScale =
            Mathf.Max(
                0.01f,
                shadowBaseScale
            );

        attackBackstepDistance =
            Mathf.Max(
                0f,
                attackBackstepDistance
            );

        attackForwardDistance =
            Mathf.Max(
                0f,
                attackForwardDistance
            );

        attackBackstepTime =
            Mathf.Max(
                0.01f,
                attackBackstepTime
            );

        attackForwardTime =
            Mathf.Max(
                0.01f,
                attackForwardTime
            );

        attackReturnTime =
            Mathf.Max(
                0.01f,
                attackReturnTime
            );

        fallDeathTime =
            Mathf.Max(
                0.01f,
                fallDeathTime
            );

        floatingDeathShake =
            Mathf.Max(
                0f,
                floatingDeathShake
            );
    }

#endif
}