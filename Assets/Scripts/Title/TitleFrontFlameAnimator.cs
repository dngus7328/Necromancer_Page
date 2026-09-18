using UnityEngine;
using UnityEngine.UI;

public class TitleFrontFlameAnimator : MonoBehaviour
{
    [Header("불꽃 이미지를 넣어줘")]
    [SerializeField] private Image targetImage;

    [Header("불꽃 프레임들 순서대로 넣기")]
    [SerializeField] private Sprite[] frames;

    [Header("초당 프레임 수")]
    [SerializeField] private float framesPerSecond = 8f;

    private int currentFrame = 0;
    private float timer = 0f;
    private float frameTime = 0f;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        frameTime = 1f / framesPerSecond;

        if (frames != null && frames.Length > 0 && targetImage != null)
        {
            currentFrame = 0;
            targetImage.sprite = frames[currentFrame];
        }
    }

    private void Update()
    {
        if (targetImage == null)
            return;

        if (frames == null || frames.Length == 0)
            return;

        timer += Time.unscaledDeltaTime;

        if (timer >= frameTime)
        {
            timer -= frameTime;

            currentFrame++;

            if (currentFrame >= frames.Length)
                currentFrame = 0;

            targetImage.sprite = frames[currentFrame];
        }
    }
}