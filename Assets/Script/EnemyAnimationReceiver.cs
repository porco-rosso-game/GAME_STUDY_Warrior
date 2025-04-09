using UnityEngine;

public class EnemyAnimationReceiver : MonoBehaviour
{
    private EnemyController enemyController;

    void Awake()
    {
        // 부모(GameObject)를 검색하여 EnemyController를 찾습니다.
        enemyController = GetComponentInParent<EnemyController>();
        if(enemyController == null)
        {
            Debug.LogError("EnemyAnimationReceiver: Parent does not contain an EnemyController component!");
        }
    }
    
    // Animation Event로 호출 (애니메이션 이벤트 함수 이름을 "CastSpell"으로 설정)
    public void CastSpell()
    {
        if(enemyController != null)
        {
            enemyController.CastSpell();
        }
    }
    
    // Animation Event로 호출 (애니메이션 이벤트 함수 이름을 "DealAttackDamage"으로 설정)
    public void DealAttackDamage()
    {
        if(enemyController != null)
        {
            enemyController.DealAttackDamage();
        }
    }
}
