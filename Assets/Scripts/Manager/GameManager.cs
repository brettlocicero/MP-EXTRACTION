using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] RegionSO testRegion;
    [SerializeField] CanvasGroup menuUI;
    [SerializeField] CanvasGroup ingameUI;
    [SerializeField] EnemySpawner enemySpawner;

    [Header("Region Database")]
    [SerializeField] RegionSO[] regions;

    [Header("Arena")]
    [SerializeField] GameObject hubObjects;
    [SerializeField] Transform regionRoot;
    [SerializeField, Min(10f)] float arenaSize = 1000f;
    [SerializeField] Material arenaMaterial;
    [SerializeField] RegionTransitionAnimator regionTransitionAnimator;
    [SerializeField] float transitionDuration = 2f;

    PlayerController localPlayer;
    GameObject arena;
    bool enteringRegion;

    public PlayerController LocalPlayer => localPlayer;

    public NetworkVariable<int> CurrentRegion = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> RegionKills = new(
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
        if (IsServer)
        {
            CurrentRegion.Value = -1;
            RegionKills.Value = 0;
        }

        CurrentRegion.OnValueChanged += OnRegionChanged;
        RegionKills.OnValueChanged += OnKillsChanged;

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
        RegionKills.OnValueChanged -= OnKillsChanged;
        StopAllCoroutines();
        enteringRegion = false;
        if (enemySpawner != null)
            enemySpawner.StopSpawning();
        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.SetRegionActive(false);
        if (arena != null)
            Destroy(arena);
        if (hubObjects != null)
            hubObjects.SetActive(true);
        localPlayer = null;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void RegisterPlayer(PlayerController localPlayer)
    {
        this.localPlayer = localPlayer;
        if (arena != null && CurrentRegion.Value >= 0)
            localPlayer.Teleport(Vector3.zero, Quaternion.identity);
    }

    public void StartGameSession()
    {
        if (!IsServer || enteringRegion || CurrentRegion.Value >= 0)
            return;

        int regionIndex = GetRegionIndex(testRegion);
        if (regionIndex < 0)
            return;

        enteringRegion = true;
        StartCoroutine(EnterRegionRoutine(regionIndex));
    }

    IEnumerator EnterRegionRoutine(int regionIndex)
    {
        PlayTransitionRpc();
        yield return new WaitForSeconds(transitionDuration);

        RegionKills.Value = 0;
        CurrentRegion.Value = regionIndex;
        enemySpawner.StartSpawning();
        enteringRegion = false;
    }

    [Rpc(SendTo.Everyone)]
    void PlayTransitionRpc()
    {
        regionTransitionAnimator.PlayTransition();
    }

    void OnRegionChanged(int previousRegion, int newRegion)
    {
        if (newRegion < 0 || newRegion >= regions.Length)
            return;

        if (arena == null)
        {
            arena = GameObject.CreatePrimitive(PrimitiveType.Plane);
            arena.name = "Arena";
            arena.layer = LayerMask.NameToLayer("Ground");
            arena.transform.SetParent(regionRoot, false);
            // The existing player capsule is two metres tall and centred on its origin.
            arena.transform.position = Vector3.down;
            arena.transform.localScale = new Vector3(arenaSize / 10f, 1f, arenaSize / 10f);
            arena.GetComponent<Renderer>().sharedMaterial = arenaMaterial;
        }

        hubObjects.SetActive(false);
        regions[newRegion].ApplyRegionAtmosphere();
        Physics.SyncTransforms();
        if (localPlayer != null)
            localPlayer.Teleport(Vector3.zero, Quaternion.identity);
        CombatUIManager.Instance.SetRegionActive(true);
        CombatUIManager.Instance.UpdateKillCount(RegionKills.Value);
    }

    void OnKillsChanged(int previous, int current)
    {
        CombatUIManager.Instance.UpdateKillCount(current);
    }

    public void RegisterEnemyKill()
    {
        if (IsServer && CurrentRegion.Value >= 0)
            RegionKills.Value++;
    }

    int GetRegionIndex(RegionSO region)
    {
        for (int i = 0; i < regions.Length; i++)
        {
            if (region != null && regions[i] == region)
                return i;
        }

        Debug.LogError("The starting region is not present in the GameManager region list.");
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

    public PlayerItems GetLocalPlayerItems()
    {
        return LocalPlayer.GetComponentInChildren<PlayerItems>();
    }
}
