using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;            // 이동 속도
    public float jumpForce = 7f;            // 점프 힘
    public float crouchForwardSpeed = 1f;   // 크로우시 전진 속도
    public float airControlMultiplier = 0.5f; // 공중 이동 계수

    [Header("Attack Settings")]
    public float attackRange = 1f;          // 공격 범위
    public int attackDamage = 10;           // 공격 데미지
    public Transform attackPoint;           // 공격 발생 위치
    public LayerMask enemyLayers;           // 공격 대상 enemy 레이어

    [Header("Health Settings")]
    public int maxHealth = 100;             // 최대 체력
    public int currentHealth;               // 현재 체력

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f;       // Hurt 애니메이션 지속 시간
    public float hurtKnockbackForce = 5f;   // Hurt 시 knockback 힘
    private bool isHurt = false;
    private float hurtTimer = 0f;

    private bool isDead = false;            // 사망 여부

    [Header("Jump & Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool wasGrounded = false;
    private bool isJumping = false;
    private float jumpStartY = 0f;
    private float maxJumpY = 0f;

    // 내부 변수
    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector3 initialScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;

        if (rb == null)
            Debug.LogWarning("No Rigidbody2D found!");
        if (animator == null)
            Debug.LogWarning("No Animator found!");
        if (spriteRenderer == null)
            Debug.LogWarning("No SpriteRenderer found!");

        currentHealth = maxHealth;
    }

    // 모든 트리거 리셋 (불필요한 전이 방지용)
    void ResetAllTriggers()
    {
        animator.ResetTrigger("JumpTrigger");
        animator.ResetTrigger("FallTrigger");
        animator.ResetTrigger("CroushTrigger");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("DeathTrigger");
        animator.ResetTrigger("HurtTrigger");
    }

    void Update()
    {
        if (isDead)
            return;

        // Hurt 상태라면 hurtDuration 동안 다른 동작 무시
        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtDuration)
                isHurt = false;
            return;
        }

        // 지면 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("verticalVelocity", rb.velocity.y);

        // 점프 입력
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            ResetAllTriggers();
            jumpStartY = transform.position.y;
            maxJumpY = transform.position.y;
            isJumping = true;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            animator.SetTrigger("JumpTrigger");
            Debug.Log("Jump triggered.");
        }

        if (!isGrounded && isJumping)
        {
            maxJumpY = Mathf.Max(maxJumpY, transform.position.y);
        }

        // Fall 처리
        if (!isGrounded && rb.velocity.y < 0f && wasGrounded && !animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"))
        {
            if (!Input.GetKey(KeyCode.Q))
            {
                ResetAllTriggers();
                animator.SetTrigger("FallTrigger");
            }
        }
        else if (!isGrounded && isJumping && rb.velocity.y < 0f && !animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"))
        {
            float dropHeight = maxJumpY - transform.position.y;
            if (dropHeight >= 0.5f) // fallThreshold
            {
                if (!Input.GetKey(KeyCode.Q))
                {
                    ResetAllTriggers();
                    animator.SetTrigger("FallTrigger");
                    isJumping = false;
                }
            }
        }

        // 착지 감지
        if (!wasGrounded && isGrounded)
        {
            ResetAllTriggers();
            animator.SetTrigger("CroushTrigger");
            isJumping = false;
        }
        wasGrounded = isGrounded;

        // 이동 및 스프라이트 플립
        bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
        bool isCroush = animator.GetCurrentAnimatorStateInfo(0).IsName("Croush");
        if (!isAttacking && !isCroush)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            moveDirection = new Vector2(moveX, moveY).normalized;

            if (moveX < 0)
                transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
            else if (moveX > 0)
                transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
        }
        else
        {
            moveDirection = Vector2.zero;
        }

        // 공격 입력 (예시: Q 키)
        if (!isAttacking && Input.GetKeyDown(KeyCode.Q))
        {
            ResetAllTriggers();
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Attack");
        }

        // 달리기 애니메이션 처리
        if (!isAttacking && !isCroush && isGrounded)
        {
            bool isRunningInput = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ||
                                   Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            animator.SetBool("IsRunning", isRunningInput);
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        if (isGrounded)
        {
            bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
            bool isCroush = animator.GetCurrentAnimatorStateInfo(0).IsName("Croush");

            if (isAttacking)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
            else if (isCroush)
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                rb.velocity = new Vector2(inputX * crouchForwardSpeed, rb.velocity.y);
            }
            else
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                rb.velocity = Mathf.Approximately(inputX, 0f) ? 
                              new Vector2(0, rb.velocity.y) : new Vector2(inputX * moveSpeed, rb.velocity.y);
            }
        }
        else
        {
            bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
            if (!isAttacking)
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                rb.velocity = Mathf.Approximately(inputX, 0f) ? 
                              new Vector2(Mathf.Lerp(rb.velocity.x, 0, 0.3f), rb.velocity.y) :
                              new Vector2(moveDirection.x * moveSpeed * airControlMultiplier, rb.velocity.y);
            }
        }
    }

    // 플레이어가 데미지를 받을 때 호출되는 함수 (체력 관리 포함)
    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log("Player takes damage: " + damage + ", currentHealth = " + currentHealth);

        if (currentHealth > 0)
        {
            // 예시: 공격자 방향을 (1,0)으로 가정 (필요에 따라 수정)
            Vector2 knockbackDirection = new Vector2(1, 0);
            Hurt(knockbackDirection);
        }
        else
        {
            Die();
        }
    }

    // Hurt 함수: Hurt 애니메이션 트리거 실행, knockback 적용, hurtDuration 동안 다른 동작 무시
    public void Hurt(Vector2 knockbackDirection)
    {
        if (!isHurt)
        {
            ResetAllTriggers();
            animator.SetTrigger("HurtTrigger");
            isHurt = true;
            hurtTimer = 0f;
            rb.AddForce(knockbackDirection * hurtKnockbackForce, ForceMode2D.Impulse);
            Debug.Log("Player is hurt!");
        }
    }

    void LateUpdate()
    {
        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtDuration)
            {
                isHurt = false;
            }
        }
    }

    void Die()
    {
        ResetAllTriggers();
        animator.SetTrigger("DeathTrigger");
        isDead = true;
        rb.velocity = Vector2.zero;
        Debug.Log("Player has died.");
    }
    
    // 플레이어 공격 애니메이션 이벤트로 호출될 함수
    public void PerformAttack()
    {
        Debug.Log("Player performs attack!");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Vector2 knockbackDirection = (enemy.transform.position - attackPoint.position).normalized;
            EnemyController ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.TakeDamage(attackDamage, knockbackDirection);
                Debug.Log("Player attack hit " + enemy.name + " for " + attackDamage + " damage.");
            }
        }
    }

    // 디버그용: 공격 범위 시각화
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
