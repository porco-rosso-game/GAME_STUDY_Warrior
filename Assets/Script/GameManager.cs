using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI / Gameplay 참조")]
    public CanvasGroup startMenuCanvasGroup;    // StartMenuUI CanvasGroup
    public CanvasGroup startButtonCanvasGroup;  // StartButton CanvasGroup
    public Button    startButton;               // 클릭/엔터용 Start 버튼
    public GameObject gameplayRoot;             // 플레이 루트 오브젝트

    [Header("Fade / Blink 설정")]
    [Tooltip("메뉴 페이드인/아웃 시간 (초)")]
    public float fadeDuration = 1f;
    [Tooltip("버튼 깜빡임 속도 배수 (1 = 2초 주기, 2 = 1초 주기)")]
    public float blinkSpeed = 1f;

    bool hasStarted = false;
    Coroutine blinkRoutine;

    void Awake()
    {
        // 초기 세팅: 게임플레이 비활성, 메뉴는 투명 상태
        gameplayRoot.SetActive(false);

        startMenuCanvasGroup.gameObject.SetActive(true);
        startMenuCanvasGroup.alpha = 0f;

        startButtonCanvasGroup.gameObject.SetActive(true);
        startButtonCanvasGroup.alpha = 0f;

        // 버튼 클릭 이벤트 연결
        startButton.onClick.AddListener(OnStartButton);
    }

    IEnumerator Start()
    {
        // 1) 메뉴 페이드 인
        yield return Fade(startMenuCanvasGroup, 0f, 1f);

        // 2) 버튼 페이드 인
        yield return Fade(startButtonCanvasGroup, 0f, 1f);

        // 3) 버튼 깜빡임 시작
        blinkRoutine = StartCoroutine(BlinkButton());

        // 4) Enter 키 입력 감지
        while (!hasStarted)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnStartButton();
            yield return null;
        }
    }

    void OnStartButton()
    {
        if (hasStarted) return;
        hasStarted = true;

        // 깜빡임 중지
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        // 메뉴 페이드 아웃 후 게임 시작
        StartCoroutine(FadeOutAndStartGame());
    }

    IEnumerator FadeOutAndStartGame()
    {
        // 1) 메뉴 전체 페이드 아웃
        yield return Fade(startMenuCanvasGroup, 1f, 0f);

        // 2) UI 비활성화 및 게임플레이 활성화
        startMenuCanvasGroup.gameObject.SetActive(false);
        gameplayRoot.SetActive(true);
        Time.timeScale = 1f;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator BlinkButton()
    {
        while (true)
        {
            // PingPong을 이용해 0⇄1을 반복
            float a = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            startButtonCanvasGroup.alpha = a;
            yield return null;
        }
    }
}
