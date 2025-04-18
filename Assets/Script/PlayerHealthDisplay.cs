using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    // PlayerController 참조
    public PlayerController playerController;  
    // 체력 표시용 슬라이더
    public Slider healthSlider;

    void Start()
    {
        if (playerController != null)
        {
            healthSlider.maxValue = playerController.maxHealth;
            healthSlider.value = playerController.currentHealth;
        }
    }

    void Update()
    {
        if (playerController == null)
            return;

        // 플레이어가 죽으면 체력 UI 숨기고 이 스크립트 비활성화
        if (playerController.isDead)
        {
            healthSlider.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        // 아직 살아있으면 체력값 계속 업데이트
        healthSlider.value = playerController.currentHealth;
    }
}
