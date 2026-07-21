using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] GameObject enemyPrefab;

    [Header("Spawning")]
    [SerializeField] float spawnRadius = 10f;
    [SerializeField] int minSpawnPerTick = 1;
    [SerializeField] int maxSpawnPerTick = 3;
    [SerializeField] int maxAliveEnemies = 50;

    [Header("Malevolence")]
    [SerializeField] float malevolenceStepTime = 180f; // 3 minutes
    [SerializeField] int maxMalevolence = 5;

    [Header("Spawn Rates (seconds between spawns)")]
    [SerializeField] float[] spawnIntervals =
    {
        5f,     // Malevolence 1
        3.5f,   // Malevolence 2
        2.5f,   // Malevolence 3
        1.75f,  // Malevolence 4
        1f      // Malevolence 5
    };

    public NetworkVariable<int> malevolence = new(1);
    public NetworkVariable<float> malevolenceCountdown = new(180f);
    int aliveEnemies = 0;

    public void StartGameLoop()
    {
        if (!IsServer)
            return;

        StartCoroutine(SpawnRoutine());
        StartCoroutine(MalevolenceRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            int amountToSpawn = Random.Range(minSpawnPerTick, maxSpawnPerTick + 1);

            for (int i = 0; i < amountToSpawn; i++)
            {
                if (aliveEnemies >= maxAliveEnemies)
                    break;

                SpawnSingleEnemy();
            }

            int index = Mathf.Clamp(malevolence.Value - 1, 0, spawnIntervals.Length - 1);
            yield return new WaitForSeconds(spawnIntervals[index]);
        }
    }

    IEnumerator MalevolenceRoutine()
    {
        while (malevolence.Value < maxMalevolence)
        {
            malevolenceCountdown.Value = malevolenceStepTime;

            while (malevolenceCountdown.Value > 0f)
            {
                yield return new WaitForSeconds(1f);
                malevolenceCountdown.Value--;
            }

            malevolence.Value++;

            Debug.Log($"Malevolence increased to {malevolence.Value}");
        }

        // At max level, stop the timer.
        malevolenceCountdown.Value = 0f;
    }

    void SpawnSingleEnemy()
    {
        if (!IsServer || aliveEnemies >= maxAliveEnemies)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        if (enemy.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Spawn();
            aliveEnemies++;

            if (enemy.TryGetComponent(out EnemyAI enemyAI))
                enemyAI.OnEnemyKilled += OnEnemyKilled;
        }
        
        else
        {
            Destroy(enemy);
        }
    }

    void OnEnemyKilled(EnemyAI enemyAI)
    {
        if (!IsServer)
            return;

        enemyAI.OnEnemyKilled -= OnEnemyKilled;
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}