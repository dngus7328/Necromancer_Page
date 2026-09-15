using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RuntimeBuffVisualType
{
    Generic,
    Health,
    Damage,
    Cooldown,
    Mana,
    Shield,
    Speed,
    Spirit
}

public class RuntimePixelEffectSystem : MonoBehaviour
{
    public static RuntimePixelEffectSystem Instance
    {
        get;
        private set;
    }

    // =========================================================
    // 정렬
    // =========================================================

    [Header("정렬")]

    [Tooltip(
        "기준 캐릭터보다 이펙트를 얼마나 앞에 표시할지 결정합니다."
    )]
    [SerializeField]
    private int sortingOrderOffset = 100;

    // =========================================================
    // 도트 움직임
    // =========================================================

    [Header("도트 움직임")]

    [Tooltip(
        "이펙트 이동과 크기 변화를 몇 단계로 끊어서 표현할지 결정합니다."
    )]
    [Range(3, 16)]
    [SerializeField]
    private int defaultStepCount = 6;

    // =========================================================
    // 버프 표시
    // =========================================================

    [Header("버프 표시")]

    [Range(1, 8)]
    [SerializeField]
    private int buffIconCount = 4;

    [Min(0f)]
    [SerializeField]
    private float buffSpawnInterval = 0.11f;

    [Min(0.05f)]
    [SerializeField]
    private float buffLifetime = 0.46f;

    [SerializeField]
    private float buffRiseDistance = 0.42f;

    [Min(0.01f)]
    [SerializeField]
    private float buffScale = 0.60f;

    [SerializeField]
    private float buffHorizontalSpread = 0.55f;

    [SerializeField]
    private float buffVerticalOffset = 0.25f;

    // =========================================================
    // Runtime
    // =========================================================

    private readonly HashSet<RuntimePixelSpriteType>
        missingSpriteWarnings =
            new HashSet<RuntimePixelSpriteType>();

    // =========================================================
    // 생성 / Singleton
    // =========================================================

    public static RuntimePixelEffectSystem GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RuntimePixelEffectSystem existing =
            FindObjectOfType<RuntimePixelEffectSystem>();

        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject obj =
            new GameObject(
                "RuntimePixelEffectSystem"
            );

        Instance =
            obj.AddComponent<RuntimePixelEffectSystem>();

        return Instance;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // 버프
    // =========================================================

    public void PlayBuff(
        Transform target,
        RuntimeBuffVisualType visualType)
    {
        if (target == null)
        {
            return;
        }

        GetBuffStyle(
            visualType,
            out RuntimePixelSpriteType spriteType,
            out Color color
        );

        StartCoroutine(
            PlayBuffRoutine(
                target,
                spriteType,
                color
            )
        );
    }

    private IEnumerator PlayBuffRoutine(
        Transform target,
        RuntimePixelSpriteType spriteType,
        Color color)
    {
        int count =
            Mathf.Max(
                1,
                buffIconCount
            );

        for (int i = 0;
             i < count;
             i++)
        {
            if (target == null)
            {
                yield break;
            }

            float normalized =
                count <= 1
                    ? 0.5f
                    : i /
                      (float)(count - 1);

            float x =
                Mathf.Lerp(
                    -buffHorizontalSpread,
                    buffHorizontalSpread,
                    normalized
                );

            x +=
                Random.Range(
                    -0.05f,
                    0.05f
                );

            float y =
                buffVerticalOffset +
                Random.Range(
                    -0.04f,
                    0.04f
                );

            // 캐릭터 Transform 중심이 아니라
            // 실제 보이는 Sprite 중심 사용
            Vector3 visualCenter =
                ResolveVisualCenter(
                    target
                );

            Vector3 worldPosition =
                visualCenter +
                new Vector3(
                    x,
                    y,
                    0f
                );

            Spawn(
                spriteType,
                color,
                worldPosition,
                target,
                worldPosition -
                target.position,
                true,
                buffLifetime,
                buffRiseDistance,
                buffScale,
                buffScale * 1.15f,
                true,
                ResolveSortingLayerId(
                    target
                ),
                ResolveSortingOrder(
                    target,
                    20
                )
            );

            float wait = 0f;

            while (wait <
                   buffSpawnInterval)
            {
                wait +=
                    Time.unscaledDeltaTime;

                yield return null;
            }
        }
    }

    private void GetBuffStyle(
        RuntimeBuffVisualType visualType,
        out RuntimePixelSpriteType spriteType,
        out Color color)
    {
        switch (visualType)
        {
            case RuntimeBuffVisualType.Health:

                spriteType =
                    RuntimePixelSpriteType.ArrowHealth;

                color =
                    new Color(
                        0.55f,
                        1f,
                        0.60f,
                        1f
                    );

                break;

            case RuntimeBuffVisualType.Damage:

                spriteType =
                    RuntimePixelSpriteType.ArrowDamage;

                color =
                    new Color(
                        1f,
                        0.48f,
                        0.30f,
                        1f
                    );

                break;

            case RuntimeBuffVisualType.Cooldown:

                spriteType =
                    RuntimePixelSpriteType.ArrowCooldown;

                color =
                    new Color(
                        0.42f,
                        0.86f,
                        1f,
                        1f
                    );

                break;

            case RuntimeBuffVisualType.Mana:

                spriteType =
                    RuntimePixelSpriteType.ArrowMana;

                color =
                    new Color(
                        0.58f,
                        0.52f,
                        1f,
                        1f
                    );

                break;

            case RuntimeBuffVisualType.Shield:

                spriteType =
                    RuntimePixelSpriteType.ArrowShield;

                color =
                    new Color(
                        0.72f,
                        0.67f,
                        1f,
                        1f
                    );

                break;

            case RuntimeBuffVisualType.Speed:

                spriteType =
                    RuntimePixelSpriteType.ArrowSpeed;

                color =
                    new Color(
                        0.45f,
                        0.95f,
                        0.95f,
                        1f
                    );

                break;

            case RuntimeBuffVisualType.Spirit:

                spriteType =
                    RuntimePixelSpriteType.ArrowSpirit;

                color =
                    new Color(
                        0.80f,
                        0.52f,
                        1f,
                        1f
                    );

                break;

            default:

                spriteType =
                    RuntimePixelSpriteType.ArrowGeneric;

                color =
                    new Color(
                        0.86f,
                        0.64f,
                        1f,
                        1f
                    );

                break;
        }
    }

    // =========================================================
    // 회복
    // =========================================================

    public void PlayHeal(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 visualCenter =
            ResolveVisualCenter(
                target
            );

        for (int i = 0;
             i < 3;
             i++)
        {
            float x =
                Random.Range(
                    -0.18f,
                    0.18f
                );

            float y =
                0.15f +
                i * 0.08f;

            Vector3 worldPosition =
                visualCenter +
                new Vector3(
                    x,
                    y,
                    0f
                );

            Spawn(
                RuntimePixelSpriteType.HealCross,
                new Color(
                    0.60f,
                    1f,
                    0.60f,
                    1f
                ),
                worldPosition,
                target,
                worldPosition -
                target.position,
                true,
                0.45f,
                0.35f,
                0.50f,
                0.80f,
                true,
                ResolveSortingLayerId(
                    target
                ),
                ResolveSortingOrder(
                    target,
                    30
                )
            );
        }
    }

    // =========================================================
    // 보호막 획득
    // =========================================================

    public void PlayShieldGain(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 visualCenter =
            ResolveVisualCenter(
                target
            );

        Vector3 ringPosition =
            visualCenter +
            Vector3.up * 0.10f;

        Spawn(
            RuntimePixelSpriteType.ShieldRing,
            new Color(
                0.72f,
                0.65f,
                1f,
                0.95f
            ),
            ringPosition,
            target,
            ringPosition -
            target.position,
            true,
            0.42f,
            0.05f,
            0.60f,
            1.10f,
            true,
            ResolveSortingLayerId(
                target
            ),
            ResolveSortingOrder(
                target,
                15
            )
        );

        for (int i = 0;
             i < 4;
             i++)
        {
            float x =
                Random.Range(
                    -0.25f,
                    0.25f
                );

            float y =
                Random.Range(
                    -0.05f,
                    0.25f
                );

            Vector3 worldPosition =
                visualCenter +
                new Vector3(
                    x,
                    y,
                    0f
                );

            Spawn(
                RuntimePixelSpriteType.ShieldShard,
                new Color(
                    0.82f,
                    0.76f,
                    1f,
                    1f
                ),
                worldPosition,
                target,
                worldPosition -
                target.position,
                true,
                0.38f,
                0.22f,
                0.40f,
                0.65f,
                true,
                ResolveSortingLayerId(
                    target
                ),
                ResolveSortingOrder(
                    target,
                    18
                )
            );
        }
    }

    // =========================================================
    // 보호막 파괴
    // =========================================================

    public void PlayShieldBreak(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 visualCenter =
            ResolveVisualCenter(
                target
            );

        for (int i = 0;
             i < 6;
             i++)
        {
            float x =
                Random.Range(
                    -0.30f,
                    0.30f
                );

            float y =
                Random.Range(
                    -0.10f,
                    0.25f
                );

            Spawn(
                RuntimePixelSpriteType.ShieldShard,
                new Color(
                    0.92f,
                    0.86f,
                    1f,
                    1f
                ),
                visualCenter +
                new Vector3(
                    x,
                    y,
                    0f
                ),
                null,
                Vector3.zero,
                false,
                0.35f,
                0.28f,
                0.40f,
                0.62f,
                true,
                ResolveSortingLayerId(
                    target
                ),
                ResolveSortingOrder(
                    target,
                    18
                )
            );
        }
    }

    // =========================================================
    // 소환
    // =========================================================

    public void PlaySummonBurst(
        Vector3 worldPosition,
        Transform sortingReference = null)
    {
        worldPosition =
            SanitizePosition(
                worldPosition,
                sortingReference
            );

        int layerId =
            ResolveSortingLayerId(
                sortingReference
            );

        int order =
            ResolveSortingOrder(
                sortingReference,
                5
            );

        Spawn(
            RuntimePixelSpriteType.RingLarge,
            new Color(
                0.82f,
                0.52f,
                1f,
                0.95f
            ),
            worldPosition,
            null,
            Vector3.zero,
            false,
            0.45f,
            0.02f,
            0.60f,
            1.15f,
            true,
            layerId,
            order
        );

        for (int i = 0;
             i < 6;
             i++)
        {
            float x =
                Random.Range(
                    -0.35f,
                    0.35f
                );

            float y =
                Random.Range(
                    -0.10f,
                    0.20f
                );

            Spawn(
                RuntimePixelSpriteType.RuneShard,
                new Color(
                    0.78f,
                    0.46f,
                    1f,
                    1f
                ),
                worldPosition +
                new Vector3(
                    x,
                    y,
                    0f
                ),
                null,
                Vector3.zero,
                false,
                0.42f,
                0.30f,
                0.40f,
                0.65f,
                true,
                layerId,
                order + 1
            );
        }
    }

    // =========================================================
    // 집결
    // =========================================================

    public void PlayRallyPulse(
        Vector3 worldPosition,
        Transform sortingReference = null)
    {
        worldPosition =
            SanitizePosition(
                worldPosition,
                sortingReference
            );

        int layerId =
            ResolveSortingLayerId(
                sortingReference
            );

        int order =
            ResolveSortingOrder(
                sortingReference,
                5
            );

        Spawn(
            RuntimePixelSpriteType.RingLarge,
            new Color(
                0.35f,
                0.95f,
                1f,
                0.95f
            ),
            worldPosition,
            null,
            Vector3.zero,
            false,
            0.35f,
            0.02f,
            0.55f,
            1.00f,
            true,
            layerId,
            order
        );

        for (int i = 0;
             i < 4;
             i++)
        {
            float x =
                Random.Range(
                    -0.25f,
                    0.25f
                );

            float y =
                Random.Range(
                    -0.05f,
                    0.15f
                );

            Spawn(
                RuntimePixelSpriteType.Diamond,
                new Color(
                    0.42f,
                    0.96f,
                    1f,
                    1f
                ),
                worldPosition +
                new Vector3(
                    x,
                    y,
                    0f
                ),
                null,
                Vector3.zero,
                false,
                0.30f,
                0.20f,
                0.32f,
                0.52f,
                true,
                layerId,
                order + 1
            );
        }
    }

    // =========================================================
    // 피격 먼지
    // =========================================================

    public void PlayAllyHitDust(
        Vector3 worldPosition,
        Transform sortingReference = null)
    {
        worldPosition =
            SanitizePosition(
                worldPosition,
                sortingReference
            );

        int layerId =
            ResolveSortingLayerId(
                sortingReference
            );

        int order =
            ResolveSortingOrder(
                sortingReference,
                2
            );

        for (int i = 0;
             i < 3;
             i++)
        {
            float x =
                Random.Range(
                    -0.20f,
                    0.20f
                );

            Spawn(
                RuntimePixelSpriteType.DustPuff,
                new Color(
                    0.88f,
                    0.82f,
                    0.76f,
                    0.95f
                ),
                worldPosition +
                new Vector3(
                    x,
                    0f,
                    0f
                ),
                null,
                Vector3.zero,
                false,
                0.30f,
                0.12f,
                0.32f,
                0.55f,
                true,
                layerId,
                order
            );
        }
    }

    // =========================================================
    // 스파크
    // =========================================================

    public void PlaySparkBurst(
        Vector3 worldPosition,
        Transform sortingReference = null)
    {
        worldPosition =
            SanitizePosition(
                worldPosition,
                sortingReference
            );

        int layerId =
            ResolveSortingLayerId(
                sortingReference
            );

        int order =
            ResolveSortingOrder(
                sortingReference,
                10
            );

        for (int i = 0;
             i < 4;
             i++)
        {
            float x =
                Random.Range(
                    -0.18f,
                    0.18f
                );

            float y =
                Random.Range(
                    -0.05f,
                    0.18f
                );

            Spawn(
                RuntimePixelSpriteType.Spark,
                new Color(
                    1f,
                    0.95f,
                    0.65f,
                    1f
                ),
                worldPosition +
                new Vector3(
                    x,
                    y,
                    0f
                ),
                null,
                Vector3.zero,
                false,
                0.26f,
                0.16f,
                0.28f,
                0.50f,
                true,
                layerId,
                order
            );
        }
    }

    // =========================================================
    // 충격파
    // =========================================================

    public void PlayShockwave(
        Vector3 worldPosition,
        Transform sortingReference = null)
    {
        worldPosition =
            SanitizePosition(
                worldPosition,
                sortingReference
            );

        int layer =
            ResolveSortingLayerId(
                sortingReference
            );

        int order =
            ResolveSortingOrder(
                sortingReference,
                8
            );

        Spawn(
            RuntimePixelSpriteType.RingSmall,
            new Color(
                1f,
                1f,
                1f,
                0.90f
            ),
            worldPosition,
            null,
            Vector3.zero,
            false,
            0.24f,
            0.02f,
            0.45f,
            1.00f,
            true,
            layer,
            order
        );

        Spawn(
            RuntimePixelSpriteType.GroundCrack,
            new Color(
                0.88f,
                0.82f,
                0.74f,
                0.90f
            ),
            worldPosition +
            Vector3.down * 0.05f,
            null,
            Vector3.zero,
            false,
            0.30f,
            0f,
            0.55f,
            0.75f,
            true,
            layer,
            order - 1
        );
    }

    // =========================================================
    // 유령
    // =========================================================

    public void PlayGhostBurst(
        Vector3 worldPosition,
        Transform sortingReference = null)
    {
        worldPosition =
            SanitizePosition(
                worldPosition,
                sortingReference
            );

        int layerId =
            ResolveSortingLayerId(
                sortingReference
            );

        int order =
            ResolveSortingOrder(
                sortingReference,
                12
            );

        Spawn(
            RuntimePixelSpriteType.GhostOrb,
            new Color(
                0.64f,
                0.48f,
                1f,
                0.95f
            ),
            worldPosition,
            null,
            Vector3.zero,
            false,
            0.28f,
            0.12f,
            0.45f,
            0.80f,
            true,
            layerId,
            order
        );

        for (int i = 0;
             i < 4;
             i++)
        {
            float x =
                Random.Range(
                    -0.24f,
                    0.24f
                );

            float y =
                Random.Range(
                    -0.10f,
                    0.18f
                );

            Spawn(
                RuntimePixelSpriteType.GhostTrail,
                new Color(
                    0.76f,
                    0.62f,
                    1f,
                    0.85f
                ),
                worldPosition +
                new Vector3(
                    x,
                    y,
                    0f
                ),
                null,
                Vector3.zero,
                false,
                0.32f,
                0.22f,
                0.30f,
                0.48f,
                true,
                layerId,
                order - 1
            );
        }
    }

    // =========================================================
    // 뼈 파편
    // =========================================================

    public void PlayBoneBurst(
        Vector3 worldPosition,
        Transform sortingReference = null)
    {
        worldPosition =
            SanitizePosition(
                worldPosition,
                sortingReference
            );

        int layerId =
            ResolveSortingLayerId(
                sortingReference
            );

        int order =
            ResolveSortingOrder(
                sortingReference,
                10
            );

        for (int i = 0;
             i < 6;
             i++)
        {
            float x =
                Random.Range(
                    -0.30f,
                    0.30f
                );

            float y =
                Random.Range(
                    -0.08f,
                    0.18f
                );

            Spawn(
                RuntimePixelSpriteType.BoneShard,
                new Color(
                    0.90f,
                    0.86f,
                    0.76f,
                    1f
                ),
                worldPosition +
                new Vector3(
                    x,
                    y,
                    0f
                ),
                null,
                Vector3.zero,
                false,
                0.36f,
                0.28f,
                0.34f,
                0.55f,
                true,
                layerId,
                order
            );
        }
    }

    // =========================================================
    // 해골 표식
    // =========================================================

    public void PlaySkullMark(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        // 캐릭터 루트 기준 +0.8이 아니라
        // 실제 Sprite 맨 위 기준
        Vector3 worldPosition =
            ResolveVisualTop(
                target
            ) +
            Vector3.up * 0.12f;

        Spawn(
            RuntimePixelSpriteType.SkullMark,
            new Color(
                0.82f,
                0.62f,
                1f,
                1f
            ),
            worldPosition,
            target,
            worldPosition -
            target.position,
            true,
            0.55f,
            0.24f,
            0.42f,
            0.62f,
            true,
            ResolveSortingLayerId(
                target
            ),
            ResolveSortingOrder(
                target,
                25
            )
        );
    }

    // =========================================================
    // 실제 이펙트 생성
    // =========================================================

    private void Spawn(
        RuntimePixelSpriteType spriteType,
        Color color,
        Vector3 worldPosition,
        Transform followTarget,
        Vector3 followOffset,
        bool follow,
        float lifetime,
        float riseDistance,
        float startScale,
        float endScale,
        bool fadeOut,
        int sortingLayerId,
        int sortingOrder)
    {
        Sprite sprite =
            RuntimePixelEffectFactory
                .GetSprite(
                    spriteType
                );

        // 기존 코드는 여기서 아무 말 없이 종료했음
        if (sprite == null)
        {
            if (!missingSpriteWarnings.Contains(
                    spriteType))
            {
                missingSpriteWarnings.Add(
                    spriteType
                );

                Debug.LogWarning(
                    "[RuntimePixelEffectSystem] " +
                    spriteType +
                    " Sprite 생성에 실패했습니다."
                );
            }

            return;
        }

        worldPosition =
            SanitizePosition(
                worldPosition,
                followTarget
            );

        if (!IsFinite(followOffset))
        {
            followOffset =
                Vector3.zero;
        }

        lifetime =
            SafeFloat(
                lifetime,
                0.30f,
                0.05f
            );

        riseDistance =
            SafeFloat(
                riseDistance,
                0f
            );

        startScale =
            SafeFloat(
                startScale,
                1f,
                0.001f
            );

        endScale =
            SafeFloat(
                endScale,
                startScale,
                0.001f
            );

        GameObject obj =
            new GameObject(
                "RuntimeEffect_" +
                spriteType
            );

        RuntimePixelEffectInstance instance =
            obj.AddComponent<RuntimePixelEffectInstance>();

        instance.Initialize(
            sprite,
            color,
            sortingLayerId,
            sortingOrder,
            worldPosition,
            followTarget,
            followOffset,
            follow,
            lifetime,
            riseDistance,
            startScale,
            endScale,
            fadeOut,
            Mathf.Clamp(
                defaultStepCount,
                2,
                24
            )
        );
    }

    // =========================================================
    // 실제 캐릭터 SpriteRenderer 찾기
    // =========================================================

    private SpriteRenderer ResolveReferenceRenderer(
        Transform reference)
    {
        if (reference == null)
        {
            return null;
        }

        SpriteRenderer rootRenderer =
            reference.GetComponent<SpriteRenderer>();

        if (IsValidBodyRenderer(
                rootRenderer))
        {
            return rootRenderer;
        }

        SpriteRenderer[] renderers =
            reference.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        if (renderers == null ||
            renderers.Length == 0)
        {
            return null;
        }

        SpriteRenderer best =
            null;

        float bestScore =
            float.MinValue;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                renderers[i];

            if (!IsValidBodyRenderer(
                    renderer))
            {
                continue;
            }

            string lowerName =
                renderer.gameObject.name.ToLowerInvariant();

            float score = 0f;

            // HP바, 그림자, 범위표시 등을
            // 캐릭터 본체로 오인하지 않도록 함
            if (lowerName.Contains("hp") ||
                lowerName.Contains("health") ||
                lowerName.Contains("bar") ||
                lowerName.Contains("shadow") ||
                lowerName.Contains("marker") ||
                lowerName.Contains("range") ||
                lowerName.Contains("minimap") ||
                lowerName.Contains("point"))
            {
                score -= 100000f;
            }

            if (renderer.enabled)
            {
                score += 1000f;
            }

            Vector3 boundsSize =
                renderer.bounds.size;

            float area =
                Mathf.Abs(
                    boundsSize.x *
                    boundsSize.y
                );

            score +=
                area * 100f;

            int depth =
                GetHierarchyDepth(
                    reference,
                    renderer.transform
                );

            score -=
                depth * 10f;

            if (score >
                bestScore)
            {
                bestScore =
                    score;

                best =
                    renderer;
            }
        }

        return best;
    }

    private bool IsValidBodyRenderer(
        SpriteRenderer renderer)
    {
        return
            renderer != null &&
            renderer.sprite != null;
    }

    // =========================================================
    // 캐릭터 실제 보이는 중심
    // =========================================================

    private Vector3 ResolveVisualCenter(
        Transform reference)
    {
        if (reference == null)
        {
            return Vector3.zero;
        }

        SpriteRenderer renderer =
            ResolveReferenceRenderer(
                reference
            );

        if (renderer != null)
        {
            Vector3 center =
                renderer.bounds.center;

            if (IsFinite(center))
            {
                return center;
            }
        }

        return
            IsFinite(reference.position)
                ? reference.position
                : Vector3.zero;
    }

    // =========================================================
    // 캐릭터 Sprite 최상단
    // =========================================================

    private Vector3 ResolveVisualTop(
        Transform reference)
    {
        if (reference == null)
        {
            return Vector3.zero;
        }

        SpriteRenderer renderer =
            ResolveReferenceRenderer(
                reference
            );

        if (renderer != null)
        {
            Vector3 top =
                new Vector3(
                    renderer.bounds.center.x,
                    renderer.bounds.max.y,
                    renderer.bounds.center.z
                );

            if (IsFinite(top))
            {
                return top;
            }
        }

        return
            IsFinite(reference.position)
                ? reference.position
                : Vector3.zero;
    }

    // =========================================================
    // Sorting Layer
    // =========================================================

    private int ResolveSortingLayerId(
        Transform reference)
    {
        SpriteRenderer renderer =
            ResolveReferenceRenderer(
                reference
            );

        return renderer != null
            ? renderer.sortingLayerID
            : 0;
    }

    // =========================================================
    // Sorting Order
    // =========================================================

    private int ResolveSortingOrder(
        Transform reference,
        int extra)
    {
        SpriteRenderer renderer =
            ResolveReferenceRenderer(
                reference
            );

        int baseOrder =
            renderer != null
                ? renderer.sortingOrder
                : 0;

        long result =
            (long)baseOrder +
            sortingOrderOffset +
            extra;

        return
            (int)Mathf.Clamp(
                result,
                -32760,
                32760
            );
    }

    // =========================================================
    // Transform 깊이
    // =========================================================

    private int GetHierarchyDepth(
        Transform root,
        Transform target)
    {
        if (root == null ||
            target == null)
        {
            return 999;
        }

        int depth = 0;

        Transform current =
            target;

        while (current != null &&
               current != root)
        {
            depth++;

            current =
                current.parent;
        }

        return depth;
    }

    // =========================================================
    // NaN / Infinity 방지
    // =========================================================

    private Vector3 SanitizePosition(
        Vector3 position,
        Transform fallback)
    {
        if (IsFinite(position))
        {
            return position;
        }

        if (fallback != null &&
            IsFinite(fallback.position))
        {
            Debug.LogWarning(
                "[RuntimePixelEffectSystem] " +
                "잘못된 이펙트 위치가 들어와 " +
                "대상 위치로 보정했습니다."
            );

            return fallback.position;
        }

        Debug.LogWarning(
            "[RuntimePixelEffectSystem] " +
            "잘못된 이펙트 위치가 들어와 " +
            "Vector3.zero로 보정했습니다."
        );

        return Vector3.zero;
    }

    private float SafeFloat(
        float value,
        float fallback,
        float minimum =
            float.NegativeInfinity)
    {
        if (!IsFinite(value))
        {
            return fallback;
        }

        return
            Mathf.Max(
                minimum,
                value
            );
    }

    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }

    private static bool IsFinite(
        Vector3 value)
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);
    }

#if UNITY_EDITOR

    // =========================================================
    // Inspector 값 보호
    // =========================================================

    private void OnValidate()
    {
        sortingOrderOffset =
            Mathf.Clamp(
                sortingOrderOffset,
                -1000,
                1000
            );

        defaultStepCount =
            Mathf.Clamp(
                defaultStepCount,
                3,
                16
            );

        buffIconCount =
            Mathf.Clamp(
                buffIconCount,
                1,
                8
            );

        buffSpawnInterval =
            Mathf.Max(
                0f,
                buffSpawnInterval
            );

        buffLifetime =
            Mathf.Max(
                0.05f,
                buffLifetime
            );

        buffScale =
            Mathf.Max(
                0.01f,
                buffScale
            );

        if (!IsFinite(
                buffRiseDistance))
        {
            buffRiseDistance =
                0.42f;
        }

        if (!IsFinite(
                buffHorizontalSpread))
        {
            buffHorizontalSpread =
                0.55f;
        }

        if (!IsFinite(
                buffVerticalOffset))
        {
            buffVerticalOffset =
                0.25f;
        }
    }

#endif
}