using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] RegionSO testRegion;
    [SerializeField] Transform[] playerSpawnPoints;
    [SerializeField] CanvasGroup menuUI;
    [SerializeField] CanvasGroup ingameUI;
    [SerializeField] EnemySpawner enemySpawner;

    [Header("Region Database")]
    [SerializeField] RegionSO[] regions;

    [Header("Single Player")]
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Camera menuCamera;
    [SerializeField] UnityEngine.UI.Button playButton;

    PlayerController localPlayer;
    bool hasGeneratedRegion = false;

    public PlayerController LocalPlayer => localPlayer;

    public int CurrentRegion { get; private set; } = -1;

    public int RegionSeed { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        playButton.onClick.AddListener(StartSinglePlayer);
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(StartSinglePlayer);
        if (Instance == this) Instance = null;
    }

    public void StartSinglePlayer()
    {
        if (localPlayer != null) return;
        if (playerPrefab == null)
        {
            Debug.LogError("GameManager: assign the player prefab.");
            return;
        }

        Transform spawn = playerSpawnPoints.Length > 0 ? playerSpawnPoints[0] : null;
        GameObject player = Instantiate(playerPrefab,
            spawn != null ? spawn.position : Vector3.zero,
            spawn != null ? spawn.rotation : Quaternion.identity);
        RegisterPlayer(player.GetComponent<PlayerController>());
        if (menuCamera != null) menuCamera.gameObject.SetActive(false);
        SwapToGameUI();
    }

    public void RegisterPlayer(PlayerController localPlayer)
    {
        this.localPlayer = localPlayer;
    }

    public void StartGameSession()
    {
        if (hasGeneratedRegion || localPlayer == null) return;

        Debug.Log("[GameManager] Starting game session...");

        int seed = new System.Random().Next(int.MinValue, int.MaxValue);
        int regionIndex = GetRegionIndex(testRegion);

        if (regionIndex < 0) return;

        RegionSeed = seed;
        CurrentRegion = regionIndex;

        GenerateRegion(regions[regionIndex], seed);
        enemySpawner.StartSpawning();
    }

    void GenerateRegion(RegionSO region, int seed)
    {
        if (hasGeneratedRegion) return;

        RegionGenerator.Instance.GenerateRegion(region, seed);
        hasGeneratedRegion = true;
    }

    int GetRegionIndex(RegionSO region)
    {
        for (int i = 0; i < regions.Length; i++)
        {
            if (regions[i] == region)
                return i;
        }

        Debug.LogError($"Region '{region.name}' is not present in the GameManager region list.");
        return -1;
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

    public PlayerItems GetLocalPlayerItems()
    {
        return LocalPlayer.GetComponentInChildren<PlayerItems>();
    }
}