using UnityEngine;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] GameObject enemyPrefab; 
    [SerializeField] int enemiesToSpawn = 5;
    [SerializeField] float spawnRadius = 10f;

    // The GameManager will now call this method directly.
    public void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[Spawner] Enemy prefab is not assigned!");
            return;
        }

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Generate a random position within a circle around the spawner
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            // 1. Instantiate the prefab locally on the server
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            // 2. Get the NetworkObject component
            NetworkObject netObj = enemyInstance.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                // 3. Spawn it across the network so all clients see it
                netObj.Spawn();
            }
            else
            {
                Debug.LogError($"[Spawner] The prefab {enemyPrefab.name} is missing a NetworkObject component!");
                Destroy(enemyInstance); 
            }
        }
        
        Debug.Log($"[Spawner] Successfully spawned {enemiesToSpawn} enemies.");
    }

    // Visualizes the spawn radius in the Unity Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}