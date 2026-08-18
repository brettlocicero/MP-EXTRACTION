using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class RoomEnemySpawner : NetworkBehaviour
{
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] float spawnDuration = 60f;
    [SerializeField] Vector2 spawnIntervalRange = new(1f, 2f);
    [SerializeField] float spawnRadius = 40f;
    [SerializeField] int maxAliveEnemies = 15;
    [SerializeField] Vector3 spawnOffset = Vector3.zero;

    public event System.Action<GameObject> OnEnemySpawned;
    public event System.Action OnSpawningComplete;

    bool isSpawning = false;
    int aliveCount = 0;

    public float SpawnDuration => spawnDuration;

    public void StartEnemySpawning()
    {
        if (!IsServer) return;
        if (isSpawning) return;

        isSpawning = true;
        StartCoroutine(SpawnEnemiesRoutine());
    }

    public void NotifyEnemyDied()
    {
        aliveCount--;
    }

    IEnumerator SpawnEnemiesRoutine()
    {
        float elapsed = 0f;

        while (elapsed < spawnDuration)
        {
            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return new WaitForSeconds(wait);
            elapsed += wait;

            if (aliveCount < maxAliveEnemies)
            {
                SpawnEnemy();
            }
        }

        isSpawning = false;
        OnSpawningComplete?.Invoke();
    }

    void SpawnEnemy()
    {
        Vector2 circlePoint = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + spawnOffset + new Vector3(circlePoint.x, 0f, circlePoint.y);

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        enemy.GetComponent<NetworkObject>().Spawn();

        aliveCount++;
        OnEnemySpawned?.Invoke(enemy);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + spawnOffset, spawnRadius);
    }
}