using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoomObject : NetworkBehaviour
{
    [SerializeField] EnemyAI[] enemyPool;
    [SerializeField] int baseEnemyCount = 10;
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] float minSpawnDistance = 20f;
    [SerializeField] float maxSpawnDistance = 40f;
    [SerializeField] Transform relicSpawnPoint;
    [SerializeField] RoomPortal portalPrefab;
    [SerializeField] Transform portalSpawnPoint;

    RoomPortal portal;
    int enemiesKilled;

    readonly List<EnemyAI> aliveEnemies = new();

    public void InitRoom()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        WaitForSeconds delay = new(spawnInterval);

        for (int i = 0; i < baseEnemyCount; i++)
        {
            SpawnEnemy();
            yield return delay;
        }
    }

    void SpawnEnemy()
    {
        EnemyAI prefab = enemyPool[Random.Range(0, enemyPool.Length)];

        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);

        EnemyAI enemy = Instantiate(prefab, position, Quaternion.identity);
        enemy.NetworkObject.Spawn();
        enemy.OnEnemyKilled += OnEnemyKilled;
        aliveEnemies.Add(enemy);
    }

    void OnEnemyKilled(EnemyAI enemy)
    {
        enemy.OnEnemyKilled -= OnEnemyKilled;
        aliveEnemies.Remove(enemy);

        if (++enemiesKilled == baseEnemyCount)
            ClearRoom();
    }

    void ClearRoom()
    {
        portal = Instantiate(portalPrefab, portalSpawnPoint.position, portalSpawnPoint.rotation);
        portal.NetworkObject.Spawn();

        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            client.PlayerObject.GetComponent<PlayerRelics>().OfferRelics(relicSpawnPoint.position);
    }

    public void RelicChosen(ulong clientId)
    {
        portal.RelicChosen(clientId);
    }

    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();

        foreach (EnemyAI enemy in aliveEnemies)
        {
            if (enemy != null)
            {
                enemy.OnEnemyKilled -= OnEnemyKilled;

                if (enemy.IsSpawned)
                    enemy.NetworkObject.Despawn();
            }
        }

        aliveEnemies.Clear();

        if (portal != null && portal.IsSpawned)
            portal.NetworkObject.Despawn();
    }
}