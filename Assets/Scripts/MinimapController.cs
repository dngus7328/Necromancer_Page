using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class MinimapController : MonoBehaviour
{
    [Header("현재 방")]
    [Tooltip("현재 방 범위. CameraBounds의 BoxCollider2D 연결")]
    [SerializeField] private BoxCollider2D roomBounds;

    [Header("벽 / 장애물")]
    [Tooltip("미니맵에서 막힌 영역으로 표시할 레이어")]
    [SerializeField] private LayerMask blockerLayer;

    [Header("리벨")]
    [SerializeField] private Transform ribel;

    [Header("미니맵 해상도")]
    [Tooltip("클수록 선명하지만 갱신 비용 증가")]
    [SerializeField] private int textureWidth = 160;

    [SerializeField] private int textureHeight = 100;

    [Header("색")]
    [SerializeField]
    private Color walkableColor =
        new Color(
            0.12f,
            0.10f,
            0.16f,
            1f
        );

    [SerializeField]
    private Color blockedColor =
        new Color(
            0.025f,
            0.02f,
            0.035f,
            1f
        );

    [SerializeField]
    private Color ribelColor =
        new Color(
            0.9f,
            0.8f,
            1f,
            1f
        );

    [SerializeField]
    private Color summonColor =
        new Color(
            0.35f,
            0.65f,
            1f,
            1f
        );

    [SerializeField]
    private Color enemyColor =
        new Color(
            1f,
            0.25f,
            0.25f,
            1f
        );

    [Header("점 크기")]
    [SerializeField] private int ribelDotRadius = 2;
    [SerializeField] private int summonDotRadius = 1;
    [SerializeField] private int enemyDotRadius = 1;

    [Header("갱신")]
    [Tooltip("유닛 위치 갱신 주기")]
    [SerializeField] private float refreshInterval = 0.1f;

    private RawImage rawImage;
    private Texture2D minimapTexture;

    private Color[] basePixels;

    private float refreshTimer;

    private SummonUnitBase[] summons;
    private EnemyUnitBase[] enemies;

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
        rawImage =
            GetComponent<RawImage>();

        FindRibel();

        CreateTexture();
    }

    private void Start()
    {
        BuildStaticMap();

        RefreshUnitCache();

        DrawMinimap();
    }

    private void Update()
    {
        refreshTimer -=
            Time.deltaTime;

        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer =
            refreshInterval;

        RefreshUnitCache();
        DrawMinimap();
    }

    // =========================================================
    // 리벨 찾기
    // =========================================================

    private void FindRibel()
    {
        if (ribel != null)
        {
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (player != null)
        {
            ribel =
                player.transform;
        }
    }

    // =========================================================
    // Texture 생성
    // =========================================================

    private void CreateTexture()
    {
        textureWidth =
            Mathf.Max(
                32,
                textureWidth
            );

        textureHeight =
            Mathf.Max(
                32,
                textureHeight
            );

        minimapTexture =
            new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                false
            );

        minimapTexture.filterMode =
            FilterMode.Point;

        minimapTexture.wrapMode =
            TextureWrapMode.Clamp;

        rawImage.texture =
            minimapTexture;

        basePixels =
            new Color[
                textureWidth *
                textureHeight
            ];
    }

    // =========================================================
    // 고정 맵 생성
    // =========================================================

    private void BuildStaticMap()
    {
        if (roomBounds == null)
        {
            FillBase(
                blockedColor
            );

            return;
        }

        Bounds bounds =
            roomBounds.bounds;

        for (int y = 0;
             y < textureHeight;
             y++)
        {
            for (int x = 0;
                 x < textureWidth;
                 x++)
            {
                Vector2 worldPosition =
                    PixelToWorld(
                        x,
                        y,
                        bounds
                    );

                Collider2D blocker =
                    Physics2D.OverlapPoint(
                        worldPosition,
                        blockerLayer
                    );

                Color color =
                    blocker != null
                        ? blockedColor
                        : walkableColor;

                int index =
                    y *
                    textureWidth +
                    x;

                basePixels[index] =
                    color;
            }
        }
    }

    private void FillBase(
        Color color)
    {
        for (int i = 0;
             i < basePixels.Length;
             i++)
        {
            basePixels[i] =
                color;
        }
    }

    // =========================================================
    // 유닛 목록 갱신
    // =========================================================

    private void RefreshUnitCache()
    {
        summons =
            FindObjectsOfType<SummonUnitBase>();

        enemies =
            FindObjectsOfType<EnemyUnitBase>();
    }

    // =========================================================
    // 미니맵 그리기
    // =========================================================

    private void DrawMinimap()
    {
        if (minimapTexture == null ||
            basePixels == null)
        {
            return;
        }

        minimapTexture.SetPixels(
            basePixels
        );

        if (roomBounds == null)
        {
            minimapTexture.Apply();
            return;
        }

        Bounds bounds =
            roomBounds.bounds;

        // 소환수
        if (summons != null)
        {
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

                DrawWorldDot(
                    summon.transform.position,
                    summonColor,
                    summonDotRadius,
                    bounds
                );
            }
        }

        // 적
        if (enemies != null)
        {
            for (int i = 0;
                 i < enemies.Length;
                 i++)
            {
                EnemyUnitBase enemy =
                    enemies[i];

                if (enemy == null)
                {
                    continue;
                }

                DrawWorldDot(
                    enemy.transform.position,
                    enemyColor,
                    enemyDotRadius,
                    bounds
                );
            }
        }

        // 리벨은 항상 마지막에 그려서
        // 다른 점에 가리지 않게
        if (ribel != null)
        {
            DrawWorldDot(
                ribel.position,
                ribelColor,
                ribelDotRadius,
                bounds
            );
        }

        minimapTexture.Apply();
    }

    // =========================================================
    // 월드 위치 -> 미니맵 점
    // =========================================================

    private void DrawWorldDot(
        Vector2 worldPosition,
        Color color,
        int radius,
        Bounds bounds)
    {
        Vector2 pixel =
            WorldToPixel(
                worldPosition,
                bounds
            );

        int centerX =
            Mathf.RoundToInt(
                pixel.x
            );

        int centerY =
            Mathf.RoundToInt(
                pixel.y
            );

        radius =
            Mathf.Max(
                0,
                radius
            );

        for (int y = -radius;
             y <= radius;
             y++)
        {
            for (int x = -radius;
                 x <= radius;
                 x++)
            {
                int px =
                    centerX + x;

                int py =
                    centerY + y;

                if (px < 0 ||
                    px >= textureWidth ||
                    py < 0 ||
                    py >= textureHeight)
                {
                    continue;
                }

                minimapTexture.SetPixel(
                    px,
                    py,
                    color
                );
            }
        }
    }

    // =========================================================
    // 좌표 변환
    // =========================================================

    private Vector2 WorldToPixel(
        Vector2 worldPosition,
        Bounds bounds)
    {
        float normalizedX =
            Mathf.InverseLerp(
                bounds.min.x,
                bounds.max.x,
                worldPosition.x
            );

        float normalizedY =
            Mathf.InverseLerp(
                bounds.min.y,
                bounds.max.y,
                worldPosition.y
            );

        return new Vector2(
            normalizedX *
            (textureWidth - 1),

            normalizedY *
            (textureHeight - 1)
        );
    }

    private Vector2 PixelToWorld(
        int x,
        int y,
        Bounds bounds)
    {
        float normalizedX =
            x /
            (float)(
                textureWidth - 1
            );

        float normalizedY =
            y /
            (float)(
                textureHeight - 1
            );

        return new Vector2(
            Mathf.Lerp(
                bounds.min.x,
                bounds.max.x,
                normalizedX
            ),

            Mathf.Lerp(
                bounds.min.y,
                bounds.max.y,
                normalizedY
            )
        );
    }

    // =========================================================
    // 방 변경용
    // =========================================================

    public void SetRoomBounds(
        BoxCollider2D newBounds)
    {
        roomBounds =
            newBounds;

        BuildStaticMap();

        DrawMinimap();
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        textureWidth =
            Mathf.Max(
                32,
                textureWidth
            );

        textureHeight =
            Mathf.Max(
                32,
                textureHeight
            );

        ribelDotRadius =
            Mathf.Max(
                0,
                ribelDotRadius
            );

        summonDotRadius =
            Mathf.Max(
                0,
                summonDotRadius
            );

        enemyDotRadius =
            Mathf.Max(
                0,
                enemyDotRadius
            );

        refreshInterval =
            Mathf.Max(
                0.05f,
                refreshInterval
            );
    }

#endif
}