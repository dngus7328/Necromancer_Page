using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum PixelFadePattern
{
    Scatter,
    BottomUp,
    EdgeIn,
    CenterOut
}

[RequireComponent(typeof(RawImage))]
public class PixelFadeOverlay : MonoBehaviour
{
    public bool IsPlaying
    {
        get;
        private set;
    }

    private RawImage rawImage;
    private Texture2D texture;

    private Color[] pixels;
    private int[] order;

    private Coroutine transitionCoroutine;

    private int currentWidth = -1;
    private int currentHeight = -1;

    private readonly Color clear =
        new Color(
            0f,
            0f,
            0f,
            0f
        );

    public static PixelFadeOverlay GetOrCreate(
        Transform requester)
    {
        if (requester == null)
        {
            return null;
        }

        Canvas canvas =
            requester.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            canvas =
                UnityEngine.Object.FindObjectOfType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning(
                "[PixelFadeOverlay] Canvas를 찾지 못했습니다."
            );

            return null;
        }

        Canvas rootCanvas =
            canvas.rootCanvas != null
                ? canvas.rootCanvas
                : canvas;

        PixelFadeOverlay existing =
            rootCanvas.GetComponentInChildren<PixelFadeOverlay>(
                true
            );

        if (existing != null)
        {
            existing.transform.SetAsLastSibling();

            return existing;
        }

        GameObject obj =
            new GameObject(
                "Runtime_PixelFadeOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(PixelFadeOverlay)
            );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.SetParent(
            rootCanvas.transform,
            false
        );

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        obj.transform.SetAsLastSibling();

        PixelFadeOverlay overlay =
            obj.GetComponent<PixelFadeOverlay>();

        overlay.Initialize();

        return overlay;
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (rawImage == null)
        {
            rawImage =
                GetComponent<RawImage>();
        }

        if (rawImage != null)
        {
            rawImage.color =
                Color.white;

            rawImage.raycastTarget =
                true;

            rawImage.enabled =
                false;
        }
    }

    public void PlayTransition(
        float coverDuration,
        float holdDuration,
        float revealDuration,
        Color fadeColor,
        int gridWidth,
        int gridHeight,
        Action onCovered)
    {
        PlayTransition(
            coverDuration,
            holdDuration,
            revealDuration,
            fadeColor,
            gridWidth,
            gridHeight,
            PixelFadePattern.Scatter,
            onCovered
        );
    }

    public void PlayTransition(
        float coverDuration,
        float holdDuration,
        float revealDuration,
        Color fadeColor,
        int gridWidth,
        int gridHeight,
        PixelFadePattern pattern,
        Action onCovered)
    {
        Initialize();

        EnsureTexture(
            gridWidth,
            gridHeight
        );

        BuildOrder(
            pattern
        );

        if (transitionCoroutine != null)
        {
            StopCoroutine(
                transitionCoroutine
            );
        }

        transform.SetAsLastSibling();

        transitionCoroutine =
            StartCoroutine(
                TransitionRoutine(
                    Mathf.Max(
                        0.01f,
                        coverDuration
                    ),
                    Mathf.Max(
                        0f,
                        holdDuration
                    ),
                    Mathf.Max(
                        0.01f,
                        revealDuration
                    ),
                    fadeColor,
                    onCovered
                )
            );
    }

    private IEnumerator TransitionRoutine(
        float coverDuration,
        float holdDuration,
        float revealDuration,
        Color fadeColor,
        Action onCovered)
    {
        IsPlaying =
            true;

        rawImage.enabled =
            true;

        rawImage.raycastTarget =
            true;

        ApplyCoverage(
            0,
            fadeColor
        );

        yield return AnimateCoverage(
            0,
            order.Length,
            coverDuration,
            fadeColor
        );

        onCovered?.Invoke();

        if (holdDuration > 0f)
        {
            float timer =
                0f;

            while (timer <
                   holdDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                yield return null;
            }
        }

        yield return AnimateCoverage(
            order.Length,
            0,
            revealDuration,
            fadeColor
        );

        rawImage.enabled =
            false;

        rawImage.raycastTarget =
            false;

        transitionCoroutine =
            null;

        IsPlaying =
            false;
    }

    private IEnumerator AnimateCoverage(
        int fromCount,
        int toCount,
        float duration,
        Color fadeColor)
    {
        float elapsed =
            0f;

        int previousCount =
            -1;

        while (elapsed <
               duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            // 일반 알파 페이드가 아니라 블록 수 자체가 단계적으로 변합니다.
            int count =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        fromCount,
                        toCount,
                        t
                    )
                );

            if (count !=
                previousCount)
            {
                ApplyCoverage(
                    count,
                    fadeColor
                );

                previousCount =
                    count;
            }

            yield return null;
        }

        ApplyCoverage(
            toCount,
            fadeColor
        );
    }

    private void EnsureTexture(
        int width,
        int height)
    {
        width =
            Mathf.Clamp(
                width,
                8,
                256
            );

        height =
            Mathf.Clamp(
                height,
                8,
                144
            );

        if (texture != null &&
            currentWidth == width &&
            currentHeight == height)
        {
            return;
        }

        currentWidth =
            width;

        currentHeight =
            height;

        if (texture != null)
        {
            Destroy(
                texture
            );
        }

        texture =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );

        texture.name =
            "Runtime_PixelFadeTexture";

        texture.filterMode =
            FilterMode.Point;

        texture.wrapMode =
            TextureWrapMode.Clamp;

        pixels =
            new Color[
                width *
                height
            ];

        order =
            new int[
                pixels.Length
            ];

        for (int i = 0;
             i < pixels.Length;
             i++)
        {
            pixels[i] =
                clear;

            order[i] =
                i;
        }

        texture.SetPixels(
            pixels
        );

        texture.Apply(
            false,
            false
        );

        rawImage.texture =
            texture;
    }

    private void BuildOrder(
        PixelFadePattern pattern)
    {
        if (order == null ||
            order.Length == 0)
        {
            return;
        }

        int width =
            currentWidth;

        int height =
            currentHeight;

        for (int i = 0;
             i < order.Length;
             i++)
        {
            order[i] =
                i;
        }

        // 현재 강화 선택 전환은 Scatter만 사용합니다.
        // 아래 나머지 패턴도 혹시 호출되더라도
        // 비교 함수 안에서 Random을 쓰지 않도록 결정적으로 정렬합니다.
        switch (pattern)
        {
            case PixelFadePattern.BottomUp:
                System.Array.Sort(
                    order,
                    (a, b) =>
                    {
                        int ay =
                            a / width;

                        int by =
                            b / width;

                        int compareY =
                            ay.CompareTo(
                                by
                            );

                        if (compareY != 0)
                        {
                            return compareY;
                        }

                        int ax =
                            a % width;

                        int bx =
                            b % width;

                        return ax.CompareTo(
                            bx
                        );
                    }
                );
                break;

            case PixelFadePattern.EdgeIn:
                System.Array.Sort(
                    order,
                    (a, b) =>
                    {
                        int ax =
                            a % width;

                        int ay =
                            a / width;

                        int bx =
                            b % width;

                        int by =
                            b / width;

                        int aEdge =
                            Mathf.Min(
                                Mathf.Min(
                                    ax,
                                    width - 1 - ax
                                ),
                                Mathf.Min(
                                    ay,
                                    height - 1 - ay
                                )
                            );

                        int bEdge =
                            Mathf.Min(
                                Mathf.Min(
                                    bx,
                                    width - 1 - bx
                                ),
                                Mathf.Min(
                                    by,
                                    height - 1 - by
                                )
                            );

                        int compareEdge =
                            aEdge.CompareTo(
                                bEdge
                            );

                        if (compareEdge != 0)
                        {
                            return compareEdge;
                        }

                        return a.CompareTo(
                            b
                        );
                    }
                );
                break;

            case PixelFadePattern.CenterOut:
                float centerX =
                    (width - 1) *
                    0.5f;

                float centerY =
                    (height - 1) *
                    0.5f;

                System.Array.Sort(
                    order,
                    (a, b) =>
                    {
                        float ax =
                            a % width;

                        float ay =
                            a / width;

                        float bx =
                            b % width;

                        float by =
                            b / width;

                        float aDistance =
                            (
                                (ax - centerX) *
                                (ax - centerX)
                            ) +
                            (
                                (ay - centerY) *
                                (ay - centerY)
                            );

                        float bDistance =
                            (
                                (bx - centerX) *
                                (bx - centerX)
                            ) +
                            (
                                (by - centerY) *
                                (by - centerY)
                            );

                        int compareDistance =
                            aDistance.CompareTo(
                                bDistance
                            );

                        if (compareDistance != 0)
                        {
                            return compareDistance;
                        }

                        return a.CompareTo(
                            b
                        );
                    }
                );
                break;

            default:
                // 안전한 Fisher-Yates 셔플.
                for (int i =
                         order.Length - 1;
                     i > 0;
                     i--)
                {
                    int j =
                        UnityEngine.Random.Range(
                            0,
                            i + 1
                        );

                    int temp =
                        order[i];

                    order[i] =
                        order[j];

                    order[j] =
                        temp;
                }
                break;
        }
    }

    private void ApplyCoverage(
        int filledCount,
        Color fadeColor)
    {
        if (texture == null ||
            pixels == null ||
            order == null)
        {
            return;
        }

        filledCount =
            Mathf.Clamp(
                filledCount,
                0,
                order.Length
            );

        for (int i = 0;
             i < pixels.Length;
             i++)
        {
            pixels[i] =
                clear;
        }

        Color solidColor =
            fadeColor;

        solidColor.a =
            Mathf.Clamp01(
                fadeColor.a
            );

        for (int i = 0;
             i < filledCount;
             i++)
        {
            pixels[
                order[i]
            ] =
                solidColor;
        }

        texture.SetPixels(
            pixels
        );

        texture.Apply(
            false,
            false
        );
    }

    private void OnDestroy()
    {
        if (texture != null)
        {
            Destroy(
                texture
            );
        }
    }
}
