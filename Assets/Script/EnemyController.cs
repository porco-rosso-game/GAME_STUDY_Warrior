using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 20;      // 적의 최대 체력
    public int currentHealth;       // 현재 체력 (public으로 변경)
    public float knockbackForce = 5f;
    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage, Vector2 attackDirection)
    {
        currentHealth -= damage;
        Debug.Log("적 체력: " + currentHealth);

        if (rb != null)
        {
            rb.AddForce(attackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("적 죽음");
        Destroy(gameObject);
    }
}
