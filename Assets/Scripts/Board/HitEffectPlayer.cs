using UnityEngine;

public class HitEffectPlayer : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameRate = 36f;
    private float timer;
    private int frameIndex;

    public void Play(Sprite[] effectFrames, float fps, float scale, int sortingOrder)
    {
        frames = effectFrames;
        frameRate = Mathf.Max(1f, fps);
        frameIndex = 0;
        timer = 0f;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = sortingOrder;

        transform.localScale = Vector3.one * scale;

        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        int nextFrame = Mathf.FloorToInt(timer * frameRate);

        if (nextFrame >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }

        if (nextFrame != frameIndex)
        {
            frameIndex = nextFrame;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}
