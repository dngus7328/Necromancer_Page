using System.Collections.Generic;
using UnityEngine;

public enum RuntimePixelSpriteType
{
    ArrowGeneric,
    ArrowHealth,
    ArrowDamage,
    ArrowCooldown,
    ArrowMana,
    ArrowShield,
    ArrowSpeed,
    ArrowSpirit,

    HealCross,
    ShieldRing,
    ShieldShard,
    DustPuff,
    Spark,
    RuneShard,
    Diamond,
    RingSmall,
    RingLarge,
    GhostOrb,
    GhostTrail,
    BoneShard,
    SkullMark,
    GroundCrack
}

public static class RuntimePixelEffectFactory
{
    private static readonly Dictionary<RuntimePixelSpriteType, Sprite> cache =
        new Dictionary<RuntimePixelSpriteType, Sprite>();

    public static Sprite GetSprite(RuntimePixelSpriteType type)
    {
        if (cache.TryGetValue(type, out Sprite sprite) &&
            sprite != null)
        {
            return sprite;
        }

        sprite = CreateSprite(type);
        cache[type] = sprite;

        return sprite;
    }

    private static Sprite CreateSprite(RuntimePixelSpriteType type)
    {
        switch (type)
        {
            case RuntimePixelSpriteType.ArrowHealth:
                return Create(
                    11, 13,
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
                    "ArrowHealth"
                );

            case RuntimePixelSpriteType.ArrowDamage:
                return Create(
                    11, 13,
                    new int[,]
                    {
                        {5,12},
                        {4,11},{5,11},{6,11},
                        {3,10},{4,10},{5,10},{6,10},{7,10},
                        {2,9},{3,9},{4,9},{5,9},{6,9},{7,9},{8,9},
                        {4,8},{5,8},{6,8},
                        {4,7},{5,7},{6,7},
                        {3,6},{4,6},{5,6},{6,6},{7,6},
                        {4,5},{5,5},{6,5},
                        {5,4},
                        {5,3},
                        {5,2},
                        {5,1}
                    },
                    "ArrowDamage"
                );

            case RuntimePixelSpriteType.ArrowCooldown:
                return Create(
                    11, 13,
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
                    "ArrowCooldown"
                );

            case RuntimePixelSpriteType.ArrowMana:
                return Create(
                    11, 13,
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
                    "ArrowMana"
                );

            case RuntimePixelSpriteType.ArrowShield:
                return Create(
                    11, 13,
                    new int[,]
                    {
                        {5,12},
                        {4,11},{5,11},{6,11},
                        {3,10},{4,10},{5,10},{6,10},{7,10},
                        {4,9},{5,9},{6,9},
                        {4,8},{5,8},{6,8},
                        {2,5},{3,5},{4,5},{5,5},{6,5},{7,5},{8,5},
                        {2,4},{8,4},
                        {2,3},{3,3},{7,3},{8,3},
                        {3,2},{4,2},{6,2},{7,2},
                        {4,1},{5,1},{6,1},
                        {5,0}
                    },
                    "ArrowShield"
                );

            case RuntimePixelSpriteType.ArrowSpeed:
                return Create(
                    13, 11,
                    new int[,]
                    {
                        {8,10},
                        {7,9},{8,9},{9,9},
                        {6,8},{7,8},{8,8},{9,8},{10,8},
                        {8,7},{9,7},{10,7},
                        {9,6},{10,6},{11,6},
                        {3,5},{4,5},{5,5},{6,5},{7,5},{8,5},{9,5},{10,5},{11,5},{12,5},
                        {2,4},{3,4},{4,4},{5,4},{6,4},{7,4},{8,4},
                        {1,3},{2,3},{3,3},{4,3},{5,3},{6,3},
                        {0,2},{1,2},{2,2},{3,2},{4,2}
                    },
                    "ArrowSpeed"
                );

            case RuntimePixelSpriteType.ArrowSpirit:
                return Create(
                    11, 13,
                    new int[,]
                    {
                        {5,12},
                        {4,11},{5,11},{6,11},
                        {3,10},{4,10},{5,10},{6,10},{7,10},
                        {4,9},{5,9},{6,9},
                        {4,8},{5,8},{6,8},
                        {5,6},
                        {3,5},{4,5},{5,5},{6,5},{7,5},
                        {2,4},{4,4},{6,4},{8,4},
                        {3,3},{5,3},{7,3},
                        {4,2},{6,2},
                        {5,1}
                    },
                    "ArrowSpirit"
                );

            case RuntimePixelSpriteType.HealCross:
                return Create(
                    9, 9,
                    new int[,]
                    {
                        {3,8},{4,8},{5,8},
                        {3,7},{4,7},{5,7},
                        {3,6},{4,6},{5,6},
                        {0,5},{1,5},{2,5},{3,5},{4,5},{5,5},{6,5},{7,5},{8,5},
                        {0,4},{1,4},{2,4},{3,4},{4,4},{5,4},{6,4},{7,4},{8,4},
                        {0,3},{1,3},{2,3},{3,3},{4,3},{5,3},{6,3},{7,3},{8,3},
                        {3,2},{4,2},{5,2},
                        {3,1},{4,1},{5,1},
                        {3,0},{4,0},{5,0}
                    },
                    "HealCross"
                );

            case RuntimePixelSpriteType.ShieldRing:
                return CreateRing(15, "ShieldRing");

            case RuntimePixelSpriteType.ShieldShard:
                return Create(
                    7, 9,
                    new int[,]
                    {
                        {3,8},
                        {2,7},{3,7},{4,7},
                        {1,6},{2,6},{3,6},{4,6},{5,6},
                        {2,5},{3,5},{4,5},
                        {2,4},{3,4},{4,4},
                        {3,3},
                        {3,2},
                        {2,1},{3,1}
                    },
                    "ShieldShard"
                );

            case RuntimePixelSpriteType.DustPuff:
                return Create(
                    9, 7,
                    new int[,]
                    {
                        {2,6},{3,6},{5,6},{6,6},
                        {1,5},{2,5},{3,5},{4,5},{5,5},{6,5},{7,5},
                        {0,4},{1,4},{2,4},{3,4},{4,4},{5,4},{6,4},{7,4},{8,4},
                        {1,3},{2,3},{3,3},{4,3},{5,3},{6,3},{7,3},
                        {2,2},{3,2},{4,2},{5,2},{6,2},
                        {3,1},{4,1},{5,1}
                    },
                    "DustPuff"
                );

            case RuntimePixelSpriteType.Spark:
                return Create(
                    7, 7,
                    new int[,]
                    {
                        {3,6},
                        {3,5},
                        {2,4},{3,4},{4,4},
                        {0,3},{1,3},{2,3},{3,3},{4,3},{5,3},{6,3},
                        {2,2},{3,2},{4,2},
                        {3,1},
                        {3,0}
                    },
                    "Spark"
                );

            case RuntimePixelSpriteType.RuneShard:
                return Create(
                    7, 9,
                    new int[,]
                    {
                        {3,8},
                        {2,7},{3,7},{4,7},
                        {2,6},{4,6},
                        {1,5},{3,5},{5,5},
                        {2,4},{4,4},
                        {2,3},{3,3},{4,3},
                        {3,2},
                        {3,1}
                    },
                    "RuneShard"
                );

            case RuntimePixelSpriteType.Diamond:
                return Create(
                    7, 7,
                    new int[,]
                    {
                        {3,6},
                        {2,5},{3,5},{4,5},
                        {1,4},{2,4},{3,4},{4,4},{5,4},
                        {0,3},{1,3},{2,3},{3,3},{4,3},{5,3},{6,3},
                        {1,2},{2,2},{3,2},{4,2},{5,2},
                        {2,1},{3,1},{4,1},
                        {3,0}
                    },
                    "Diamond"
                );

            case RuntimePixelSpriteType.RingSmall:
                return CreateRing(13, "RingSmall");

            case RuntimePixelSpriteType.RingLarge:
                return CreateRing(19, "RingLarge");

            case RuntimePixelSpriteType.GhostOrb:
                return Create(
                    11, 11,
                    new int[,]
                    {
                        {4,10},{5,10},{6,10},
                        {2,9},{3,9},{4,9},{5,9},{6,9},{7,9},{8,9},
                        {1,8},{2,8},{3,8},{4,8},{5,8},{6,8},{7,8},{8,8},{9,8},
                        {1,7},{2,7},{3,7},{4,7},{5,7},{6,7},{7,7},{8,7},{9,7},
                        {0,6},{1,6},{2,6},{3,6},{4,6},{5,6},{6,6},{7,6},{8,6},{9,6},{10,6},
                        {1,5},{2,5},{3,5},{4,5},{5,5},{6,5},{7,5},{8,5},{9,5},
                        {1,4},{2,4},{3,4},{4,4},{5,4},{6,4},{7,4},{8,4},{9,4},
                        {2,3},{3,3},{4,3},{5,3},{6,3},{7,3},{8,3},
                        {3,2},{4,2},{5,2},{6,2},{7,2},
                        {4,1},{5,1},{6,1},
                        {5,0}
                    },
                    "GhostOrb"
                );

            case RuntimePixelSpriteType.GhostTrail:
                return Create(
                    9, 5,
                    new int[,]
                    {
                        {0,2},{1,2},{2,2},{3,2},{4,2},{5,2},{6,2},{7,2},{8,2},
                        {2,3},{3,3},{4,3},{5,3},{6,3},
                        {2,1},{3,1},{4,1},{5,1},{6,1}
                    },
                    "GhostTrail"
                );

            case RuntimePixelSpriteType.BoneShard:
                return Create(
                    9, 5,
                    new int[,]
                    {
                        {1,4},{2,4},
                        {0,3},{1,3},{2,3},{3,3},{4,3},{5,3},{6,3},{7,3},
                        {1,2},{2,2},{3,2},{4,2},{5,2},{6,2},{7,2},{8,2},
                        {6,1},{7,1},{8,1},
                        {7,0},{8,0}
                    },
                    "BoneShard"
                );

            case RuntimePixelSpriteType.SkullMark:
                return Create(
                    11, 11,
                    new int[,]
                    {
                        {3,10},{4,10},{5,10},{6,10},{7,10},
                        {2,9},{3,9},{4,9},{5,9},{6,9},{7,9},{8,9},
                        {1,8},{2,8},{3,8},{4,8},{5,8},{6,8},{7,8},{8,8},{9,8},
                        {1,7},{2,7},{3,7},{4,7},{5,7},{6,7},{7,7},{8,7},{9,7},
                        {2,6},{3,6},{4,6},{5,6},{6,6},{7,6},{8,6},
                        {2,5},{3,5},{7,5},{8,5},
                        {2,4},{3,4},{4,4},{6,4},{7,4},{8,4},
                        {3,3},{4,3},{5,3},{6,3},{7,3},
                        {4,2},{5,2},{6,2},
                        {4,1},{6,1}
                    },
                    "SkullMark"
                );

            case RuntimePixelSpriteType.GroundCrack:
                return Create(
                    13, 7,
                    new int[,]
                    {
                        {6,6},
                        {5,5},{6,5},
                        {4,4},{5,4},{7,4},
                        {3,3},{4,3},{7,3},{8,3},{9,3},
                        {1,2},{2,2},{3,2},{9,2},{10,2},
                        {0,1},{1,1},{10,1},{11,1},{12,1}
                    },
                    "GroundCrack"
                );

            default:
                return Create(
                    9, 11,
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
                    "ArrowGeneric"
                );
        }
    }

    private static Sprite CreateRing(int size, string name)
    {
        Texture2D texture =
            NewTexture(size, size, name + "_Texture");

        float center =
            (size - 1) * 0.5f;

        float radius =
            center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx =
                    x - center;

                float dy =
                    y - center;

                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dy * dy
                    );

                if (distance >= radius - 0.8f &&
                    distance <= radius + 0.8f)
                {
                    texture.SetPixel(
                        x,
                        y,
                        Color.white
                    );
                }
            }
        }

        return FinishTexture(
            texture,
            name
        );
    }

    private static Sprite Create(
        int width,
        int height,
        int[,] pixels,
        string name)
    {
        Texture2D texture =
            NewTexture(
                width,
                height,
                name + "_Texture"
            );

        int count =
            pixels.GetLength(0);

        for (int i = 0; i < count; i++)
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

        return FinishTexture(
            texture,
            name
        );
    }

    private static Texture2D NewTexture(
        int width,
        int height,
        string name)
    {
        Texture2D texture =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );

        texture.name =
            name;

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

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(
                    x,
                    y,
                    clear
                );
            }
        }

        return texture;
    }

    private static Sprite FinishTexture(
        Texture2D texture,
        string spriteName)
    {
        texture.Apply();

        Sprite sprite =
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
                16f
            );

        sprite.name =
            spriteName;

        return sprite;
    }
}
