using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlobalButtonPressFeedback : MonoBehaviour
{
    [Header("Press Effect")]
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float pressSpeed = 25f;
    [SerializeField] private float releaseSpeed = 18f;

    private Button pressedButton;
    private RectTransform pressedRect;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private readonly List<RaycastResult> raycastResults =
        new List<RaycastResult>();

    private void Update()
    {
        HandleMouseInput();
        UpdateAnimation();
    }

    private void HandleMouseInput()
    {
        // 마우스 왼쪽 버튼을 누른 순간
        if (Input.GetMouseButtonDown(0))
        {
            TryPressButton();
        }

        // 마우스를 놓은 순간
        if (Input.GetMouseButtonUp(0))
        {
            ReleaseButton();
        }
    }

    private void TryPressButton()
    {
        if (EventSystem.current == null)
            return;

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

        pointerData.position = Input.mousePosition;

        raycastResults.Clear();

        EventSystem.current.RaycastAll(
            pointerData,
            raycastResults
        );

        foreach (RaycastResult result in raycastResults)
        {
            Button button =
                result.gameObject.GetComponentInParent<Button>();

            if (button == null)
                continue;

            if (!button.interactable)
                continue;

            StartPress(button);
            return;
        }
    }

    private void StartPress(Button button)
    {
        // 이전 버튼이 남아 있다면 복구
        ForceRestore();

        pressedButton = button;
        pressedRect =
            button.GetComponent<RectTransform>();

        if (pressedRect == null)
            return;

        originalScale = pressedRect.localScale;

        targetScale =
            originalScale * pressedScale;
    }

    private void ReleaseButton()
    {
        if (pressedRect == null)
            return;

        targetScale = originalScale;

        pressedButton = null;
    }

    private void UpdateAnimation()
    {
        if (pressedRect == null)
            return;

        float speed =
            pressedButton != null
                ? pressSpeed
                : releaseSpeed;

        pressedRect.localScale =
            Vector3.Lerp(
                pressedRect.localScale,
                targetScale,
                1f - Mathf.Exp(
                    -speed *
                    Time.unscaledDeltaTime
                )
            );

        // 원래 크기로 거의 돌아왔으면 종료
        if (pressedButton == null)
        {
            if (Vector3.Distance(
                    pressedRect.localScale,
                    originalScale
                ) < 0.001f)
            {
                pressedRect.localScale =
                    originalScale;

                pressedRect = null;
            }
        }
    }

    private void ForceRestore()
    {
        if (pressedRect != null)
        {
            pressedRect.localScale =
                originalScale;
        }

        pressedButton = null;
        pressedRect = null;
    }

    private void OnDisable()
    {
        ForceRestore();
    }
}