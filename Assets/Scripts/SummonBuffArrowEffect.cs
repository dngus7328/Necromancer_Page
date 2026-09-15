using System.Collections;
using UnityEngine;

public enum BuffArrowVisualType
{
    Generic,
    MaxHealth,
    Damage,
    Cooldown,
    ManaCost
}

public class SummonBuffArrowEffect : MonoBehaviour
{
    [Header("버프 화살표")]
    [Tooltip("한 번 버프가 적용될 때 순차적으로 올라오는 화살표 수")]
    [Range(1, 8)]
    [SerializeField]
    private int arrowCount = 4;

    [Tooltip("화살표 사이 생성 간격")]
    [Range(0.01f, 0.3f)]
    [SerializeField]
    private float spawnInterval = 0.07f;

    [Tooltip("화살표 하나가 보이는 시간")]
    [Range(0.15f, 1.5f)]
    [SerializeField]
    private float arrowLifetime = 0.52f;

    [Tooltip("몸 중심 기준 시작 높이")]
    [SerializeField]
    private float verticalOffset = 0.35f;

    [Tooltip("몸 좌우로 흩어지는 범위")]
    [Range(0f, 1f)]
    [SerializeField]
    private float horizontalSpread = 0.34f;

    [Tooltip("위로 올라가는 거리")]
    [Range(0.05f, 1.5f)]
    [SerializeField]
    private float riseDistance = 0.55f;

    [Tooltip("화살표 크기")]
    [Range(0.2f, 2f)]
    [SerializeField]
    private float arrowScale = 0.95f;

    [Tooltip("기존 소환수 스프라이트보다 위에 그릴 정렬값")]
    [Range(1, 5000)]
    [SerializeField]
    private int sortingOrderOffset = 100;

    [Tooltip("위로 올라가는 움직임을 몇 단계로 끊을지 정합니다.")]
    [Range(3, 16)]
    [SerializeField]
    private int movementSteps = 7;

    [Header("버프별 색상")]
    [SerializeField]
    private Color genericColor =
        new Color(0.86f, 0.64f, 1f, 1f);

    [SerializeField]
    private Color maxHealthColor =
        new Color(0.55f, 1f, 0.60f, 1f);

    [SerializeField]
    private Color damageColor =
        new Color(1f, 0.48f, 0.30f, 1f);

    [SerializeField]
    private Color cooldownColor =
        new Color(0.42f, 0.86f, 1f, 1f);

    [SerializeField]
    private Color manaCostColor =
        new Color(0.58f, 0.52f, 1f, 1f);

    private Coroutine playCoroutine;

    private static Sprite genericArrowSprite;
    private static Sprite maxHealthArrowSprite;
    private static Sprite damageArrowSprite;
    private static Sprite cooldownArrowSprite;
    private static Sprite manaArrowSprite;

    public void Play()
    {
        Play(BuffArrowVisualType.Generic);
    }

    public void Play(
        BuffArrowVisualType visualType)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (playCoroutine != null)
        {
            StopCoroutine(
                playCoroutine
            );
        }

        playCoroutine =
            StartCoroutine(
                PlayRoutine(
                    visualType
                )
            );
    }

    private IEnumerator PlayRoutine(
        BuffArrowVisualType visualType)
    {
        int count =
            Mathf.Max(
                1,
                arrowCount
            );

        for (int i = 0;
             i < count;
             i++)
        {
            SpawnArrow(
                i,
                count,
                visualType
            );

            if (i < count - 1)
            {
                float wait = 0f;

                while (wait <
                       spawnInterval)
                {
                    wait +=
                        Time.unscaledDeltaTime;

                    yield return null;
                }
            }
        }

        playCoroutine = null;
    }

    private void SpawnArrow(
        int index,
        int totalCount,
        BuffArrowVisualType visualType)
    {
        Sprite sprite =
            GetOrCreateArrowSprite(
                visualType
            );

        if (sprite == null)
        {
            return;
        }

        GameObject arrowObject =
            new GameObject(
                "BuffArrow_" +
                visualType
            );

        SpriteRenderer renderer =
            arrowObject.AddComponent<SpriteRenderer>();

        renderer.sprite =
            sprite;

        Color color =
            GetColor(
                visualType
            );

        renderer.color =
            color;

        ApplySorting(
            renderer
        );

        float normalized =
            totalCount <= 1
                ? 0.5f
                : index /
                  (float)(totalCount - 1);

        float relativeX =
            Mathf.Lerp(
                -horizontalSpread,
                horizontalSpread,
                normalized
            );

        relativeX +=
            Random.Range(
                -0.05f,
                0.05f
            );

        float relativeY =
            verticalOffset +
            Random.Range(
                -0.05f,
                0.05f
            );

        arrowObject.transform.position =
            transform.position +
            new Vector3(
                relativeX,
                relativeY,
                -0.02f
            );

        arrowObject.transform.localScale =
            Vector3.one *
            arrowScale;

        StartCoroutine(
            AnimateArrow(
                arrowObject,
                renderer,
                relativeX,
                relativeY,
                color
            )
        );
    }

    private IEnumerator AnimateArrow(
        GameObject arrowObject,
        SpriteRenderer renderer,
        float relativeX,
        float relativeY,
        Color baseColor)
    {
        float elapsed = 0f;

        float duration =
            Mathf.Max(
                0.15f,
                arrowLifetime
            );

        int steps =
            Mathf.Clamp(
                movementSteps,
                3,
                16
            );

        while (elapsed < duration &&
               arrowObject != null)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float rawT =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            float steppedT =
                Mathf.Round(
                    rawT *
                    (steps - 1)
                ) /
                (steps - 1);

            arrowObject.transform.position =
                transform.position +
                new Vector3(
                    relativeX,
                    relativeY +
                    riseDistance *
                    steppedT,
                    -0.02f
                );

            float pop =
                1f +
                Mathf.Sin(
                    steppedT *
                    Mathf.PI
                ) *
                0.18f;

            arrowObject.transform.localScale =
                Vector3.one *
                arrowScale *
                pop;

            Color color =
                baseColor;

            if (steppedT > 0.50f)
            {
                color.a *=
                    Mathf.InverseLerp(
                        1f,
                        0.50f,
                        steppedT
                    );
            }

            renderer.color =
                color;

            yield return null;
        }

        if (arrowObject != null)
        {
            Destroy(
                arrowObject
            );
        }
    }

    private Color GetColor(
        BuffArrowVisualType visualType)
    {
        switch (visualType)
        {
            case BuffArrowVisualType.MaxHealth:
                return maxHealthColor;

            case BuffArrowVisualType.Damage:
                return damageColor;

            case BuffArrowVisualType.Cooldown:
                return cooldownColor;

            case BuffArrowVisualType.ManaCost:
                return manaCostColor;

            default:
                return genericColor;
        }
    }

    private void ApplySorting(
        SpriteRenderer arrowRenderer)
    {
        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );

        SpriteRenderer reference = null;
        int highestOrder = int.MinValue;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer candidate =
                renderers[i];

            if (candidate == null)
            {
                continue;
            }

            if (candidate.sortingOrder >
                highestOrder)
            {
                highestOrder =
                    candidate.sortingOrder;

                reference =
                    candidate;
            }
        }

        if (reference != null)
        {
            arrowRenderer.sortingLayerID =
                reference.sortingLayerID;

            arrowRenderer.sortingOrder =
                Mathf.Min(
                    32760,
                    reference.sortingOrder +
                    sortingOrderOffset
                );
        }
        else
        {
            arrowRenderer.sortingOrder =
                sortingOrderOffset;
        }
    }

    private static Sprite GetOrCreateArrowSprite(
        BuffArrowVisualType visualType)
    {
        switch (visualType)
        {
            case BuffArrowVisualType.MaxHealth:
                if (maxHealthArrowSprite == null)
                {
                    maxHealthArrowSprite =
                        CreateMaxHealthArrow();
                }

                return maxHealthArrowSprite;

            case BuffArrowVisualType.Damage:
                if (damageArrowSprite == null)
                {
                    damageArrowSprite =
                        CreateDamageArrow();
                }

                return damageArrowSprite;

            case BuffArrowVisualType.Cooldown:
                if (cooldownArrowSprite == null)
                {
                    cooldownArrowSprite =
                        CreateCooldownArrow();
                }

                return cooldownArrowSprite;

            case BuffArrowVisualType.ManaCost:
                if (manaArrowSprite == null)
                {
                    manaArrowSprite =
                        CreateManaArrow();
                }

                return manaArrowSprite;

            default:
                if (genericArrowSprite == null)
                {
                    genericArrowSprite =
                        CreateGenericArrow();
                }

                return genericArrowSprite;
        }
    }

    private static Sprite CreateGenericArrow()
    {
        return CreateSpriteFromPixels(
            9,
            11,
            new int[,]
            {
                {4,10},
                {3,9},{4,9},{5,9},
                {2,8},{3,8},{4,8},{5,8},{6,8},
                {1,7},{2,7},{3,7},{4,7},{5,7},{6,7},{7,7},
                {0,6},{1,6},{2,6},{3,6},{4,6},{5,6},{6,6},{7,6},{8,6},
                {3,5},{4,5},{5,5},
                {3,4},{4,4},{5,4},
                {3,3},{4,3},{5,3},
                {3,2},{4,2},{5,2},
                {3,1},{4,1},{5,1},
                {3,0},{4,0},{5,0}
            },
            "BuffArrow_Generic"
        );
    }

    private static Sprite CreateMaxHealthArrow()
    {
        return CreateSpriteFromPixels(
            11,
            13,
            new int[,]
            {
                {5,12},
                {4,11},{5,11},{6,11},
                {3,10},{4,10},{5,10},{6,10},{7,10},
                {4,9},{5,9},{6,9},
                {4,8},{5,8},{6,8},
                {4,7},{5,7},{6,7},

                {5,5},
                {5,4},
                {3,3},{4,3},{5,3},{6,3},{7,3},
                {5,2},
                {5,1}
            },
            "BuffArrow_MaxHealth"
        );
    }

    private static Sprite CreateDamageArrow()
    {
        return CreateSpriteFromPixels(
            11,
            13,
            new int[,]
            {
                {5,12},
                {4,11},{5,11},{6,11},
                {3,10},{4,10},{5,10},{6,10},{7,10},
                {2,9},{3,9},{4,9},{5,9},{6,9},{7,9},{8,9},
                {4,8},{5,8},{6,8},
                {4,7},{5,7},{6,7},
                {4,6},{5,6},{6,6},
                {3,5},{4,5},{5,5},{6,5},{7,5},
                {4,4},{5,4},{6,4},
                {5,3},
                {5,2},
                {5,1},
                {5,0}
            },
            "BuffArrow_Damage"
        );
    }

    private static Sprite CreateCooldownArrow()
    {
        return CreateSpriteFromPixels(
            11,
            13,
            new int[,]
            {
                {3,12},{7,12},
                {2,11},{3,11},{4,11},{6,11},{7,11},{8,11},
                {1,10},{2,10},{3,10},{4,10},{5,10},{6,10},{7,10},{8,10},{9,10},
                {3,9},{7,9},
                {3,8},{7,8},
                {3,7},{7,7},
                {3,6},{7,6},
                {3,5},{7,5},
                {3,4},{7,4}
            },
            "BuffArrow_Cooldown"
        );
    }

    private static Sprite CreateManaArrow()
    {
        return CreateSpriteFromPixels(
            11,
            13,
            new int[,]
            {
                {5,12},
                {4,11},{5,11},{6,11},
                {3,10},{4,10},{5,10},{6,10},{7,10},
                {4,9},{5,9},{6,9},
                {4,8},{5,8},{6,8},
                {4,7},{5,7},{6,7},

                {5,5},
                {4,4},{5,4},{6,4},
                {3,3},{4,3},{5,3},{6,3},{7,3},
                {4,2},{5,2},{6,2},
                {5,1}
            },
            "BuffArrow_Mana"
        );
    }

    private static Sprite CreateSpriteFromPixels(
        int width,
        int height,
        int[,] pixels,
        string spriteName)
    {
        Texture2D texture =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );

        texture.name =
            spriteName +
            "_Texture";

        texture.filterMode =
            FilterMode.Point;

        texture.wrapMode =
            TextureWrapMode.Clamp;

        Color clear =
            new Color(
                0f,
                0f,
                0f,
                0f
            );

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                texture.SetPixel(
                    x,
                    y,
                    clear
                );
            }
        }

        int count =
            pixels.GetLength(
                0
            );

        for (int i = 0;
             i < count;
             i++)
        {
            int x =
                pixels[i, 0];

            int y =
                pixels[i, 1];

            if (x < 0 ||
                x >= width ||
                y < 0 ||
                y >= height)
            {
                continue;
            }

            texture.SetPixel(
                x,
                y,
                Color.white
            );
        }

        texture.Apply();

        Sprite sprite =
            Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    width,
                    height
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                16f
            );

        sprite.name =
            spriteName;

        return sprite;
    }
}
