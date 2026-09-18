using UnityEngine;
using UnityEngine.Rendering;

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

    [SerializeField]
    private Transform effectAnchor;

    [SerializeField]
    private SpriteRenderer ownerSpriteRenderer;

    // =========================================================
    // 색상
    // =========================================================

    [Header("색상")]

    [SerializeField]
    private Color enemyColor =
        new Color(1f, 0.35f, 0.25f, 1f);

    [SerializeField]
    private Color summonColor =
        new Color(0.4f, 0.95f, 1f, 1f);

    // =========================================================
    // 모양
    // =========================================================

    [Header("모양")]

    [Tooltip("생성되는 도트 이펙트 텍스처 크기")]
    [Range(16, 128)]
    [SerializeField]
    private int textureSize = 32;

    [Tooltip("반달 바깥 반지름 (픽셀 단위)")]
    [Min(2f)]
    [SerializeField]
    private float outerRadiusPixels = 13f;

    [Tooltip("반달 두께 (픽셀 단위)")]
    [Min(1f)]
    [SerializeField]
    private float thicknessPixels = 5f;

    [Tooltip("반달 각도")]
    [Range(20f, 180f)]
    [SerializeField]
    private float arcAngle = 100f;

    [Tooltip("기본 반달 방향의 추가 회전값")]
    [Range(-180f, 180f)]
    [SerializeField]
    private float arcCenterOffset = 0f;

    [Tooltip("스프라이트 1유닛당 픽셀 수")]
    [Min(1)]
    [SerializeField]
    private int pixelsPerUnit = 32;

    // =========================================================
    // 크기 / 위치
    // =========================================================

    [Header("크기 / 위치")]

    [Tooltip("실제 게임 화면에서 보이는 이펙트 크기")]
    [SerializeField]
    private Vector2 effectWorldSize =
        new Vector2(1.6f, 1.6f);

    [Tooltip("본체 중심에서 얼마나 앞에 생성할지")]
    [Min(0f)]
    [SerializeField]
    private float spawnDistance = 0.65f;

    [Tooltip("공격 방향에 더해질 추가 회전")]
    [Range(-180f, 180f)]
    [SerializeField]
    private float additionalRotation = 0f;

    [SerializeField]
    private int sortingOrderOffset = 5;

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
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        );

    // =========================================================
    // 인스펙터 미리보기
    // =========================================================

    [Header("인스펙터 미리보기")]

    [SerializeField]
    private OwnerType previewOwner =
        OwnerType.Enemy;

    [SerializeField]
    private PreviewDirection previewDirection =
        PreviewDirection.Right;

    // =========================================================
    // 캐시
    // =========================================================

    private Texture2D cachedTexture;
    private Sprite cachedSprite;

    // =========================================================
    // Public Property
    // =========================================================

    public OwnerType CurrentPreviewOwner =>
        previewOwner;

    public PreviewDirection CurrentPreviewDirection =>
        previewDirection;

    public Vector2 EffectWorldSize =>
        effectWorldSize;

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
        effectAnchor =
            transform;

        ownerSpriteRenderer =
            GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (effectAnchor == null)
        {
            effectAnchor = transform;
        }

        if (ownerSpriteRenderer == null)
        {
            ownerSpriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnDestroy()
    {
        ClearCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        textureSize =
            Mathf.Clamp(textureSize, 16, 128);

        outerRadiusPixels =
            Mathf.Max(2f, outerRadiusPixels);

        thicknessPixels =
            Mathf.Clamp(
                thicknessPixels,
                1f,
                outerRadiusPixels - 1f
            );

        pixelsPerUnit =
            Mathf.Max(1, pixelsPerUnit);

        effectWorldSize.x =
            Mathf.Max(0.05f, effectWorldSize.x);

        effectWorldSize.y =
            Mathf.Max(0.05f, effectWorldSize.y);

        spawnDistance =
            Mathf.Max(0f, spawnDistance);

        lifetime =
            Mathf.Max(0.01f, lifetime);

        moveDistance =
            Mathf.Max(0f, moveDistance);

        startScale =
            Mathf.Max(0.01f, startScale);

        endScale =
            Mathf.Max(0.01f, endScale);

        ClearCache();
    }
#endif

    // =========================================================
    // 외부 호출
    // =========================================================

    public void PlayEffect(
        Vector2 direction,
        OwnerType ownerType)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();

        Transform anchor =
            effectAnchor != null
                ? effectAnchor
                : transform;

        Vector3 spawnPosition =
            anchor.position +
            (Vector3)(direction * spawnDistance);

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg +
            additionalRotation;

        GameObject effectObject =
            new GameObject("SlashEffect");

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

        int sortingLayerID =
            0;

        int sortingOrder =
            sortingOrderOffset;

        SortingGroup ownerSortingGroup =
            GetComponentInParent<SortingGroup>();

        if (ownerSortingGroup != null)
        {
            sortingLayerID =
                ownerSortingGroup.sortingLayerID;

            sortingOrder =
                ownerSortingGroup.sortingOrder +
                sortingOrderOffset;
        }
        else if (ownerSpriteRenderer != null)
        {
            sortingLayerID =
                ownerSpriteRenderer.sortingLayerID;

            sortingOrder =
                ownerSpriteRenderer.sortingOrder +
                sortingOrderOffset;
        }

        effect.Initialize(
            GetOrCreateSprite(),
            GetOwnerColor(ownerType),
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

    public void PlayPreviewEffect()
    {
        PlayEffect(
            GetPreviewDirectionVector(),
            previewOwner
        );
    }

    // =========================================================
    // 인스펙터용 정보
    // =========================================================

    public Texture2D GetPreviewTexture()
    {
        return GetOrCreateTexture();
    }

    public Color GetPreviewColor()
    {
        return GetOwnerColor(previewOwner);
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
        float angle = 0f;

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

        return angle + additionalRotation;
    }

    // =========================================================
    // 색상
    // =========================================================

    private Color GetOwnerColor(
        OwnerType ownerType)
    {
        return ownerType == OwnerType.Enemy
            ? enemyColor
            : summonColor;
    }

    // =========================================================
    // 스프라이트 캐시
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

        cachedSprite =
            Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    texture.width,
                    texture.height
                ),
                new Vector2(0.5f, 0.5f),
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
                Destroy(cachedSprite);
            }
            else
            {
                DestroyImmediate(cachedSprite);
            }

            cachedSprite = null;
        }

        if (cachedTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(cachedTexture);
            }
            else
            {
                DestroyImmediate(cachedTexture);
            }

            cachedTexture = null;
        }
    }

    // =========================================================
    // 실제 도트 텍스처 생성
    // =========================================================

    public static Texture2D BuildSlashTexture(
        int size,
        float outerRadius,
        float thickness,
        float arc,
        float centerOffset)
    {
        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        Color clear =
            new Color(0f, 0f, 0f, 0f);

        Color fill =
            Color.white;

        Color[] colors =
            new Color[size * size];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = clear;
        }

        float half =
            size * 0.5f;

        float innerRadius =
            Mathf.Max(
                0f,
                outerRadius - thickness
            );

        float halfArc =
            arc * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px =
                    x + 0.5f - half;

                float py =
                    y + 0.5f - half;

                Vector2 point =
                    new Vector2(px, py);

                float radius =
                    point.magnitude;

                if (radius > outerRadius)
                {
                    continue;
                }

                float angle =
                    Mathf.Atan2(py, px) *
                    Mathf.Rad2Deg;

                float delta =
                    Mathf.DeltaAngle(
                        centerOffset,
                        angle
                    );

                if (Mathf.Abs(delta) > halfArc)
                {
                    continue;
                }

                float endTaper =
                    Mathf.InverseLerp(
                        halfArc,
                        0f,
                        Mathf.Abs(delta)
                    );

                float localThickness =
                    Mathf.Lerp(
                        thickness * 0.65f,
                        thickness,
                        endTaper
                    );

                float localInnerRadius =
                    outerRadius - localThickness;

                if (radius < localInnerRadius ||
                    radius < innerRadius * 0.55f)
                {
                    continue;
                }

                int index =
                    y * size + x;

                colors[index] = fill;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }
}