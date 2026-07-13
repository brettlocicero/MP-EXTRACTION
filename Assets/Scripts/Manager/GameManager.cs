using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] EnemySpawner spawner;
    [SerializeField] CanvasGroup menuUI;
    [SerializeField] CanvasGroup ingameUI;

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
        Debug.Log("[GameManager] Network session verified. Starting initialization check...");
        if (spawner != null)
        {
            StartCoroutine(WaitForSpawnerAndStartLoop());
        }

        else
        {
            Debug.LogWarning("[GameManager] Spawner reference is missing!");
        }
        
        SwapToGameUI();
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
    
    void SwapToGameUI() 
    {
        menuUI.alpha = 0f;
        menuUI.blocksRaycasts = false;
        menuUI.interactable = false;
        
        ingameUI.alpha = 1f;
        ingameUI.blocksRaycasts = true;
        ingameUI.interactable = true;
    }
}