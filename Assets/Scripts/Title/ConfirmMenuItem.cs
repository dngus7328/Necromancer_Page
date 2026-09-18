using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfirmMenuItem : MonoBehaviour, IPointerEnterHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text menuText;
    [SerializeField] private Button button;

    [Header("Selection Marker")]
    [SerializeField] private RectTransform selectionMarker;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField]
    private Color unselectedColor =
        new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Scale")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float scaleLerpSpeed = 12f;

    [Header("Floating")]
    [SerializeField] private float floatAmplitude = 2f;
    [SerializeField] private float floatSpeed = 3f;

    private ConfirmMenuController controller;
    private int index;

    private RectTransform textRect;
    private Vector2 baseTextPosition;
    private Vector3 targetScale;

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

            textRect.pivot = new Vector2(0f, 0.5f);

            baseTextPosition = textRect.anchoredPosition;

            targetScale = Vector3.one * normalScale;
            textRect.localScale = targetScale;
        }

        if (selectionMarker != null)
        {
            selectionMarker.gameObject.SetActive(false);
        }

        randomOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (textRect == null)
            return;

        // 선택된 글씨 크기 변화
        textRect.localScale = Vector3.Lerp(
            textRect.localScale,
            targetScale,
            1f - Mathf.Exp(
                -scaleLerpSpeed * Time.unscaledDeltaTime
            )
        );

        // 글씨만 살짝 둥둥 움직임
        Vector2 textPosition = baseTextPosition;

        if (isSelected)
        {
            float yOffset = Mathf.Sin(
                Time.unscaledTime * floatSpeed + randomOffset
            ) * floatAmplitude;

            textPosition.y += yOffset;
        }

        textRect.anchoredPosition = textPosition;

        // 마커 위치는 여기서 절대 건드리지 않음
    }

    public void Setup(
        ConfirmMenuController menuController,
        int itemIndex
    )
    {
        controller = menuController;
        index = itemIndex;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        // 마커는 켜고 끄기만 함
        if (selectionMarker != null)
        {
            selectionMarker.gameObject.SetActive(selected);
        }

        if (menuText == null)
            return;

        menuText.color = selected
            ? selectedColor
            : unselectedColor;

        targetScale = Vector3.one *
            (selected ? selectedScale : normalScale);

        if (!selected && textRect != null)
        {
            textRect.anchoredPosition = baseTextPosition;
        }
    }

    public RectTransform GetRectTransform()
    {
        return GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null)
        {
            controller.SelectItem(index);
        }
    }

    public void Press()
    {
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
    }
}