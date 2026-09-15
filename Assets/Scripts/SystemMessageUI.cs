using System.Collections;
using TMPro;
using UnityEngine;

public class SystemMessageUI : MonoBehaviour
{
    public enum MessageType
    {
        NotEnoughMana,
        CapacityFull,
        Cooldown,
        InvalidPosition
    }

    public static SystemMessageUI Instance { get; private set; }

    [Header("참조")]
    [SerializeField]
    private TMP_Text messageText;

    [Header("위치")]
    [Tooltip("하단 중앙 기준 시작 위치")]
    [SerializeField]
    private Vector2 startAnchoredPosition =
        new Vector2(
            0f,
            170f
        );

    [Tooltip("메시지가 위로 올라가는 거리")]
    [SerializeField]
    private float riseDistance = 45f;

    [Header("시간")]
    [SerializeField]
    private float fadeInTime = 0.08f;

    [SerializeField]
    private float stayTime = 0.75f;

    [SerializeField]
    private float fadeOutTime = 0.30f;

    [Header("색상")]
    [SerializeField]
    private Color manaColor =
        new Color(
            0.72f,
            0.62f,
            1f,
            1f
        );

    [SerializeField]
    private Color capacityColor =
        new Color(
            1f,
            0.72f,
            0.36f,
            1f
        );

    [SerializeField]
    private Color cooldownColor =
        new Color(
            0.92f,
            0.88f,
            0.62f,
            1f
        );

    [SerializeField]
    private Color invalidPositionColor =
        new Color(
            1f,
            0.48f,
            0.48f,
            1f
        );

    private RectTransform rectTransform;

    private Coroutine messageCoroutine;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }

        Instance =
            this;

        if (messageText == null)
        {
            messageText =
                GetComponent<TMP_Text>();
        }

        if (messageText == null)
        {
            Debug.LogError(
                "[SystemMessageUI] TMP_Text를 찾을 수 없습니다."
            );

            enabled =
                false;

            return;
        }

        rectTransform =
            messageText.rectTransform;

        messageText.text =
            string.Empty;

        messageText.raycastTarget =
            false;

        SetAlpha(
            0f
        );
    }

    public void Show(
        MessageType type)
    {
        string message =
            GetMessage(
                type
            );

        Color color =
            GetColor(
                type
            );

        ShowMessage(
            message,
            color
        );
    }

    public void ShowMessage(
        string message)
    {
        ShowMessage(
            message,
            Color.white
        );
    }

    private void ShowMessage(
        string message,
        Color color)
    {
        if (messageText == null ||
            string.IsNullOrWhiteSpace(
                message
            ))
        {
            return;
        }

        if (messageCoroutine != null)
        {
            StopCoroutine(
                messageCoroutine
            );
        }

        messageCoroutine =
            StartCoroutine(
                ShowRoutine(
                    message,
                    color
                )
            );
    }

    private IEnumerator ShowRoutine(
        string message,
        Color color)
    {
        messageText.text =
            message;

        messageText.color =
            new Color(
                color.r,
                color.g,
                color.b,
                0f
            );

        rectTransform.anchoredPosition =
            startAnchoredPosition;

        float totalTime =
            Mathf.Max(
                0.01f,
                fadeInTime +
                stayTime +
                fadeOutTime
            );

        float timer =
            0f;

        while (timer <
               totalTime)
        {
            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    totalTime
                );

            Vector2 position =
                startAnchoredPosition;

            position.y +=
                riseDistance *
                progress;

            rectTransform.anchoredPosition =
                position;

            float alpha;

            if (timer <
                fadeInTime)
            {
                alpha =
                    fadeInTime <= 0f
                        ? 1f
                        : timer /
                          fadeInTime;
            }
            else if (timer <
                     fadeInTime +
                     stayTime)
            {
                alpha =
                    1f;
            }
            else
            {
                float fadeTimer =
                    timer -
                    fadeInTime -
                    stayTime;

                alpha =
                    fadeOutTime <= 0f
                        ? 0f
                        : 1f -
                          fadeTimer /
                          fadeOutTime;
            }

            SetAlpha(
                Mathf.Clamp01(
                    alpha
                )
            );

            yield return null;
        }

        SetAlpha(
            0f
        );

        messageText.text =
            string.Empty;

        rectTransform.anchoredPosition =
            startAnchoredPosition;

        messageCoroutine =
            null;
    }

    private string GetMessage(
        MessageType type)
    {
        switch (type)
        {
            case MessageType.NotEnoughMana:
                return "마나가 부족합니다!";

            case MessageType.CapacityFull:
                return "소환 용량이 최대치입니다!";

            case MessageType.Cooldown:
                return "아직 재소환할 수 없습니다!";

            case MessageType.InvalidPosition:
                return "이 위치에는 소환할 수 없습니다!";
        }

        return string.Empty;
    }

    private Color GetColor(
        MessageType type)
    {
        switch (type)
        {
            case MessageType.NotEnoughMana:
                return manaColor;

            case MessageType.CapacityFull:
                return capacityColor;

            case MessageType.Cooldown:
                return cooldownColor;

            case MessageType.InvalidPosition:
                return invalidPositionColor;
        }

        return Color.white;
    }

    private void SetAlpha(
        float alpha)
    {
        if (messageText == null)
        {
            return;
        }

        Color color =
            messageText.color;

        color.a =
            alpha;

        messageText.color =
            color;
    }

    private void OnDestroy()
    {
        if (Instance ==
            this)
        {
            Instance =
                null;
        }
    }
}
