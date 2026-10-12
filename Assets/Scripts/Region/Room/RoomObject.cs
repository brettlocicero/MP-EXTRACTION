using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class RoomObject : NetworkBehaviour
{
    [SerializeField] EnemyAI[] enemyPool;
    [SerializeField] int threatBudget = 20;
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] float minSpawnDistance = 20f;
    [SerializeField] float maxSpawnDistance = 40f;
    [SerializeField] Transform relicSpawnPoint;
    [SerializeField] RoomPortal portalPrefab;
    [SerializeField] Transform portalSpawnPoint;

    RoomPortal portal;
    bool spawningComplete;

    readonly List<EnemyAI> aliveEnemies = new();

    public void InitRoom()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        WaitForSeconds delay = new(spawnInterval);

        int spentThreat = 0;

        while (spentThreat < threatBudget)
        {
            spentThreat += SpawnEnemy().Threat;
            yield return delay;
        }

        spawningComplete = true;
        TryClearRoom();
    }

    EnemyAI PickEnemy()
    {
        float roll = Random.value * enemyPool.Sum(enemy => 1f / enemy.Threat);

        foreach (EnemyAI enemy in enemyPool)
        {
            roll -= 1f / enemy.Threat;

            if (roll <= 0f)
                return enemy;
        }

        return enemyPool[^1];
    }

    EnemyAI SpawnEnemy()
    {
        EnemyAI prefab = PickEnemy();

        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);

        EnemyAI enemy = Instantiate(prefab, position, Quaternion.identity);
        enemy.NetworkObject.Spawn();
        enemy.OnEnemyKilled += OnEnemyKilled;
        aliveEnemies.Add(enemy);

        return enemy;
    }

    void OnEnemyKilled(EnemyAI enemy)
    {
        enemy.OnEnemyKilled -= OnEnemyKilled;
        aliveEnemies.Remove(enemy);
        TryClearRoom();
    }

    void TryClearRoom()
    {
        if (spawningComplete && aliveEnemies.Count == 0)
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