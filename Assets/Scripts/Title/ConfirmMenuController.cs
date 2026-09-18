using UnityEngine;

public class ConfirmMenuController : MonoBehaviour
{
    [Header("Menu Items")]
    [SerializeField] private ConfirmMenuItem[] menuItems;

    [Header("Start Selection")]
    [SerializeField] private int startIndex = 0;

    [Header("Actions")]
    [SerializeField] private TitleMenuActions titleMenuActions;

    private int currentIndex;

    private void OnEnable()
    {
        InitializeMenu();
    }

    private void Update()
    {
        HandleInput();
    }

    private void InitializeMenu()
    {
        if (menuItems == null || menuItems.Length == 0)
            return;

        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] != null)
            {
                menuItems[i].Setup(this, i);
            }
        }

        SelectItem(startIndex);
    }

    private void HandleInput()
    {
        // 왼쪽 이동
        if (Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.A))
        {
            MoveSelection(-1);
        }

        // 오른쪽 이동
        if (Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.D))
        {
            MoveSelection(1);
        }

        // 선택
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            if (currentIndex >= 0 &&
                currentIndex < menuItems.Length &&
                menuItems[currentIndex] != null)
            {
                menuItems[currentIndex].Press();
            }
        }

        // 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (titleMenuActions != null)
            {
                titleMenuActions.CloseExitConfirm();
            }
        }
    }

    public void SelectItem(int index)
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
    }

    private void MoveSelection(int direction)
    {
        if (menuItems == null || menuItems.Length == 0)
            return;

        int nextIndex = currentIndex + direction;

        if (nextIndex < 0)
            nextIndex = menuItems.Length - 1;

        if (nextIndex >= menuItems.Length)
            nextIndex = 0;

        SelectItem(nextIndex);
    }
}