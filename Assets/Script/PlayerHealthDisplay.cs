using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;   // 플레이어 체력 정보를 가진 스크립트 (Player 오브젝트에 붙은 PlayerHealth)
    public Slider healthSlider;         // 플레이어 체력을 표시할 슬라이더

    void Start()
    {
        // 슬라이더의 최대값을 플레이어의 최대 체력으로 설정
        if(playerHealth != null)
        {
            healthSlider.maxValue = playerHealth.maxHealth;
            healthSlider.value = playerHealth.currentHealth;
        }
    }

    void Update()
    {
        // 매 프레임마다 슬라이더의 값 업데이트
        if(playerHealth != null)
        {
            healthSlider.value = playerHealth.currentHealth;
        }
    }
}
