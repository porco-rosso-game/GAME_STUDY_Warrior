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
    public float fallThreshold = 0.5f;      // 최고점(maxJumpY)에서 떨어진 높이가 이 값 이상이면 Fall 전이 (유닛)

    private bool isGrounded;
    private bool wasGrounded = false;       // 이전 프레임의 바닥 접촉 상태

    // 점프 전 수평 입력 값 (공중에서는 업데이트되지 않음)
    private float lastHorizontalInput = 0f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // 초기 localScale (플립용)
    private Vector3 initialScale;

    // 점프 관련 변수
    private float jumpStartY = 0f; // 점프 시작 시의 높이 (월드 좌표)
    private float maxJumpY = 0f;   // 점프 후 최고 높이
    private bool isJumping = false; // 점프 상태 추적

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale; // 기본 스케일 저장

        if (rb == null)
            Debug.LogWarning("No Rigidbody2D found! Please add one.");
        if (animator == null)
            Debug.LogWarning("No Animator found! Please add one.");
        if (spriteRenderer == null)
            Debug.LogWarning("No SpriteRenderer found! Please add one.");
    }

    void Update()
    {
        // 바닥 체크: groundCheck의 원형 영역으로 바닥 감지
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("verticalVelocity", rb.velocity.y);

        // 점프 입력: 땅에 있을 때 스페이스바를 누르면 점프 시작
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            jumpStartY = transform.position.y;  // 점프 시작 높이 기록
            maxJumpY = transform.position.y;      // 최고 높이 초기화
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
        // 두 경우를 처리:
        // 1. 달리다가 ledge에서 떨어질 때: 이전 프레임에 땅에 있었고(isGrounded가 true였음)
        //    현재 땅에 닿지 않고(rb.velocity.y <= 0f) 즉시 Fall 전이
        // 2. 점프 후 낙하: 점프 상태(isJumping)가 true이고, rb.velocity.y < 0f이며,
        //    최고점과 현재 높이의 차이가 fallThreshold 이상이면 Fall 전이
        if (!isGrounded && rb.velocity.y < 0f && !animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"))
        {
            // 달리다가 떨어지는 경우
            if (wasGrounded)
            {
                if (!Input.GetKey(KeyCode.Q))
                {
                    Debug.Log("FallTrigger (running off ledge) called. rb.velocity.y = " + rb.velocity.y);
                    animator.SetTrigger("FallTrigger");
                }
            }
            // 점프 후 낙하하는 경우
            else if (isJumping)
            {
                float dropHeight = maxJumpY - transform.position.y;
                if (dropHeight >= fallThreshold)
                {
                    if (!Input.GetKey(KeyCode.Q))
                    {
                        Debug.Log("FallTrigger (jump fall) called. dropHeight = " + dropHeight);
                        animator.SetTrigger("FallTrigger");
                        isJumping = false; // Fall 전이 후 점프 상태 종료
                    }
                }
            }
        }

        // 착지 감지: 이전 프레임에는 공중이었고, 이번 프레임에 땅에 닿으면 착지 처리
        if (!wasGrounded && isGrounded)
        {
            Debug.Log("Landing detected. current Y = " + transform.position.y);
            animator.SetTrigger("CroushTrigger");
            isJumping = false;
        }
        wasGrounded = isGrounded;

        // 땅에 있을 때만 수평 입력 업데이트 (공중에서는 마지막 입력 유지)
        if (isGrounded && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            lastHorizontalInput = Input.GetAxis("Horizontal");
        }

        bool isCroush = animator.GetCurrentAnimatorStateInfo(0).IsName("Croush");
        bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

        // 이동 입력 처리 및 좌우 반전 (부모의 localScale 사용)
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
            animator.ResetTrigger("FallTrigger");
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Attack");
        }

        // 달리기 애니메이션 처리: 공격이나 croush 상태가 아니며 땅에 있을 때만 적용
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
            if (Mathf.Abs(lastHorizontalInput) > 0.01f)
                rb.velocity = new Vector2(lastHorizontalInput * crouchForwardSpeed, rb.velocity.y);
            else
                rb.velocity = new Vector2(0, rb.velocity.y);
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
            // enemy에게 데미지를 주는 로직 추가
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
