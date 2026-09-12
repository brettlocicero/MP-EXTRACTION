using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoomEnemySpawn : NetworkBehaviour
{
    [SerializeField] EnemyAI[] enemies;
    [SerializeField] int count = 15;
    [SerializeField] float spawnRadius = 25f;
    [SerializeField] float spawnInterval = 1f;

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        TriggerSpawnServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void TriggerSpawnServerRpc()
    {
        if (triggered)
            return;

        triggered = true;
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy()
    {
        if (enemies.Length == 0)
            return;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, 0f, offset.y);

        EnemyAI prefab = enemies[Random.Range(0, enemies.Length)];
        EnemyAI enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        enemy.GetComponent<NetworkObject>().Spawn();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}