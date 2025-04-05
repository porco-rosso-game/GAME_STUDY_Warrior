using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;      // 최대 체력
    public int currentHealth;        // 현재 체력

    void Start()
    {
        // 게임 시작 시 최대 체력으로 설정
        currentHealth = maxHealth;
    }

    // 체력을 감소시키는 함수 예시
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("플레이어 체력: " + currentHealth);

        if (currentHealth <= 0)
        {
            // 플레이어가 죽었을 때 처리
            Debug.Log("플레이어 사망");
        }
    }
}
