using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeManager : MonoBehaviour
{
    [Header("페이드 설정")]
    public Image fadeImage;
    public float fadeDuration = 1.0f;

    void Start()
    {
        // 게임 시작 시(씬 로드 시) 바로 페이드 인 시작!
        if (fadeImage != null)
        {
            StartCoroutine(FadeInRoutine());
        }
        else
        {
            Debug.Log(" FadeIn 에러: 연결된 Fade Image가 없습니다!");
        }
    }

    private IEnumerator FadeInRoutine()
    {
        Debug.Log(" 씬 시작: Fade In 연출을 시작합니다.");

        // 1. 처음엔 화면을 완전히 검은색(알파 1)으로 설정
        Color imageColor = fadeImage.color;
        imageColor.a = 1f;
        fadeImage.color = imageColor;
        fadeImage.gameObject.SetActive(true);

        float elapsedTime = 0f;

        // 2. 설정한 시간 동안 알파값을 1에서 0으로 서서히 낮춤
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            imageColor.a = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            fadeImage.color = imageColor;
            yield return null; // 다음 프레임까지 대기
        }

        // 3. 완전히 투명해지면 이미지 비활성화 (클릭 방해 방지)
        imageColor.a = 0f;
        fadeImage.color = imageColor;
        fadeImage.gameObject.SetActive(false);

        Debug.Log(" Fade In 완료! 이제 전투를 시작할 수 있습니다.");
    }
}