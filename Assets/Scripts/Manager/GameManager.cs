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
    [SerializeField] CanvasGroup menuUI;
    [SerializeField] CanvasGroup ingameUI;

    [Header("Region Database")]
    [SerializeField] RegionSO[] regions;

    PlayerController localPlayer;
    bool hasGeneratedRegion = false;

    public PlayerController LocalPlayer => localPlayer;

    public NetworkVariable<int> CurrentRegion = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> RegionSeed = new(
        0,
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
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentRegion.OnValueChanged -= OnRegionChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
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

        int seed = new System.Random().Next(int.MinValue, int.MaxValue);
        int regionIndex = GetRegionIndex(testRegion);

        RegionSeed.Value = seed;
        CurrentRegion.Value = regionIndex;

        GenerateRegionClientRpc(regionIndex, seed);
    }

    [ClientRpc]
    void GenerateRegionClientRpc(int regionIndex, int seed)
    {
        if (regionIndex < 0 || regionIndex >= regions.Length)
            return;

        GenerateRegion(regions[regionIndex], seed);
    }

    void OnRegionChanged(int previousRegion, int newRegion)
    {
        if (newRegion < 0 || newRegion >= regions.Length)
            return;

        GenerateRegion(regions[newRegion], RegionSeed.Value);
    }

    void GenerateRegion(RegionSO region, int seed)
    {
        if (hasGeneratedRegion) return;

        hubObjects.SetActive(false);
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

    void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SwapToGameUI();
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            PlayerCurrency currency = client.PlayerObject.GetComponent<PlayerCurrency>();
            currency.PersistCurrentSouls();
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