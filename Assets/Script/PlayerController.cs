using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;            // 이동 속도
    public float attackRange = 1f;          // 공격 범위
    public int attackDamage = 10;           // 공격 데미지
    public Transform attackPoint;           // 공격 위치 (플레이어 앞쪽에 배치)
    public LayerMask enemyLayers;           // 적이 포함된 레이어

    [Header("점프 관련 설정")]
    public float jumpForce = 7f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Croush 관련 설정")]
    public float crouchForwardSpeed = 1f;   // croush 상태에서 전진하는 속도

    [Header("Fall 조건 (캐릭터 기준)")]
    public float fallThreshold = 0.5f;      // 점프 후 최고점과 현재 높이 차이가 이 값 이상이면 Fall 전이

    [Header("Air Control")]
    public float airControlMultiplier = 0.5f; // 공중에서의 수평 이동 계수 (땅에서는 1)

    private bool isGrounded;
    private bool wasGrounded = false;       // 이전 프레임의 바닥 접촉 상태

    // 땅에 있을 때의 수평 입력 값 (공중에서는 마지막 입력 유지)
    private float lastHorizontalInput = 0f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // 초기 localScale (플립 시 사용)
    private Vector3 initialScale;

    // 점프 관련 변수
    private float jumpStartY = 0f; // 점프 시작 시의 높이 (월드 기준)
    private float maxJumpY = 0f;   // 점프 후 최고 높이 (월드 기준)
    private bool isJumping = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;

        if (rb == null)
            Debug.LogWarning("No Rigidbody2D found! Please add one.");
        if (animator == null)
            Debug.LogWarning("No Animator found! Please add one.");
        if (spriteRenderer == null)
            Debug.LogWarning("No SpriteRenderer found! Please add one.");
    }

    // 모든 트리거를 리셋하는 함수
    void ResetAllTriggers()
    {
        animator.ResetTrigger("JumpTrigger");
        animator.ResetTrigger("FallTrigger");
        animator.ResetTrigger("CroushTrigger");
        animator.ResetTrigger("Attack");
    }

    void Update()
    {
        // 바닥 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("verticalVelocity", rb.velocity.y);

        // 점프 입력: 땅에 있을 때 스페이스바 입력 시 점프 시작
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            ResetAllTriggers();
            jumpStartY = transform.position.y;
            maxJumpY = transform.position.y;
            isJumping = true;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            animator.SetTrigger("JumpTrigger");
            Debug.Log("Jump triggered. jumpStartY = " + jumpStartY);
        }

        // 공중에 있을 때 점프 상태이면 최고점 업데이트
        if (!isGrounded && isJumping)
        {
            maxJumpY = Mathf.Max(maxJumpY, transform.position.y);
        }

        // Fall 전이 처리:
        // 달리다가 ledge에서 떨어지는 경우
        if (!isGrounded && rb.velocity.y < 0f && wasGrounded && !animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"))
        {
            if (!Input.GetKey(KeyCode.Q))
            {
                ResetAllTriggers();
                Debug.Log("FallTrigger (running off ledge) called. rb.velocity.y = " + rb.velocity.y);
                animator.SetTrigger("FallTrigger");
            }
        }
        // 점프 후 낙하하는 경우
        else if (!isGrounded && isJumping && rb.velocity.y < 0f && !animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"))
        {
            float dropHeight = maxJumpY - transform.position.y;
            if (dropHeight >= fallThreshold)
            {
                if (!Input.GetKey(KeyCode.Q))
                {
                    ResetAllTriggers();
                    Debug.Log("FallTrigger (jump fall) called. dropHeight = " + dropHeight);
                    animator.SetTrigger("FallTrigger");
                    isJumping = false;
                }
            }
        }

        // 착지 감지: 이전 프레임에 공중이었고, 현재 땅이면
        if (!wasGrounded && isGrounded)
        {
            ResetAllTriggers();
            Debug.Log("Landing detected. current Y = " + transform.position.y);
            animator.SetTrigger("CroushTrigger");
            isJumping = false;
        }
        wasGrounded = isGrounded;

        // 땅에 있을 때 수평 입력 업데이트
        if (isGrounded && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            lastHorizontalInput = Input.GetAxis("Horizontal");
        }

        bool isCroush = animator.GetCurrentAnimatorStateInfo(0).IsName("Croush");
        bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

        // 이동 입력 처리 및 좌우 반전 (공격이나 croush 상태에서는 moveDirection을 0으로 설정)
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

        // 공격 입력: Q 키
        if (!isAttacking && Input.GetKeyDown(KeyCode.Q))
        {
            ResetAllTriggers();
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Attack");
        }

        // 달리기 애니메이션 처리: 땅에 있고, 공격이나 croush 상태가 아닐 때
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
        // 땅에 있을 때의 처리
        if (isGrounded)
        {
            bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
            bool isCroush = animator.GetCurrentAnimatorStateInfo(0).IsName("Croush");

            // 1. 땅에서 공격 중일 때는 이동 불가
            if (isAttacking)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
            // 2. 착지 후 croush 상태일 때는 crouchForwardSpeed만큼 살짝 이동 가능
            else if (isCroush)
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                rb.velocity = new Vector2(inputX * crouchForwardSpeed, rb.velocity.y);
            }
            else
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                if (Mathf.Approximately(inputX, 0f))
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                }
                else
                {
                    rb.velocity = new Vector2(inputX * moveSpeed, rb.velocity.y);
                }
            }
        }
        else
        {
            // 공중에서는 공격 중이 아니면 airControlMultiplier를 적용하고,
            // 입력이 없으면 Lerp를 통해 감속, 있을 경우 현재 방향으로 이동
            bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
            if (!isAttacking)
            {
                float inputX = Input.GetAxisRaw("Horizontal");
                if (Mathf.Approximately(inputX, 0f))
                {
                    rb.velocity = new Vector2(Mathf.Lerp(rb.velocity.x, 0, 0.3f), rb.velocity.y);
                }
                else
                {
                    rb.velocity = new Vector2(moveDirection.x * moveSpeed * airControlMultiplier, rb.velocity.y);
                }
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y);
            }
        }
    }

    public void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("공격에 맞은 적: " + enemy.name);
            
            // 공격 방향: 공격 포인트에서 적의 위치로 향하는 방향 계산
            Vector2 knockbackDirection = (enemy.transform.position - attackPoint.position).normalized;
            
            // EnemyController 스크립트의 TakeDamage 함수에 공격 데미지와 knockback 방향을 전달
            enemy.GetComponent<EnemyController>()?.TakeDamage(attackDamage, knockbackDirection);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
