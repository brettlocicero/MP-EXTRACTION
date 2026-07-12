using UnityEngine;
using Unity.Netcode;

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
        Debug.Log("[GameManager] Network session verified. Initializing game loop...");
        
        if (spawner != null)
        {
            spawner.SpawnEnemies();
        }
        
        else
        {
            Debug.LogWarning("[GameManager] Spawner reference is missing!");
        }
    }
}