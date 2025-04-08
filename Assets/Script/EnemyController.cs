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
    public Transform player;    // 플레이어의 Transform (Inspector에 연결)
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
    public SpellController spellController;     // SpellController (Inspector에 연결)

    [Header("Movement & Flip Settings")]
    // 모든 움직임과 플립의 기준으로 사용할 오브젝트 (예를 들어, FlipPivot)
    public Transform flipPivot;

    private Rigidbody2D rb;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();

        // 만약 Inspector에서 flipPivot이 할당되지 않았다면 enemy의 부모(transform.parent)를 사용
        if (flipPivot == null && transform.parent != null)
        {
            flipPivot = transform.parent;
        }
    }

    void Update()
    {
        if (isDead)
            return;

        if (flipPivot == null)
            return; // 기준 오브젝트가 없으면 더 이상 진행하지 않음

        // ▶️ 플레이어와의 상대 위치에 따라 flipPivot의 localScale.x값을 변경하여 좌우 반전 처리
        if (player != null)
        {
            Vector3 pivotScale = flipPivot.localScale;
            if (player.position.x < flipPivot.position.x)
            {
                // 플레이어가 왼쪽에 있으면 flipPivot의 x값을 양수로 유지
                pivotScale.x = Mathf.Abs(pivotScale.x);
            }
            else
            {
                // 플레이어가 오른쪽에 있으면 flipPivot의 x값을 음수로 반전
                pivotScale.x = -Mathf.Abs(pivotScale.x);
            }
            flipPivot.localScale = pivotScale;
        }

        // ▶️ Hurt 상태 우선 처리 (Hurt 애니메이션 진행 중이면 다른 동작 건너뜀)
        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtDuration)
            {
                isHurt = false;
            }
            return;
        }

        // ▶️ 모든 이동, 공격 관련 계산은 flipPivot의 위치를 기준으로 함
        float distance = Vector2.Distance(flipPivot.position, player.position);

        if (distance <= attackDistanceThreshold)
            currentState = EnemyState.Attack;
        else if (distance >= castDistanceThreshold)
            currentState = EnemyState.Cast;
        else
            currentState = EnemyState.Attack;

        if (currentState == EnemyState.Attack && isSpellInProgress)
        {
            if (spellController != null)
                spellController.CancelSpell();
            isSpellInProgress = false;
            castTimer = 0f;
        }

        if (currentState == EnemyState.Attack && !isSpellInProgress)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                PerformAttack();
                attackTimer = 0f;
            }
        }
        else if (currentState != EnemyState.Attack)
        {
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

    // ▶️ 플레이어가 enemy를 공격하여 데미지를 줄 때 호출됨
    public void TakeDamage(int damage, Vector2 attackDirection)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log("Enemy health: " + currentHealth);

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

    // ▶️ Hurt 상태 전환: Hurt 애니메이션 실행 후 잠시 다른 동작 차단
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
        // Attack 데미지 처리는 Animation Event를 통해 DealAttackDamage()에서 실행됩니다.
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

    // ▶️ Spell 애니메이션 종료 후 호출되어 진행 중 플래그를 해제
    public void OnSpellAnimationComplete()
    {
        isSpellInProgress = false;
    }

    // ▶️ Attack 애니메이션 타격 시점에 Animation Event로 호출되어 플레이어에게 데미지 적용
    public void DealAttackDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayers);
        if (hitPlayer != null)
        {
            PlayerController pc = hitPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                // knockback의 기준으로 flipPivot의 위치를 사용
                Vector2 knockbackDirection = (hitPlayer.transform.position - flipPivot.position).normalized;
                pc.TakeDamage(attackDamage);
                Debug.Log("Attack hit! Damage applied: " + attackDamage);
            }
        }
    }
}
