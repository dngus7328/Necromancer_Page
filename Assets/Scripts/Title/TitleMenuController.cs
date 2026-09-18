using UnityEngine;

public class TitleMenuController : MonoBehaviour
{
    [Header("Menu Items")]
    [SerializeField] private TitleMenuItem[] menuItems;

    [Header("Selection Visual")]
    [SerializeField] private RectTransform selectionVisualRoot;

    [Header("Selection Visual Move")]
    [SerializeField] private float visualMoveSpeed = 18f;

    [Header("Start Selection")]
    [SerializeField] private int startIndex = 0;

    private int currentIndex;
    private float fixedVisualX;
    private float targetVisualY;

    private void Start()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] != null)
            {
                menuItems[i].Setup(this, i);
            }
        }

        if (selectionVisualRoot != null)
        {
            fixedVisualX = selectionVisualRoot.anchoredPosition.x;
        }

        SelectItem(startIndex, true);
    }

    private void Update()
    {
        HandleKeyboardInput();
        UpdateSelectionVisual();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            MoveSelection(-1);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            MoveSelection(1);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (currentIndex >= 0 &&
                currentIndex < menuItems.Length &&
                menuItems[currentIndex] != null)
            {
                menuItems[currentIndex].Press();
            }
        }
    }

    private void UpdateSelectionVisual()
    {
        if (selectionVisualRoot == null)
            return;

        Vector2 currentPos = selectionVisualRoot.anchoredPosition;
        Vector2 targetPos = new Vector2(fixedVisualX, targetVisualY);

        selectionVisualRoot.anchoredPosition = Vector2.Lerp(
            currentPos,
            targetPos,
            1f - Mathf.Exp(-visualMoveSpeed * Time.unscaledDeltaTime)
        );
    }

    public void SelectItem(int index)
    {
        SelectItem(index, false);
    }

    private void SelectItem(int index, bool instantMove)
    {
        if (menuItems == null || menuItems.Length == 0)
            return;

        if (index < 0 || index >= menuItems.Length)
            return;

        currentIndex = index;

        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] != null)
            {
                menuItems[i].SetSelected(i == currentIndex);
            }
        }

        UpdateTargetVisualPosition(instantMove);
    }

    private void UpdateTargetVisualPosition(bool instantMove)
    {
        if (selectionVisualRoot == null)
            return;

        RectTransform selectedRect = menuItems[currentIndex].GetRectTransform();
        targetVisualY = selectedRect.anchoredPosition.y;

        if (instantMove)
        {
            selectionVisualRoot.anchoredPosition = new Vector2(fixedVisualX, targetVisualY);
        }
    }

    private void MoveSelection(int direction)
    {
        if (menuItems == null || menuItems.Length == 0)
            return;

        int nextIndex = currentIndex;

        do
        {
            nextIndex += direction;

            if (nextIndex < 0)
                nextIndex = menuItems.Length - 1;

            if (nextIndex >= menuItems.Length)
                nextIndex = 0;
        }
        while (menuItems[nextIndex] == null);

        SelectItem(nextIndex);
    }
}