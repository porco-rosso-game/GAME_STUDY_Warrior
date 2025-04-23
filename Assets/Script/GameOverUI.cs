using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어가 죽었을 때 호출하면,
/// 설정된 Game Over 이미지가 서서히 페이드 인됩니다.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Game Over Image")]
    [Tooltip("게임오버 상태를 표시할 Image 컴포넌트")] 
    public Image gameOverImage;

    [Header("Fade Duration")]
    [Tooltip("이미지가 완전히 보이기까지 걸리는 시간(초)")]
    public float fadeDuration = 1f;

    private void Awake()
    {
        // 초기 상태: 이미지 숨기고 투명도 0
        if (gameOverImage != null)
        {
            Color c = gameOverImage.color;
            gameOverImage.color = new Color(c.r, c.g, c.b, 0f);
            gameOverImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 게임오버 시 호출하세요.
    /// </summary>
    public void ShowGameOver()
    {
        if (gameOverImage == null)
        {
            Debug.LogWarning("GameOverUI: gameOverImage가 할당되지 않았습니다.");
            return;
        }

        gameOverImage.gameObject.SetActive(true);
        StartCoroutine(FadeInImage());
    }

    private IEnumerator FadeInImage()
    {
        float elapsed = 0f;
        Color baseColor = gameOverImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            gameOverImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        // 보정: 완전 불투명하게 설정
        gameOverImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }
}
