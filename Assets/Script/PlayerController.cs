using UnityEngine;

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
    public float crouchForwardSpeed = 1f;   // croush 상태에서 전진하는 속도 (원하는 값으로 조정)

    private bool isGrounded;
    private bool wasGrounded = false;       // 이전 프레임의 바닥 접촉 상태

    // 점프하기 직전에 땅에 있을 때의 수평 입력 값을 저장 (공중에서는 업데이트되지 않음)
    private float lastHorizontalInput = 0f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null)
            Debug.LogWarning("No Rigidbody2D found! Please add one.");
        if (animator == null)
            Debug.LogWarning("No Animator found! Please add one.");
        if (spriteRenderer == null)
            Debug.LogWarning("No SpriteRenderer found! Please add one.");
    }

    void Update()
    {
        // 바닥 체크: groundCheck 위치의 원형 영역으로 바닥 감지
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("verticalVelocity", rb.velocity.y);

        // 착지 감지: 이전 프레임에는 공중이었는데, 이번 프레임에 바닥에 닿으면 CroushTrigger 발동
        if (!wasGrounded && isGrounded)
        {
            animator.SetTrigger("CroushTrigger");
        }
        wasGrounded = isGrounded;

        // 땅에 있을 때만 수평 입력을 업데이트 → 점프 직전의 마지막 입력이 유지됨
        if (isGrounded && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            lastHorizontalInput = Input.GetAxis("Horizontal");
        }

        bool isCroush = animator.GetCurrentAnimatorStateInfo(0).IsName("Croush");
        bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

        if (isCroush)
        {
            moveDirection = Vector2.zero;
            animator.SetBool("IsRunning", false);
        }
        else if (!isAttacking)
        {
            // 이동 입력 처리 (좌/우/상/하)
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            moveDirection = new Vector2(moveX, moveY).normalized;

            // 좌우 입력에 따라 스프라이트 반전
            if (moveX < 0)
                spriteRenderer.flipX = true;
            else if (moveX > 0)
                spriteRenderer.flipX = false;
        }
        else
        {
            moveDirection = Vector2.zero;
        }

        // 공격 입력: Q 키
        if (!isAttacking && Input.GetKeyDown(KeyCode.Q))
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Attack");
        }

        // 점프 입력: 스페이스바 (땅에 있을 때만)
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            animator.SetTrigger("JumpTrigger");
        }

        // 달리기 애니메이션 처리: croush 상태가 아니고, 공격 중이 아니며 땅에 있을 때만 적용
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
        bool isCroush = animator.GetCurrentAnimatorStateInfo(0).IsName("Croush");

        if (isCroush)
        {
            // croush 상태에서 점프하기 전의 마지막 입력(lastHorizontalInput)이 0이 아니면,
            // 그 방향으로 전진, 0이면 전진하지 않음.
            if (Mathf.Abs(lastHorizontalInput) > 0.01f)
            {
                rb.velocity = new Vector2(lastHorizontalInput * crouchForwardSpeed, rb.velocity.y);
            }
            else
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        else if (animator.GetBool("IsRunning"))
        {
            rb.velocity = new Vector2(moveDirection.x * moveSpeed, rb.velocity.y);
        }
        else if (isGrounded)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    public void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("공격에 맞은 적: " + enemy.name);
            // 실제 게임에서는 enemy에게 데미지를 주는 로직을 추가하세요.
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
