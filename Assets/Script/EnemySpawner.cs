using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // 적 프리팹
    public float spawnInterval = 2f; // 적 생성 간격

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            // 적 생성
            Instantiate(enemyPrefab, new Vector2(Random.Range(-8f, 8f), 6f), Quaternion.identity);
            yield return new WaitForSeconds(spawnInterval); // 일정 시간 대기
        }
    }
}
