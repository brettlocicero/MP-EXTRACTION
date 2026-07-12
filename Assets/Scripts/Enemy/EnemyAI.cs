using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : NetworkBehaviour
{
    NavMeshAgent agent;
    
    [Header("AI Settings")]
    [SerializeField] float targetUpdateInterval = 0.5f;
    float nextTargetUpdateTime;

    [Header("Health Settings")]
    [SerializeField] float maxHealth = 100;
    
    // NetworkVariable syncs from Server to Clients by default.
    // We use NetworkVariableWritePermission.Server to ensure only the server can modify it.
    NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            agent.enabled = false;
        }

        // Initialize health on the server
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        // Subscribe to health changes so clients can react (e.g., update UI, play VFX)
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe to prevent memory leaks when the object is destroyed
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    void Update()
    {
        if (!IsServer) return;

        if (Time.time >= nextTargetUpdateTime)
        {
            nextTargetUpdateTime = Time.time + targetUpdateInterval;
            TargetClosestPlayer();
        }
    }

    void TargetClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        if (players.Length == 0) return;

        GameObject closestPlayer = null;
        float shortestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject player in players)
        {
            float distanceToPlayer = Vector3.Distance(currentPosition, player.transform.position);
            if (distanceToPlayer < shortestDistance)
            {
                shortestDistance = distanceToPlayer;
                closestPlayer = player;
            }
        }

        if (closestPlayer != null)
        {
            agent.SetDestination(closestPlayer.transform.position);
        }
    }

    /// <summary>
    /// Call this when the enemy should take damage (e.g., from a player projectile or sword).
    /// Can safely be called from server or clients; ServerRpc ensures execution happens on the server.
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (IsServer)
        {
            ModifyHealth(damageAmount);
        }
        
        else
        {
            // If a client detected the hit locally, they ask the server to apply the damage
            TakeDamageServerRpc(damageAmount);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void TakeDamageServerRpc(float damageAmount)
    {
        ModifyHealth(damageAmount);
    }

    // Core logic for updating health, kept strictly on the server
    void ModifyHealth(float damageAmount)
    {
        if (!IsServer) return;

        currentHealth.Value -= damageAmount;

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    // This runs on EVERYONE (Server + Clients) automatically whenever currentHealth changes
    void OnHealthChanged(float previousValue, float newValue)
    {
        Debug.Log($"Enemy Health Changed from {previousValue} to {newValue} on Client ID: {NetworkManager.Singleton.LocalClientId}");
        
        // UI updates or local blood/hit VFX go here!
    }

    void Die()
    {
        // Handle death logic (e.g., Despawn the object from the network)
        GetComponent<NetworkObject>().Despawn();
    }
}