using UnityEngine;

public class ProceduralSlashEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private Color baseColor;

    private Vector3 startPosition;
    private Vector3 moveOffset;

    private Vector3 baseScale;

    private float startScaleMultiplier;
    private float endScaleMultiplier;

    private float lifetime;
    private float elapsedTime;

    private AnimationCurve alphaCurve;

    private bool initialized;

    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                gameObject.AddComponent<SpriteRenderer>();
        }
    }

    public void Initialize(
        Sprite sprite,
        Color color,
        Vector2 worldSize,
        Vector2 moveDirection,
        float moveDistance,
        float effectLifetime,
        float startScale,
        float endScale,
        int sortingLayerID,
        int sortingOrder,
        AnimationCurve customAlphaCurve)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (sprite == null)
        {
            Destroy(gameObject);
            return;
        }

        spriteRenderer.sprite =
            sprite;

        spriteRenderer.sortingLayerID =
            sortingLayerID;

        spriteRenderer.sortingOrder =
            sortingOrder;

        baseColor =
            color;

        spriteRenderer.color =
            color;

        startPosition =
            transform.position;

        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            moveDirection =
                Vector2.right;
        }

        moveDirection.Normalize();

        moveOffset =
            (Vector3)(moveDirection * moveDistance);

        lifetime =
            Mathf.Max(0.01f, effectLifetime);

        startScaleMultiplier =
            startScale;

        endScaleMultiplier =
            endScale;

        if (customAlphaCurve != null)
        {
            alphaCurve =
                new AnimationCurve(
                    customAlphaCurve.keys
                );
        }
        else
        {
            alphaCurve =
                AnimationCurve.EaseInOut(
                    0f, 1f,
                    1f, 0f
                );
        }

        Vector2 spriteWorldSize =
            sprite.bounds.size;

        if (spriteWorldSize.x <= 0f)
        {
            spriteWorldSize.x = 1f;
        }

        if (spriteWorldSize.y <= 0f)
        {
            spriteWorldSize.y = 1f;
        }

        baseScale =
            new Vector3(
                worldSize.x / spriteWorldSize.x,
                worldSize.y / spriteWorldSize.y,
                1f
            );

        transform.localScale =
            baseScale * startScaleMultiplier;

        elapsedTime = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        float t =
            Mathf.Clamp01(
                elapsedTime / lifetime
            );

        transform.position =
            startPosition +
            moveOffset * t;

        float scale =
            Mathf.Lerp(
                startScaleMultiplier,
                endScaleMultiplier,
                t
            );

        transform.localScale =
            baseScale * scale;

        float alpha =
            alphaCurve != null
                ? alphaCurve.Evaluate(t)
                : 1f - t;

        Color color =
            baseColor;

        color.a *= alpha;

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                color;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}