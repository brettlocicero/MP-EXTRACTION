using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] EnemySpawner spawner;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGameSession()
    {
        // if (!IsServer) return;

        Debug.Log("[GameManager] Network session verified. Starting initialization check...");
        if (spawner != null)
        {
            StartCoroutine(WaitForSpawnerAndStartLoop());
        }

        else
        {
            Debug.LogWarning("[GameManager] Spawner reference is missing!");
        }
    }

    IEnumerator WaitForSpawnerAndStartLoop()
    {
        while (spawner == null || !spawner.IsSpawned)
        {
            yield return null; 
        }

        Debug.Log("[GameManager] Spawner Netcode identity confirmed active. Launching game loop!");
        spawner.StartGameLoop();
    }
}