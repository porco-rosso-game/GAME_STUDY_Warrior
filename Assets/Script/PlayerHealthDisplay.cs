using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    // 이전 PlayerHealth 대신 PlayerController를 참조합니다.
    public PlayerController playerController;  
    public Slider healthSlider;

    void Start()
    {
        if(playerController != null)
        {
            healthSlider.maxValue = playerController.maxHealth;
            healthSlider.value = playerController.currentHealth;
        }
    }

    void Update()
    {
        if(playerController != null)
        {
            healthSlider.value = playerController.currentHealth;
        }
    }
}
