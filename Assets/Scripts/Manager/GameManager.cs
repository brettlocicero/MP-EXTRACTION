using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] RegionSO testRegion;
    [SerializeField] GameObject hubObjects;
    [SerializeField] Transform[] playerSpawnPoints;
    [SerializeField] EnemySpawner spawner;
    [SerializeField] CanvasGroup menuUI;
    [SerializeField] CanvasGroup ingameUI;

    [Header("Region Database")]
    [SerializeField] RegionSO[] regions;

    PlayerController localPlayer;

    public PlayerController LocalPlayer => localPlayer;

    public NetworkVariable<int> CurrentRegion = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        CurrentRegion.OnValueChanged += OnRegionChanged;

        if (CurrentRegion.Value != -1)
        {
            OnRegionChanged(-1, CurrentRegion.Value);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentRegion.OnValueChanged -= OnRegionChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    public void RegisterPlayer(PlayerController localPlayer)
    {
        this.localPlayer = localPlayer;
    }

    public void StartGameSession()
    {
        if (!IsServer)
            return;

        Debug.Log("[GameManager] Starting game session...");

        ClientGameStartEffectsClientRpc();
        MoveAllPlayers();
        CurrentRegion.Value = GetRegionIndex(testRegion);

        if (spawner != null)
        {
            StartCoroutine(WaitForSpawnerAndStartLoop());
        }
        
        else
        {
            Debug.LogWarning("[GameManager] Spawner reference is missing!");
        }
    }

    [ClientRpc]
    void ClientGameStartEffectsClientRpc()
    {
        hubObjects.SetActive(false);
    }

    void MoveAllPlayers()
    {
        int index = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            Transform spawn = playerSpawnPoints[index % playerSpawnPoints.Length];

            PlayerController player = client.PlayerObject.GetComponent<PlayerController>();
            if (player != null)
                player.Teleport(spawn.position, spawn.rotation);

            index++;
        }
    }

    IEnumerator WaitForSpawnerAndStartLoop()
    {
        while (spawner == null || !spawner.IsSpawned)
        {
            yield return null;
        }

        Debug.Log("[GameManager] Spawner ready. Starting game loop.");
        spawner.StartGameLoop();
    }

    void OnRegionChanged(int previousRegion, int newRegion)
    {
        if (newRegion < 0 || newRegion >= regions.Length)
            return;

        RegionGenerator.Instance.GenerateRegion(regions[newRegion]);
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

    void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SwapToGameUI();
        }
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