using UnityEngine;

public class SpellController : MonoBehaviour
{
    private Animator animator;
    public bool isSpellAnimating = false;  // Spell 애니메이션 진행 여부
    public EnemyController enemyController; // Inspector에서 직접 할당하거나 자동으로 찾음

    [Header("Spell Damage Settings")]
    public Transform spellPoint;      // Spell 효과 발생 위치 (Inspector에 할당)
    public float spellRange = 1f;       // Spell 범위
    public int spellDamage = 15;        // Spell 데미지
    public LayerMask playerLayers;      // 플레이어가 속한 레이어

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (enemyController == null)
            enemyController = FindObjectOfType<EnemyController>();
    }

    public void ActivateSpell()
    {
        if (isSpellAnimating)
            return;
        isSpellAnimating = true;
        if (enemyController != null && enemyController.player != null)
        {
            transform.SetParent(null); // 독립적 오브젝트로 전환
            // 플레이어의 위치에서 y축으로 위로 이동한 위치로 설정
            Vector3 pos = enemyController.player.transform.position;
            pos.y += 1.5f;
            transform.position = pos;
        }
        gameObject.SetActive(true);
        if (animator != null)
            animator.Play("Spell"); // "Spell"은 Spell 애니메이션 클립의 상태 이름
    }

    // Spell 애니메이션 타격 시점에 Animation Event로 호출되어 데미지를 적용합니다.
    public void DealSpellDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(spellPoint.position, spellRange, playerLayers);
        if (hitPlayer != null)
        {
            PlayerController pc = hitPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(spellDamage);
                Debug.Log("Spell hit! Damage applied: " + spellDamage);
            }
        }
    }

    // Spell 애니메이션 종료 시 Animation Event로 호출됩니다.
    public void OnSpellAnimationEnd()
    {
        gameObject.SetActive(false);
        isSpellAnimating = false;
        if (enemyController != null)
            enemyController.OnSpellAnimationComplete();
    }

    public void CancelSpell()
    {
        if (animator != null)
            animator.StopPlayback();
        gameObject.SetActive(false);
        isSpellAnimating = false;
    }
}
