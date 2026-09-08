using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ClickMoveEffectDestroy : MonoBehaviour
{
    private Animator animator;
    private bool started;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null)
        {
            Destroy(gameObject);
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // Animator가 실제 재생을 시작한 뒤부터 검사
        if (state.normalizedTime > 0f)
        {
            started = true;
        }

        // 애니메이션 1회 재생 완료
        if (started && state.normalizedTime >= 1f && !animator.IsInTransition(0))
        {
            Destroy(gameObject);
        }
    }
}