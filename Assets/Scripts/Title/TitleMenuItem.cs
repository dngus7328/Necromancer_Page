using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleMenuItem : MonoBehaviour, IPointerEnterHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text menuText;
    [SerializeField] private Button button;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Scale")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float scaleLerpSpeed = 12f;

    [Header("Floating")]
    [SerializeField] private float floatAmplitude = 3f;
    [SerializeField] private float floatSpeed = 3f;

    private TitleMenuController controller;
    private int index;

    private RectTransform textRect;
    private Vector3 targetScale;
    private Vector2 baseAnchoredPosition;

    private bool isSelected;
    private float randomOffset;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (menuText == null)
            menuText = GetComponentInChildren<TMP_Text>();

        if (menuText != null)
        {
            textRect = menuText.rectTransform;

            // 왼쪽 기준, 세로 중앙 기준
            textRect.pivot = new Vector2(0f, 0.5f);

            // 글자 위치 저장
            baseAnchoredPosition = textRect.anchoredPosition;

            targetScale = Vector3.one * normalScale;
            textRect.localScale = targetScale;
        }

        randomOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (textRect == null)
            return;

        // 크기 보간
        textRect.localScale = Vector3.Lerp(
            textRect.localScale,
            targetScale,
            1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime)
        );

        // 위치는 기본적으로 고정, 선택되었을 때만 Y로 살짝 떠오름
        Vector2 pos = baseAnchoredPosition;

        if (isSelected)
        {
            float yOffset = Mathf.Sin(Time.unscaledTime * floatSpeed + randomOffset) * floatAmplitude;
            pos.y = baseAnchoredPosition.y + yOffset;
        }

        textRect.anchoredPosition = pos;
    }

    public void Setup(TitleMenuController menuController, int itemIndex)
    {
        controller = menuController;
        index = itemIndex;

        if (button == null)
            button = GetComponent<Button>();

        if (menuText == null)
            menuText = GetComponentInChildren<TMP_Text>();

        if (menuText != null && textRect == null)
        {
            textRect = menuText.rectTransform;
            textRect.pivot = new Vector2(0f, 0.5f);
            baseAnchoredPosition = textRect.anchoredPosition;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (menuText == null)
            return;

        menuText.color = selected ? selectedColor : unselectedColor;
        targetScale = Vector3.one * (selected ? selectedScale : normalScale);

        if (!selected && textRect != null)
        {
            textRect.anchoredPosition = baseAnchoredPosition;
        }
    }

    public RectTransform GetRectTransform()
    {
        return GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null)
            controller.SelectItem(index);
    }

    public void Press()
    {
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
    }
}