using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;      // 최대 체력
    public int currentHealth;        // 현재 체력

    void Start()
    {
        currentHealth = maxHealth;
    }

    // 체력을 감소시키고 데미지를 기록하며, 체력이 0 이하이면 Die()를 호출합니다.
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("플레이어 체력: " + currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // 플레이어 사망 처리 함수
    private void Die()
    {
        Debug.Log("플레이어 사망");
        // 추가적인 사망 처리 로직을 넣을 수 있습니다.
        // 예를 들어, 플레이어 오브젝트를 비활성화하거나, 게임 오버 화면을 표시하는 등.
        gameObject.SetActive(false);
    }
}
