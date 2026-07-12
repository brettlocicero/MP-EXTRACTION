using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NetworkEnemyAI : NetworkBehaviour
{
    NavMeshAgent agent;
    
    [Header("AI Settings")]
    [SerializeField] float targetUpdateInterval = 0.5f;
    float nextTargetUpdateTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        // Disable the NavMeshAgent on clients. 
        // Only the server should handle AI pathfinding logic.
        if (!IsServer)
        {
            agent.enabled = false;
        }
    }

    void Update()
    {
        // Guard clause: Only execute AI logic on the server
        if (!IsServer) return;

        // Optimize performance by not checking every single frame
        if (Time.time >= nextTargetUpdateTime)
        {
            nextTargetUpdateTime = Time.time + targetUpdateInterval;
            TargetClosestPlayer();
        }
    }

    void TargetClosestPlayer()
    {
        // Find all player objects in the session
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

        // Set the NavMeshAgent destination if a player is found
        if (closestPlayer != null)
        {
            agent.SetDestination(closestPlayer.transform.position);
        }
    }
}