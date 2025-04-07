using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState { Idle, Attack, Cast }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Health Settings")]
    public int maxHealth = 20;
    public int currentHealth;
    public float knockbackForce = 5f;

    [Header("Damage Settings (Attack)")]
    public Transform attackPoint;      // 공격 발생 위치 (Inspector에 할당)
    public float attackRange = 0.5f;     // 공격 범위 (원 형태)
    public LayerMask playerLayers;       // 플레이어가 포함된 레이어 (예: "Player")
    public int attackDamage = 10;        // 공격 데미지

    [Header("AI Attack Settings")]
    public Transform player;                      // 플레이어의 Transform (Inspector에 연결)
    public float attackDistanceThreshold = 2f;    // 근접 공격 영역: 이 값 이하이면 공격
    public float castDistanceThreshold = 4f;      // 주문 캐스트 영역: 이 값 이상이면 cast 실행
    public float attackCooldown = 1.5f;
    public float castCooldown = 3f;  

    private float attackTimer = 0f;
    private float castTimer = 0f;
    
    public bool isSpellInProgress = false; // Spell 애니메이션 진행 여부

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f; // Hurt 애니메이션 지속 시간
    private bool isHurt = false;
    private float hurtTimer = 0f;

    [Header("Animation and Spell Settings")]
    public Animator animator;                   // Enemy Animator (Idle, Attack, Cast, Hurt, Death 등)
    public SpellController spellController;     // 독립된 Spell 오브젝트의 SpellController (Inspector에 연결)

    private Rigidbody2D rb;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead)
            return;

        // Hurt 상태 우선 처리: Hurt 애니메이션 진행 중이면 다른 동작을 건너뜁니다.
        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtDuration)
            {
                isHurt = false;
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        // 상태 결정: 가까우면 Attack, 멀면 Cast, 애매한 영역은 기본적으로 Attack으로 처리
        if (distance <= attackDistanceThreshold)
            currentState = EnemyState.Attack;
        else if (distance >= castDistanceThreshold)
            currentState = EnemyState.Cast;
        else
            currentState = EnemyState.Attack;

        // 만약 Attack 상태인데 Spell 진행 중이면 cast 취소
        if (currentState == EnemyState.Attack && isSpellInProgress)
        {
            if (spellController != null)
                spellController.CancelSpell();
            isSpellInProgress = false;
            castTimer = 0f;
        }

        // 상태별 타이머 업데이트 및 동작 실행
        if (currentState == EnemyState.Attack && !isSpellInProgress)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                PerformAttack();
                attackTimer = 0f;
            }
        }
        else
        {
            if (currentState != EnemyState.Attack)
                attackTimer = 0f;
        }

        if (currentState == EnemyState.Cast && !isSpellInProgress)
        {
            castTimer += Time.deltaTime;
            if (castTimer >= castCooldown)
            {
                CastSpell();
                castTimer = 0f;
            }
        }
        else if (currentState != EnemyState.Cast)
        {
            castTimer = 0f;
        }
    }

    // 플레이어가 enemy를 공격하여 데미지를 줄 때 호출됩니다.
    public void TakeDamage(int damage, Vector2 attackDirection)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log("Enemy health: " + currentHealth);

        // Knockback 적용 (enemy에도 knockback 효과가 있다면)
        if (rb != null)
            rb.AddForce(attackDirection * knockbackForce, ForceMode2D.Impulse);

        if (currentHealth > 0)
        {
            Hurt();
        }
        else
        {
            Die();
        }
    }

    // Hurt 상태로 전환: Hurt 애니메이션 실행, hurtDuration 동안 다른 동작을 막습니다.
    void Hurt()
    {
        if (animator != null)
        {
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("CastTrigger");
            animator.SetTrigger("HurtTrigger");
        }
        isHurt = true;
        hurtTimer = 0f;
        Debug.Log("Enemy is hurt!");
    }

    void Die()
    {
        if (isDead)
            return;
        isDead = true;
        Debug.Log("Enemy died");
        if (animator != null)
        {
            animator.SetTrigger("DeathTrigger");
            StartCoroutine(DestroyAfterAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    void PerformAttack()
    {
        if (animator != null)
        {
            animator.ResetTrigger("CastTrigger");
            animator.SetTrigger("AttackTrigger");
        }
        Debug.Log("Enemy performs attack");
        // 데미지 적용은 아래 DealAttackDamage()에서 Animation Event를 통해 처리됩니다.
    }

    public void CastSpell()
    {
        if (!isSpellInProgress)
        {
            isSpellInProgress = true;
            if (animator != null)
            {
                animator.ResetTrigger("AttackTrigger");
                animator.SetTrigger("CastTrigger");
            }
            if (spellController != null)
                spellController.ActivateSpell();
            Debug.Log("Enemy casts spell");
        }
    }

    // SpellController에서 Spell 애니메이션 종료 시 호출하여 플래그를 리셋합니다.
    public void OnSpellAnimationComplete()
    {
        isSpellInProgress = false;
    }

    // 이 함수는 Attack 애니메이션 클립의 타격 시점에 Animation Event로 호출되어, 플레이어에게 데미지를 적용합니다.
    public void DealAttackDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayers);
        if (hitPlayer != null)
        {
            PlayerController pc = hitPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                Vector2 knockbackDirection = (hitPlayer.transform.position - transform.position).normalized;
                pc.TakeDamage(attackDamage);
                Debug.Log("Attack hit! Damage applied: " + attackDamage);
            }
        }
    }
}
