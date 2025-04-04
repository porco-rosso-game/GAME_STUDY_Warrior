using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;            // 이동 속도
    public float attackRange = 1f;          // 공격 범위
    public int attackDamage = 10;           // 공격 데미지
    public Transform attackPoint;           // 공격 위치 (플레이어 앞쪽에 배치)
    public LayerMask enemyLayers;           // 적이 포함된 레이어

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
        // 현재 Attack 상태이면 이동 입력 무시
        bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

        if (!isAttacking)
        {
            // 이동 입력 처리 (좌/우/상/하)
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            moveDirection = new Vector2(moveX, moveY).normalized;

            // 좌우 입력에 따라 스프라이트 반전 (왼쪽이면 flipX true, 오른쪽이면 false)
            if (moveX < 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (moveX > 0)
            {
                spriteRenderer.flipX = false;
            }
        }
        else
        {
            moveDirection = Vector2.zero;
        }

        // 공격 입력: 키패드 1을 누르면 공격 (현재 공격 중이 아닐 때만)
        if (!isAttacking && Input.GetKeyDown(KeyCode.Q))
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Attack");
        }

        // 이동 관련 입력 처리 (공격 중이 아닐 때)
        if (!isAttacking)
        {
            // 달리기 입력: 왼쪽(A, LeftArrow) 또는 오른쪽(D, RightArrow) 키가 눌렸으면 바로 Run 애니메이션 시작
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
        // Animator의 IsRunning 파라미터가 true일 때만 이동 처리
        if (animator.GetBool("IsRunning"))
        {
            rb.velocity = moveDirection * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    // Attack 애니메이션의 Animation Event에서 호출되는 함수
    public void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("공격에 맞은 적: " + enemy.name);
            // 예시: enemy.GetComponent<Enemy>().TakeDamage(attackDamage);
        }
    }

    // 에디터에서 공격 범위를 시각화하기 위한 함수
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
