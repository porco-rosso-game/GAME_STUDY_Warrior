using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;              // 이동 속도
    public float jumpForce = 7f;              // 점프 힘
    public float crouchForwardSpeed = 1f;     // 크로우시 전진 속도
    public float airControlMultiplier = 0.5f; // 공중 이동 계수

    [Header("Attack Settings")]
    public float attackRange = 1f;            // 공격 범위
    public int attackDamage = 10;             // 일반 공격 데미지
    public Transform attackPoint;             // 공격 발생 위치
    public LayerMask enemyLayers;             // 공격 대상 enemy 레이어

    [Header("Health Settings")]
    public int maxHealth = 100;               // 최대 체력
    public int currentHealth;                 // 현재 체력

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f;         // Hurt 애니메이션 지속 시간
    public float hurtKnockbackForce = 5f;     // Hurt 시 knockback 힘
    private bool isHurt = false;
    private float hurtTimer = 0f;

    // 플레이어 사망 여부 (다른 스크립트에서 접근할 수 있도록 public)
    public bool isDead = false;

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

    // Dash 관련 변수
    public bool isDashing = false;
    private float dashStartTime = 0f;
    public float dashDuration = 0.2f;         // Dash 상태 지속 시간
    private int dashDirection = 0;            // -1: 왼쪽, 1: 오른쪽
    public float doubleTapThreshold = 0.3f;   // 더블탭 간격 최대 시간
    private float lastLeftKeyTapTime = -1f;
    private float lastRightKeyTapTime = -1f;

    [Header("Dash Attack Settings")]
    public float dashAttackPushForce = 10f;   // Dash-Attack 시 앞으로 밀리는 힘
    public int dashAttackDamage = 20;         // Dash-Attack 공격 데미지

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;

        if (rb == null) Debug.LogWarning("No Rigidbody2D found!");
        if (animator == null) Debug.LogWarning("No Animator found!");
        if (spriteRenderer == null) Debug.LogWarning("No SpriteRenderer found!");

        currentHealth = maxHealth;
    }

    void ResetAllTriggers()
    {
        animator.ResetTrigger("JumpTrigger");
        animator.ResetTrigger("FallTrigger");
        animator.ResetTrigger("CroushTrigger");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("DeathTrigger");
        animator.ResetTrigger("HurtTrigger");
        animator.ResetTrigger("Dash");
        animator.ResetTrigger("DashAttack");
    }

    void StartDash(int direction)
    {
        isDashing = true;
        dashStartTime = Time.time;
        dashDirection = direction;
        ResetAllTriggers();
        animator.SetTrigger("Dash");
        Debug.Log("Dash triggered in direction: " + direction);
    }

    void Update()
    {
        if (isDead) return;

        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtDuration)
                isHurt = false;
            return;
        }

        // 더블탭 Dash 검출
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Time.time - lastRightKeyTapTime <= doubleTapThreshold && !isDashing)
                StartDash(1);
            lastRightKeyTapTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Time.time - lastLeftKeyTapTime <= doubleTapThreshold && !isDashing)
                StartDash(-1);
            lastLeftKeyTapTime = Time.time;
        }

        // Dash-Attack 입력 (Dash 상태 우선)
        if (isDashing && Input.GetKeyDown(KeyCode.Q))
        {
            ResetAllTriggers();
            animator.SetTrigger("DashAttack");
            isDashing = false;
            Debug.Log("Dash attack triggered.");
            return;
        }

        // 현재 애니메이터 상태 확인
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool isAttackingState = state.IsName("Attack") || state.IsName("DashAttack");

        // 지면 체크 및 애니메이터 파라미터 업데이트
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("verticalVelocity", rb.velocity.y);

        // 일반 공격 입력 처리 (가장 우선)
        if (!isAttackingState && !isDashing && Input.GetKeyDown(KeyCode.Q))
        {
            ResetAllTriggers();
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Attack");
            Debug.Log("Attack triggered.");
            return;
        }

        // 점프 입력 처리
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

        // Fall 처리 (공중에서 하강 중일 때, 공격/대시 중이 아니면)
        if (!isGrounded 
            && rb.velocity.y < 0f 
            && !state.IsName("Fall") 
            && !isAttackingState 
            && !isDashing)
        {
            ResetAllTriggers();
            animator.SetTrigger("FallTrigger");
            isJumping = false;
            Debug.Log("Fall triggered.");
        }

        // 착지 감지 -> Croush
        if (!wasGrounded && isGrounded)
        {
            ResetAllTriggers();
            animator.SetTrigger("CroushTrigger");
            isJumping = false;
        }
        wasGrounded = isGrounded;

        // 이동 방향과 스프라이트 플립 (공격/크로우시/대시가 아닐 때)
        bool isCroush = state.IsName("Croush");
        if (!isAttackingState && !isCroush && !isDashing)
        {
            float moveX = Input.GetAxis("Horizontal");
            moveDirection = new Vector2(moveX, Input.GetAxis("Vertical")).normalized;
            if (moveX < 0)
                transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
            else if (moveX > 0)
                transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
        }

        // 달리기 애니메이션 처리
        if (!isAttackingState && !isCroush && isGrounded && !isDashing)
        {
            bool isRunningInput = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ||
                                   Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            animator.SetBool("IsRunning", isRunningInput);
        }
        else if (!isDashing)
        {
            animator.SetBool("IsRunning", false);
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (isDashing)
        {
            rb.velocity = new Vector2(dashDirection * moveSpeed * 2, rb.velocity.y);
            if (Time.time - dashStartTime >= dashDuration)
                isDashing = false;
            return;
        }

        if (isGrounded)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool isAttackingState = state.IsName("Attack");

            if (isAttackingState)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
            else if (state.IsName("Croush"))
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                rb.velocity = new Vector2(inputX * crouchForwardSpeed, rb.velocity.y);
            }
            else
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                rb.velocity = Mathf.Approximately(inputX, 0f)
                    ? new Vector2(0, rb.velocity.y)
                    : new Vector2(inputX * moveSpeed, rb.velocity.y);
            }
        }
        else
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool isAttackingState = state.IsName("Attack");

            if (!isAttackingState)
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                rb.velocity = Mathf.Approximately(inputX, 0f)
                    ? new Vector2(Mathf.Lerp(rb.velocity.x, 0, 0.3f), rb.velocity.y)
                    : new Vector2(moveDirection.x * moveSpeed * airControlMultiplier, rb.velocity.y);
            }
        }
    }

    // 이하 TakeDamage, Hurt, Die, PerformAttack, PerformDashAttack 메서드는 기존과 동일합니다.
    public void TakeDamage(int damage)
    {
        if (isDead || isDashing) return;

        currentHealth -= damage;
        if (currentHealth > 0)
        {
            Hurt(new Vector2(1, 0));
        }
        else
        {
            Die();
        }
    }

    public void Hurt(Vector2 knockbackDirection)
    {
        if (!isHurt)
        {
            ResetAllTriggers();
            animator.SetTrigger("HurtTrigger");
            isHurt = true;
            hurtTimer = 0f;
            rb.AddForce(knockbackDirection * hurtKnockbackForce, ForceMode2D.Impulse);
        }
    }

    void LateUpdate()
    {
        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtDuration)
                isHurt = false;
        }
    }

    void Die()
    {
        ResetAllTriggers();
        animator.SetTrigger("DeathTrigger");
        isDead = true;
        rb.velocity = Vector2.zero;
        this.enabled = false;
    }

    public void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            var ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
                ec.TakeDamage(attackDamage, (enemy.transform.position - attackPoint.position).normalized);
        }
    }

    public void PerformDashAttack()
    {
        int dir = transform.localScale.x > 0 ? 1 : -1;
        rb.velocity = new Vector2(dashAttackPushForce * dir, rb.velocity.y);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            var ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
                ec.TakeDamage(dashAttackDamage, (enemy.transform.position - attackPoint.position).normalized);
        }
    }
}
