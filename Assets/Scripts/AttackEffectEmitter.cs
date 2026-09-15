using UnityEngine;

public class AttackEffectEmitter : MonoBehaviour
{
    public enum OwnerType
    {
        Enemy,
        Summon
    }

    public enum PreviewDirection
    {
        Right,
        Left,
        Up,
        Down
    }

    // =========================================================
    // 기준 위치
    // =========================================================

    [Header("기준 위치")]

    [Tooltip(
        "지정하면 이 Transform을 이펙트 기준점으로 사용합니다.\n" +
        "비워두면 캐릭터 SpriteRenderer의 실제 중심을 자동 사용합니다."
    )]
    [SerializeField]
    private Transform effectAnchor;

    [Tooltip(
        "캐릭터 본체 SpriteRenderer입니다.\n" +
        "비워두면 자동으로 본체로 보이는 SpriteRenderer를 찾습니다."
    )]
    [SerializeField]
    private SpriteRenderer ownerSpriteRenderer;

    [Tooltip(
        "자동 계산된 캐릭터 중심에서 이펙트 위치를 추가로 보정합니다.\n" +
        "X = 좌우, Y = 위아래"
    )]
    [SerializeField]
    private Vector2 effectOriginOffset =
        new Vector2(0f, 0.1f);

    // =========================================================
    // 색상
    // =========================================================

    [Header("색상")]

    [SerializeField]
    private Color enemyColor =
        new Color(
            1f,
            0.35f,
            0.25f,
            1f
        );

    [SerializeField]
    private Color summonColor =
        new Color(
            0.4f,
            0.95f,
            1f,
            1f
        );

    // =========================================================
    // 모양
    // =========================================================

    [Header("모양")]

    [Range(16, 128)]
    [SerializeField]
    private int textureSize = 32;

    [Min(2f)]
    [SerializeField]
    private float outerRadiusPixels = 13f;

    [Min(1f)]
    [SerializeField]
    private float thicknessPixels = 5f;

    [Range(20f, 180f)]
    [SerializeField]
    private float arcAngle = 100f;

    [Range(-180f, 180f)]
    [SerializeField]
    private float arcCenterOffset = 0f;

    [Min(1)]
    [SerializeField]
    private int pixelsPerUnit = 32;

    // =========================================================
    // 크기 / 위치
    // =========================================================

    [Header("크기 / 위치")]

    [SerializeField]
    private Vector2 effectWorldSize =
        new Vector2(
            1.6f,
            1.6f
        );

    [Tooltip(
        "캐릭터 중심에서 공격 방향으로 " +
        "얼마나 앞에 생성할지 결정합니다."
    )]
    [Min(0f)]
    [SerializeField]
    private float spawnDistance = 0.65f;

    [Range(-180f, 180f)]
    [SerializeField]
    private float additionalRotation = 0f;

    [Tooltip(
        "캐릭터보다 몇 단계 위에 이펙트를 그릴지 결정합니다."
    )]
    [SerializeField]
    private int sortingOrderOffset = 20;

    // =========================================================
    // 애니메이션
    // =========================================================

    [Header("애니메이션")]

    [Min(0.01f)]
    [SerializeField]
    private float lifetime = 0.14f;

    [Min(0f)]
    [SerializeField]
    private float moveDistance = 0.18f;

    [Min(0.01f)]
    [SerializeField]
    private float startScale = 0.95f;

    [Min(0.01f)]
    [SerializeField]
    private float endScale = 1.15f;

    [SerializeField]
    private AnimationCurve alphaCurve =
        new AnimationCurve(
            new Keyframe(
                0f,
                1f
            ),
            new Keyframe(
                1f,
                0f
            )
        );

    // =========================================================
    // Inspector 미리보기
    // =========================================================

    [Header("인스펙터 미리보기")]

    [SerializeField]
    private OwnerType previewOwner =
        OwnerType.Enemy;

    [SerializeField]
    private PreviewDirection previewDirection =
        PreviewDirection.Right;

    // =========================================================
    // Cache
    // =========================================================

    private Texture2D cachedTexture;

    private Sprite cachedSprite;

    // =========================================================
    // Public
    // =========================================================

    public OwnerType CurrentPreviewOwner =>
        previewOwner;

    public PreviewDirection CurrentPreviewDirection =>
        previewDirection;

    public Vector2 EffectWorldSize =>
        effectWorldSize;

    public Vector2 EffectOriginOffset =>
        effectOriginOffset;

    public float SpawnDistance =>
        spawnDistance;

    public float AdditionalRotation =>
        additionalRotation;

    public float Lifetime =>
        lifetime;

    // =========================================================
    // Unity
    // =========================================================

    private void Reset()
    {
        FindOwnerSpriteRenderer();
    }

    private void Awake()
    {
        FindOwnerSpriteRenderer();
    }

    private void OnDestroy()
    {
        ClearCache();
    }

    // =========================================================
    // SpriteRenderer 자동 탐색
    // =========================================================

    private void FindOwnerSpriteRenderer()
    {
        if (ownerSpriteRenderer != null)
        {
            return;
        }

        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );

        if (renderers == null ||
            renderers.Length == 0)
        {
            return;
        }

        SpriteRenderer bestRenderer =
            null;

        float bestScore =
            float.MinValue;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                renderers[i];

            if (renderer == null ||
                renderer.sprite == null)
            {
                continue;
            }

            string objectName =
                renderer.gameObject.name
                    .ToLowerInvariant();

            // HP바 / 그림자 / 마커 등은 본체 후보에서 제외
            if (objectName.Contains("hp") ||
                objectName.Contains("health") ||
                objectName.Contains("bar") ||
                objectName.Contains("shadow") ||
                objectName.Contains("marker") ||
                objectName.Contains("range") ||
                objectName.Contains("minimap"))
            {
                continue;
            }

            Vector3 size =
                renderer.bounds.size;

            float area =
                Mathf.Abs(
                    size.x *
                    size.y
                );

            float score =
                area;

            if (renderer.enabled)
            {
                score += 1000f;
            }

            if (score >
                bestScore)
            {
                bestScore =
                    score;

                bestRenderer =
                    renderer;
            }
        }

        if (bestRenderer != null)
        {
            ownerSpriteRenderer =
                bestRenderer;
        }
    }

    // =========================================================
    // 공격 이펙트 실행
    // =========================================================

    public void PlayEffect(
        Vector2 direction,
        OwnerType ownerType)
    {
        if (!IsFinite(direction) ||
            direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                Vector2.right;
        }

        direction.Normalize();

        FindOwnerSpriteRenderer();

        Vector3 origin =
            ResolveEffectOrigin();

        Vector3 spawnPosition =
            origin +
            (Vector3)(
                direction *
                spawnDistance
            );

        if (!IsFinite(spawnPosition))
        {
            spawnPosition =
                transform.position;
        }

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg +
            additionalRotation;

        GameObject effectObject =
            new GameObject(
                ownerType ==
                OwnerType.Enemy
                    ? "Enemy_SlashEffect"
                    : "Summon_SlashEffect"
            );

        effectObject.transform.position =
            spawnPosition;

        effectObject.transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        ProceduralSlashEffect effect =
            effectObject.AddComponent<ProceduralSlashEffect>();

        int sortingLayerID = 0;

        int sortingOrder =
            sortingOrderOffset;

        if (ownerSpriteRenderer != null)
        {
            sortingLayerID =
                ownerSpriteRenderer
                    .sortingLayerID;

            sortingOrder =
                ownerSpriteRenderer
                    .sortingOrder +
                sortingOrderOffset;
        }

        Sprite sprite =
            GetOrCreateSprite();

        if (sprite == null)
        {
            Debug.LogWarning(
                "[AttackEffectEmitter] " +
                "반달 Sprite 생성에 실패했습니다.",
                this
            );

            Destroy(
                effectObject
            );

            return;
        }

        effect.Initialize(
            sprite,
            GetOwnerColor(
                ownerType
            ),
            effectWorldSize,
            direction,
            moveDistance,
            lifetime,
            startScale,
            endScale,
            sortingLayerID,
            sortingOrder,
            alphaCurve
        );
    }

    // =========================================================
    // 실제 생성 기준점
    // =========================================================

    private Vector3 ResolveEffectOrigin()
    {
        Vector3 origin;

        // Anchor를 직접 넣었으면 그 위치 우선
        if (effectAnchor != null)
        {
            origin =
                effectAnchor.position;
        }

        // 없으면 캐릭터 이미지의 실제 중심 사용
        else if (ownerSpriteRenderer != null)
        {
            origin =
                ownerSpriteRenderer
                    .bounds
                    .center;
        }

        else
        {
            origin =
                transform.position;
        }

        origin +=
            new Vector3(
                effectOriginOffset.x,
                effectOriginOffset.y,
                0f
            );

        return origin;
    }

    // =========================================================
    // Preview
    // =========================================================

    public void PlayPreviewEffect()
    {
        PlayEffect(
            GetPreviewDirectionVector(),
            previewOwner
        );
    }

    public Texture2D GetPreviewTexture()
    {
        return GetOrCreateTexture();
    }

    public Color GetPreviewColor()
    {
        return GetOwnerColor(
            previewOwner
        );
    }

    public Vector2 GetPreviewDirectionVector()
    {
        switch (previewDirection)
        {
            case PreviewDirection.Left:
                return Vector2.left;

            case PreviewDirection.Up:
                return Vector2.up;

            case PreviewDirection.Down:
                return Vector2.down;

            default:
                return Vector2.right;
        }
    }

    public float GetPreviewGuiAngle()
    {
        float angle;

        switch (previewDirection)
        {
            case PreviewDirection.Left:
                angle = 180f;
                break;

            case PreviewDirection.Up:
                angle = -90f;
                break;

            case PreviewDirection.Down:
                angle = 90f;
                break;

            default:
                angle = 0f;
                break;
        }

        return angle +
               additionalRotation;
    }

    // =========================================================
    // 색상
    // =========================================================

    private Color GetOwnerColor(
        OwnerType ownerType)
    {
        return
            ownerType ==
            OwnerType.Enemy
                ? enemyColor
                : summonColor;
    }

    // =========================================================
    // Texture / Sprite
    // =========================================================

    private Texture2D GetOrCreateTexture()
    {
        if (cachedTexture != null)
        {
            return cachedTexture;
        }

        cachedTexture =
            BuildSlashTexture(
                textureSize,
                outerRadiusPixels,
                thicknessPixels,
                arcAngle,
                arcCenterOffset
            );

        if (cachedTexture == null)
        {
            return null;
        }

        cachedTexture.filterMode =
            FilterMode.Point;

        cachedTexture.wrapMode =
            TextureWrapMode.Clamp;

        cachedTexture.hideFlags =
            HideFlags.DontSave;

        return cachedTexture;
    }

    private Sprite GetOrCreateSprite()
    {
        if (cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture =
            GetOrCreateTexture();

        if (texture == null)
        {
            return null;
        }

        cachedSprite =
            Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    texture.width,
                    texture.height
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                pixelsPerUnit
            );

        cachedSprite.name =
            "ProceduralSlashSprite";

        return cachedSprite;
    }

    private void ClearCache()
    {
        if (cachedSprite != null)
        {
            if (Application.isPlaying)
            {
                Destroy(
                    cachedSprite
                );
            }
            else
            {
                DestroyImmediate(
                    cachedSprite
                );
            }

            cachedSprite =
                null;
        }

        if (cachedTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(
                    cachedTexture
                );
            }
            else
            {
                DestroyImmediate(
                    cachedTexture
                );
            }

            cachedTexture =
                null;
        }
    }

    // =========================================================
    // 반달 도트 생성
    // =========================================================

    public static Texture2D BuildSlashTexture(
        int size,
        float outerRadius,
        float thickness,
        float arc,
        float centerOffset)
    {
        size =
            Mathf.Max(
                16,
                size
            );

        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        Color clear =
            new Color(
                0f,
                0f,
                0f,
                0f
            );

        Color fill =
            Color.white;

        Color[] colors =
            new Color[
                size * size
            ];

        for (int i = 0;
             i < colors.Length;
             i++)
        {
            colors[i] =
                clear;
        }

        float half =
            size * 0.5f;

        outerRadius =
            Mathf.Clamp(
                outerRadius,
                2f,
                half - 1f
            );

        thickness =
            Mathf.Clamp(
                thickness,
                1f,
                outerRadius - 1f
            );

        float halfArc =
            Mathf.Clamp(
                arc * 0.5f,
                10f,
                90f
            );

        for (int y = 0;
             y < size;
             y++)
        {
            for (int x = 0;
                 x < size;
                 x++)
            {
                float px =
                    x +
                    0.5f -
                    half;

                float py =
                    y +
                    0.5f -
                    half;

                Vector2 point =
                    new Vector2(
                        px,
                        py
                    );

                float radius =
                    point.magnitude;

                if (radius >
                    outerRadius)
                {
                    continue;
                }

                float angle =
                    Mathf.Atan2(
                        py,
                        px
                    ) *
                    Mathf.Rad2Deg;

                float delta =
                    Mathf.DeltaAngle(
                        centerOffset,
                        angle
                    );

                if (Mathf.Abs(delta) >
                    halfArc)
                {
                    continue;
                }

                float middleAmount =
                    1f -
                    Mathf.Clamp01(
                        Mathf.Abs(delta) /
                        halfArc
                    );

                float currentThickness =
                    Mathf.Lerp(
                        thickness * 0.55f,
                        thickness,
                        middleAmount
                    );

                float innerRadius =
                    outerRadius -
                    currentThickness;

                if (radius <
                    innerRadius)
                {
                    continue;
                }

                int index =
                    y * size +
                    x;

                colors[index] =
                    fill;
            }
        }

        texture.SetPixels(
            colors
        );

        texture.Apply();

        return texture;
    }

    // =========================================================
    // 안전장치
    // =========================================================

    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }

    private static bool IsFinite(
        Vector2 value)
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y);
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        textureSize =
            Mathf.Clamp(
                textureSize,
                16,
                128
            );

        outerRadiusPixels =
            Mathf.Max(
                2f,
                outerRadiusPixels
            );

        thicknessPixels =
            Mathf.Clamp(
                thicknessPixels,
                1f,
                Mathf.Max(
                    1.01f,
                    outerRadiusPixels - 1f
                )
            );

        pixelsPerUnit =
            Mathf.Max(
                1,
                pixelsPerUnit
            );

        effectWorldSize.x =
            Mathf.Max(
                0.05f,
                effectWorldSize.x
            );

        effectWorldSize.y =
            Mathf.Max(
                0.05f,
                effectWorldSize.y
            );

        spawnDistance =
            Mathf.Max(
                0f,
                spawnDistance
            );

        lifetime =
            Mathf.Max(
                0.01f,
                lifetime
            );

        moveDistance =
            Mathf.Max(
                0f,
                moveDistance
            );

        startScale =
            Mathf.Max(
                0.01f,
                startScale
            );

        endScale =
            Mathf.Max(
                0.01f,
                endScale
            );

        ClearCache();
    }

#endif
}