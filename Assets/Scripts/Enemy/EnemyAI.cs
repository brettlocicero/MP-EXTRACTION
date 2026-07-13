using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : NetworkBehaviour
{
    public System.Action<EnemyAI> OnEnemyKilled;

    NavMeshAgent agent;
    
    [Header("Stats")]
    [SerializeField] float maxHealth = 100f;

    [Header("AI Settings")]
    [SerializeField] float targetUpdateInterval = 0.5f;

    [Header("References")]
    [SerializeField] Animator animator;
    
    [Header("FX & Audio Settings")]
    [SerializeField] GameObject hitVFXPrefab;
    [SerializeField] AudioClip hitSFX;
    [SerializeField] GameObject deathVFXPrefab;
    [SerializeField] AudioClip deathSFX;
    [SerializeField] AudioSource audioSource;
    
    float nextTargetUpdateTime;
    
    // Server-side state tracking for the stun mechanic
    bool isStunned = false;
    Coroutine stunCoroutine;

    // NetworkVariable syncs from Server to Clients automatically.
    NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Fallback in case AudioSource isn't assigned via inspector
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            agent.enabled = false;
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    void Update()
    {
        if (!IsServer) return;
        if (isStunned) return; 

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

    public void TakeDamage(float damage, float stunTime, AttackDirection attackDirection)
    {
        if (IsServer)
        {
            ModifyHealth(damage, stunTime, attackDirection);
        }
        else
        {
            TakeDamageServerRpc(damage, stunTime, attackDirection);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void TakeDamageServerRpc(float damage, float stunTime, AttackDirection attackDirection)
    {
        ModifyHealth(damage, stunTime, attackDirection);
    }

    void ModifyHealth(float damage, float stunTime, AttackDirection attackDirection)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            // Call client RPC for death visuals right before despawning the object
            PlayDeathFXRpc(); 
            Die();
            return;
        }

        PlayHitAnimationRpc(attackDirection);
        TriggerStun(stunTime);
    }

    void TriggerStun(float customStunDuration)
    {
        if (!IsServer) return;
        if (customStunDuration <= 0f) return;

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunRoutine(customStunDuration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    [Rpc(SendTo.Everyone)]
    void PlayHitAnimationRpc(AttackDirection attackDirection)
    {
        switch (attackDirection)
        {
            case AttackDirection.Left:
                animator.SetTrigger("HitLeft");
                break;
            case AttackDirection.Right:
                animator.SetTrigger("HitRight");
                break;
            default:
                animator.SetTrigger("HitLeft");
                break;
        }
    }

    // Automatically triggers on all clients whenever the NetworkVariable updates
    void OnHealthChanged(float previousValue, float newValue)
    {
        // Only trigger "Hit" FX if the enemy is taking damage, not dying or healing
        if (newValue < previousValue && newValue > 0f)
        {
            // Play Hit Audio
            if (audioSource != null && hitSFX != null)
            {
                audioSource.PlayOneShot(hitSFX);
            }

            // Spawn Hit Particles
            if (hitVFXPrefab != null)
            {
                Instantiate(hitVFXPrefab, transform.position + Vector3.up, Quaternion.identity);
            }
        }
    }

    // Client RPC to handle Death FX simultaneously across all screens
    [Rpc(SendTo.Everyone)]
    void PlayDeathFXRpc()
    {
        // Play death audio at the enemy's location. 
        // AudioSource.PlayClipAtPoint ensures the audio keeps playing even if this GameObject is immediately destroyed.
        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, 0.2f);
        }

        // Spawn Death Particles
        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, transform.rotation);
        }
    }

    void Die()
    {
        OnEnemyKilled?.Invoke(this);
        GetComponent<NetworkObject>().Despawn();
    }
}