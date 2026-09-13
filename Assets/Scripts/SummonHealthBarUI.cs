using UnityEngine;

public class SummonHealthBar : MonoBehaviour
{
    [Header("대상")]
    [SerializeField]
    private SummonUnitBase summon;

    [Header("체력바")]
    [SerializeField]
    private Transform fill;

    [Tooltip("체력이 가득 찼을 때 Fill의 실제 가로 폭")]
    [SerializeField]
    private float fullWidth = 0.8f;

    private Vector3 originalFillScale;
    private Vector3 originalFillPosition;

    private bool hiddenAfterDeath;

    private void Awake()
    {
        if (summon == null)
        {
            summon =
                GetComponentInParent<SummonUnitBase>();
        }

        if (fill != null)
        {
            originalFillScale =
                fill.localScale;

            originalFillPosition =
                fill.localPosition;
        }
    }

    private void OnEnable()
    {
        hiddenAfterDeath =
            false;
    }

    private void LateUpdate()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (hiddenAfterDeath)
        {
            return;
        }

        if (summon == null ||
            fill == null)
        {
            return;
        }

        // 사망한 순간 체력바 전체를 즉시 끈다.
        // Fill만 줄이는 방식으로는 자식 이미지/외곽선/잔여 픽셀이
        // 잠깐 남아 보일 수 있으므로 루트 자체를 비활성화한다.
        if (summon.IsDead ||
            summon.CurrentHealth <= 0f)
        {
            HideWholeHealthBar();

            return;
        }

        if (summon.MaxHealth <= 0f)
        {
            HideWholeHealthBar();

            return;
        }

        float ratio =
            Mathf.Clamp01(
                summon.CurrentHealth /
                summon.MaxHealth
            );

        if (!fill.gameObject.activeSelf)
        {
            fill.gameObject.SetActive(
                true
            );
        }

        Vector3 newScale =
            originalFillScale;

        newScale.x =
            originalFillScale.x *
            ratio;

        fill.localScale =
            newScale;

        // Fill Pivot이 가운데여도 왼쪽 끝이 고정되어 보이도록 위치 보정
        float lostWidth =
            fullWidth *
            (1f - ratio);

        Vector3 newPosition =
            originalFillPosition;

        newPosition.x =
            originalFillPosition.x -
            (lostWidth * 0.5f);

        fill.localPosition =
            newPosition;
    }

    private void HideWholeHealthBar()
    {
        hiddenAfterDeath =
            true;

        // 혹시 현재 프레임에서 렌더링될 Fill도 먼저 0으로 만든다.
        if (fill != null)
        {
            Vector3 scale =
                fill.localScale;

            scale.x =
                0f;

            fill.localScale =
                scale;

            fill.gameObject.SetActive(
                false
            );
        }

        // 이 스크립트가 붙어 있는 HealthBar 오브젝트 전체를 끈다.
        // Background / Fill / 자식 효과가 모두 즉시 사라진다.
        gameObject.SetActive(
            false
        );
    }
}
