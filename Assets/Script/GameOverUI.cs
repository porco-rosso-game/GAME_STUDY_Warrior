using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("CanvasGroup on the full-screen dark panel")]
    public CanvasGroup darkPanel;
    [Tooltip("Game Over text object (initially inactive)")]
    public GameObject gameOverText;

    [Header("Fade Settings")]
    [Tooltip("Target alpha for the dark overlay (0 = transparent, 1 = opaque)")]
    [Range(0f, 1f)]
    public float targetAlpha = 0.5f;
    [Tooltip("Duration of the fade in seconds")]
    public float fadeDuration = 1f;

    void Start()
    {
        // 초기 상태: 투명 패널 + 텍스트 비활성화
        if (darkPanel != null)
            darkPanel.alpha = 0f;
        if (gameOverText != null)
            gameOverText.SetActive(false);
    }

    /// <summary>
    /// 외부에서 호출하여 게임 오버 UI를 표시합니다.
    /// </summary>
    public void ShowGameOver()
    {
        if (darkPanel == null || gameOverText == null)
        {
            Debug.LogWarning("GameOverUI: Dark panel or GameOverText is not assigned.");
            return;
        }

        // 패널 페이드 인
        StartCoroutine(FadeInPanel());
    }

    private IEnumerator FadeInPanel()
    {
        float elapsed = 0f;
        float startAlpha = darkPanel.alpha;

        // 페이드 중 텍스트 비활성
        gameOverText.SetActive(false);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // 선형 보간으로 알파 변경
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            darkPanel.alpha = alpha;
            yield return null;
        }

        // 최종 알파 보정
        darkPanel.alpha = targetAlpha;

        // Game Over 텍스트 활성화
        gameOverText.SetActive(true);
    }
}
