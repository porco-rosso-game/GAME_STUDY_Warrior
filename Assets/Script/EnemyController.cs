using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState { Idle, Walk, Attack, Cast }
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

    [Header("AI Settings")]
    public Transform player;           // 플레이어의 Transform (Inspector에 연결)
    public float attackCooldown = 1.5f;
    public float castCooldown = 3f;  

    private float attackTimer = 0f;
    private float castTimer = 0f;
    
    [Header("Movement Settings")]
    public float walkSpeed = 2f;       // Walk 상태일 때 이동 속도

    [Header("Attack Range Settings")]
    public float attackMinDistance = 0f;    // 공격 범위 최소값
    public float attackMaxDistance = 2f;    // 공격 범위 최대값

    [Header("Cast Range Settings")]
    public float castMinDistance = 4f;      // 주문 시전 범위 최소값
    public float castMaxDistance = 10f;     // 주문 시전 범위 최대값

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f;       // Hurt 애니메이션 지속 시간
    private bool isHurt = false;
    private float hurtTimer = 0f;

    [Header("Animation and Spell Settings")]
    public Animator animator;               // Enemy Animator (애니메이션 실행)
    public SpellController spellController; // SpellController (Inspector에 연결)

    // Enemy 좌우 회전 기준 (플립 Pivot; 할당되지 않으면 transform 사용)
    public Transform flipPivot;

    private Rigidbody2D rb;
    private bool isDead = false;

    // Spell 진행 여부 플래그
    private bool isSpellInProgress = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(); // Animator는 자식에 있을 수 있음

        if (flipPivot == null)
            Debug.LogWarning("FlipPivot is not assigned. Please assign the parent pivot object.");
    }

    void Update()
    {
        if (isDead)
            return;
        if (flipPivot == null || player == null)
            return;
        
        // ★ 플레이어가 죽었는지 확인 (플레이어의 PlayerController의 isDead가 public이어야 함)
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null && pc.isDead)
        {
            rb.velocity = Vector2.zero;
            animator.Play("Idle");
            currentState = EnemyState.Idle;
            attackTimer = 0f;
            castTimer = 0f;
            return;
        }
        
        // ── 부모(flipPivot)의 localScale 업데이트 ──
        Vector3 pivotScale = flipPivot.localScale;
        if (player.position.x < flipPivot.position.x)
            pivotScale.x = Mathf.Abs(pivotScale.x);
        else
            pivotScale.x = -Mathf.Abs(pivotScale.x);
        flipPivot.localScale = pivotScale;
        // ─────────────────────────────────────────────

        // ── 추가: enemy(자기 자신의 transform)도 항상 플레이어를 바라보도록 업데이트 ──
        Vector3 selfScale = transform.localScale;
        if (player.position.x < transform.position.x)
            selfScale.x = Mathf.Abs(selfScale.x);
        else
            selfScale.x = -Mathf.Abs(selfScale.x);
        transform.localScale = selfScale;
        // ─────────────────────────────────────────────

        // ▶️ Hurt 처리
        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtDuration)
                isHurt = false;
            return;
        }

        // ▶️ 플레이어와의 거리 계산 (flipPivot 기준)
        float distance = Vector2.Distance(flipPivot.position, player.position);

        // ▶️ 상태 결정:
        if (distance >= attackMinDistance && distance <= attackMaxDistance)
            currentState = EnemyState.Attack;
        else if (distance >= castMinDistance && distance <= castMaxDistance)
            currentState = EnemyState.Cast;
        else
            currentState = EnemyState.Walk;

        // ▶️ Animator의 Walk 애니메이션 제어
        animator.SetBool("IsWalking", currentState == EnemyState.Walk);

        // ▶️ 상태별 행동 처리
        switch (currentState)
        {
            case EnemyState.Walk:
                {
                    int direction = player.position.x > flipPivot.position.x ? 1 : -1;
                    rb.velocity = new Vector2(walkSpeed * direction, rb.velocity.y);
                    attackTimer = 0f;
                    castTimer = 0f;
                    break;
                }
            case EnemyState.Attack:
                {
                    rb.velocity = Vector2.zero;
                    if (isSpellInProgress && spellController != null)
                    {
                        spellController.CancelSpell();
                        isSpellInProgress = false;
                        castTimer = 0f;
                    }
                    attackTimer += Time.deltaTime;
                    if (attackTimer >= attackCooldown)
                    {
                        PerformAttack();
                        attackTimer = 0f;
                    }
                    break;
                }
            case EnemyState.Cast:
                {
                    rb.velocity = Vector2.zero;
                    attackTimer = 0f;
                    if (!isSpellInProgress)
                    {
                        castTimer += Time.deltaTime;
                        if (castTimer >= castCooldown)
                        {
                            CastSpell();
                            castTimer = 0f;
                        }
                    }
                    break;
                }
            default:
                {
                    rb.velocity = Vector2.zero;
                    animator.SetBool("IsWalking", false);
                    break;
                }
        }
    }

    public void TakeDamage(int damage, Vector2 attackDirection)
    {
        if (isDead)
            return;
        currentHealth -= damage;
        Debug.Log("Enemy health: " + currentHealth);
        if (rb != null)
            rb.AddForce(attackDirection * knockbackForce, ForceMode2D.Impulse);
        if (currentHealth > 0)
            Hurt();
        else
            Die();
    }

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
        // Attack 데미지는 Attack 애니메이션의 Animation Event를 통해 DealAttackDamage()에서 적용
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

    // Spell 애니메이션 마지막에 Animation Event로 호출되어 Spell 플래그 해제
    public void OnSpellAnimationComplete()
    {
        isSpellInProgress = false;
    }

    // Attack 애니메이션 중 Animation Event로 호출되어 데미지 적용
    public void DealAttackDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayers);
        if (hitPlayer != null)
        {
            PlayerController pc = hitPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                Vector2 knockbackDirection = (hitPlayer.transform.position - flipPivot.position).normalized;
                pc.TakeDamage(attackDamage);
                Debug.Log("Attack hit! Damage applied: " + attackDamage);
            }
        }
    }
}
