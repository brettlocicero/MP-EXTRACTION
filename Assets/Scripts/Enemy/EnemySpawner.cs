using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] GameObject enemyPrefab; 
    [SerializeField] int baseEnemiesPerWave = 5;
    [SerializeField] int extraEnemiesPerWave = 2;
    [SerializeField] float spawnRadius = 10f;
    [SerializeField] float spawnDelay = 1f;
    [SerializeField] float timeBetweenWaves = 20f;

    public NetworkVariable<int> currentWave = new NetworkVariable<int>(0);
    public NetworkVariable<float> waveCountdown = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> isIntermission = new NetworkVariable<bool>(false);

    int enemiesRemainingToKill = 0;

    public void StartGameLoop()
    {
        StartCoroutine(GameLoopRoutine());
    }

    IEnumerator GameLoopRoutine()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[Spawner] Enemy prefab is not assigned!");
            yield break;
        }

        while (true)
        {
            currentWave.Value++;
            isIntermission.Value = false;
            
            // Calculate total enemy count for this wave
            int enemiesToSpawn = baseEnemiesPerWave + ((currentWave.Value - 1) * extraEnemiesPerWave);
            enemiesRemainingToKill = enemiesToSpawn;

            Debug.Log($"[Spawner] Wave {currentWave.Value} Started! Spawning {enemiesToSpawn} enemies.");

            // Spawn enemies slowly, one by one
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnSingleEnemy();
                yield return new WaitForSeconds(spawnDelay);
            }

            // Wait until all spawned enemies are dead before starting the countdown
            while (enemiesRemainingToKill > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // --- INTERMISSION PERIOD ---
            isIntermission.Value = true;
            waveCountdown.Value = timeBetweenWaves;

            while (waveCountdown.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                waveCountdown.Value -= 1f; // Countdown by 1 second intervals
            }
        }
    }

    void SpawnSingleEnemy()
    {
        if (!IsServer) return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = enemyInstance.GetComponent<NetworkObject>();
        
        if (netObj != null)
        {
            netObj.Spawn();
            
            // Get our EnemyAI component and subscribe to our custom death event
            EnemyAI enemyAI = enemyInstance.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.OnEnemyKilled += OnEnemyKilled;
            }
        }
        else
        {
            Debug.LogError($"[Spawner] {enemyPrefab.name} is missing a NetworkObject component!");
            Destroy(enemyInstance);
            enemiesRemainingToKill--; // Adjust tracking so game doesn't get stuck
        }
    }

    // Updated parameter to accept our EnemyAI component type
    void OnEnemyKilled(EnemyAI enemyAI)
    {
        if (!IsServer) return;

        // Clean up the event listener subscription safely
        enemyAI.OnEnemyKilled -= OnEnemyKilled;
        
        enemiesRemainingToKill--;
        Debug.Log($"[Spawner] Enemy killed! Remaining: {enemiesRemainingToKill}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}