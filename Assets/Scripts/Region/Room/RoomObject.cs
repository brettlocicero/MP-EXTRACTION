using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomObject : MonoBehaviour
{
    [SerializeField] RoomEnemySpawner enemySpawner;
    [SerializeField] Transform connector;
    [SerializeField] GameObject barrierObject;

    bool hasTriggered = false;
    bool spawningComplete = false;
    int enemiesAlive = 0;

    bool barrierUpValue = true;
    bool barrierUp
    {
        get => barrierUpValue;
        set
        {
            if (barrierUpValue == value) return;
            bool previous = barrierUpValue;
            barrierUpValue = value;
            OnBarrierChanged(previous, value);
        }
    }

    bool combatActiveValue = false;
    bool combatActive
    {
        get => combatActiveValue;
        set
        {
            if (combatActiveValue == value) return;
            bool previous = combatActiveValue;
            combatActiveValue = value;
            OnCombatActiveChanged(previous, value);
        }
    }

    int killCountValue = 0;
    int killCount
    {
        get => killCountValue;
        set
        {
            if (killCountValue == value) return;
            int previous = killCountValue;
            killCountValue = value;
            OnKillCountChanged(previous, value);
        }
    }

    public Transform Connector => connector;

    void Start()
    {
        if (barrierObject)
        {
            barrierObject.SetActive(barrierUp);
        }

        if (combatActive)
        {
            CombatUIManager.Instance.NotifyCombatStarted(enemySpawner.SpawnDuration);
            CombatUIManager.Instance.UpdateKillCount(killCount);
        }
    }

    void OnDestroy()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemySpawned -= RegisterEnemy;
            enemySpawner.OnSpawningComplete -= OnSpawningComplete;
        }
        if (combatActive && CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.NotifyCombatEnded();
        }
    }

    void OnBarrierChanged(bool previous, bool current)
    {
        if (barrierObject)
        {
            barrierObject.SetActive(current);
        }
    }

    void OnCombatActiveChanged(bool previous, bool current)
    {
        if (current)
        {
            CombatUIManager.Instance.NotifyCombatStarted(enemySpawner.SpawnDuration);
        }
        else
        {
            CombatUIManager.Instance.NotifyCombatEnded();
        }
    }

    void OnKillCountChanged(int previous, int current)
    {
        CombatUIManager.Instance.UpdateKillCount(current);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.GetComponent<PlayerController>() == null) return;

        TriggerRoom();
    }

    void TriggerRoom()
    {
        if (hasTriggered) return;

        hasTriggered = true;
        combatActive = true;

        enemySpawner.OnEnemySpawned += RegisterEnemy;
        enemySpawner.OnSpawningComplete += OnSpawningComplete;
        enemySpawner.StartEnemySpawning();
    }

    void OnSpawningComplete()
    {
        spawningComplete = true;
        enemySpawner.OnSpawningComplete -= OnSpawningComplete;

        if (enemiesAlive <= 0)
        {
            ClearRoom();
        }
    }

    void RegisterEnemy(GameObject enemy)
    {
        enemiesAlive++;

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        enemyAI.OnEnemyKilled += OnEnemyKilled;
    }

    void OnEnemyKilled(EnemyAI enemy)
    {
        enemy.OnEnemyKilled -= OnEnemyKilled;
        enemiesAlive--;
        enemySpawner.NotifyEnemyDied();
        killCount++;

        if (spawningComplete && enemiesAlive <= 0)
        {
            ClearRoom();
        }
    }

    void ClearRoom()
    {
        barrierUp = false;
        combatActive = false;
    }
}
