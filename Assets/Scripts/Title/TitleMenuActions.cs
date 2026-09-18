using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleMenuActions : MonoBehaviour
{
    [Header("Exit Confirm Panel")]
    [SerializeField] private GameObject exitConfirmPanel;

    [Header("Exit Popup Animation")]
    [SerializeField] private UIPopupAnimator exitPopupAnimator;

    [Header("Confirm Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Main Menu")]
    [SerializeField] private TitleMenuController titleMenuController;

    [Header("Return Selection")]
    [SerializeField] private GameObject exitButton;

    private bool isExitPanelOpen = false;

    private void Start()
    {
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isExitPanelOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseExitConfirm();
        }
    }

    // 나가기 버튼
    public void OpenExitConfirm()
    {
        if (exitConfirmPanel == null)
            return;

        isExitPanelOpen = true;

        // 팝업 애니메이션으로 열기
        if (exitPopupAnimator != null)
        {
            exitPopupAnimator.Open();
        }
        else
        {
            // 애니메이터 연결이 안 되어 있어도 창은 열리게
            exitConfirmPanel.SetActive(true);
        }

        // 뒤쪽 타이틀 메뉴 입력 정지
        if (titleMenuController != null)
        {
            titleMenuController.enabled = false;
        }

        // 기본 선택은 "아니오"
        if (EventSystem.current != null && noButton != null)
        {
            EventSystem.current.SetSelectedGameObject(
                noButton.gameObject
            );
        }
    }

    // 아니오 / ESC
    public void CloseExitConfirm()
    {
        if (exitConfirmPanel == null)
            return;

        isExitPanelOpen = false;

        // 팝업 애니메이션으로 닫기
        if (exitPopupAnimator != null)
        {
            exitPopupAnimator.Close();
        }
        else
        {
            exitConfirmPanel.SetActive(false);
        }

        // 메인 메뉴 입력 다시 활성화
        if (titleMenuController != null)
        {
            titleMenuController.enabled = true;
        }

        // 나가기 버튼으로 선택 복귀
        if (EventSystem.current != null && exitButton != null)
        {
            EventSystem.current.SetSelectedGameObject(
                exitButton
            );
        }
    }

    // 예
    public void ConfirmExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}