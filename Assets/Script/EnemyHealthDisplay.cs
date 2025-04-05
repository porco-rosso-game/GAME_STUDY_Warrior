using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    public EnemyController enemyController;  // 이 UI가 참조할 적의 컨트롤러
    public Slider healthSlider;              // 체력바 슬라이더

    void Update()
    {
        if (enemyController != null)
        {
            // 체력을 최대 체력에 대한 비율로 표시 (0~1 사이)
            healthSlider.value = (float)enemyController.currentHealth / enemyController.maxHealth;
        }
    }
}
