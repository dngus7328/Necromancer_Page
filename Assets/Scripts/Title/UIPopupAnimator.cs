using System.Collections;
using UnityEngine;

public class UIPopupAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform popupRect;

    [Header("Open Animation")]
    [SerializeField] private float openDuration = 0.18f;
    [SerializeField] private float startScale = 0.90f;

    [Header("Close Animation")]
    [SerializeField] private float closeDuration = 0.12f;
    [SerializeField] private float closeScale = 0.94f;

    [Header("Pixel Step")]
    [Tooltip("애니메이션이 몇 단계로 끊겨서 보일지 설정")]
    [Range(2, 10)]
    [SerializeField] private int animationSteps = 5;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (popupRect == null)
            popupRect = GetComponent<RectTransform>();
    }

    public void Open()
    {
        gameObject.SetActive(true);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (!gameObject.activeSelf)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        if (canvasGroup == null || popupRect == null)
            yield break;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        popupRect.localScale =
            Vector3.one * startScale;

        float time = 0f;

        while (time < openDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                time / openDuration
            );

            // 0~1 값을 몇 단계로 끊음
            float steppedT =
                Mathf.Floor(t * animationSteps) /
                animationSteps;

            canvasGroup.alpha = steppedT;

            float scale =
                Mathf.Lerp(
                    startScale,
                    1f,
                    steppedT
                );

            popupRect.localScale =
                Vector3.one * scale;

            yield return null;
        }

        canvasGroup.alpha = 1f;
        popupRect.localScale = Vector3.one;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        animationCoroutine = null;
    }

    private IEnumerator CloseRoutine()
    {
        if (canvasGroup == null || popupRect == null)
            yield break;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float startAlpha = canvasGroup.alpha;
        float startScaleValue = popupRect.localScale.x;

        float time = 0f;

        while (time < closeDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                time / closeDuration
            );

            // 닫힐 때도 같은 방식으로 단계화
            float steppedT =
                Mathf.Floor(t * animationSteps) /
                animationSteps;

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    steppedT
                );

            float scale =
                Mathf.Lerp(
                    startScaleValue,
                    closeScale,
                    steppedT
                );

            popupRect.localScale =
                Vector3.one * scale;

            yield return null;
        }

        canvasGroup.alpha = 0f;
        popupRect.localScale = Vector3.one;

        animationCoroutine = null;

        gameObject.SetActive(false);
    }
}