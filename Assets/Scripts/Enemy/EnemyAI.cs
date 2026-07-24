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

    [Header("Attack Settings")]
    [SerializeField] float attackRange = 2f;
    [SerializeField] int attackDamage = 10;
    [SerializeField] float attackHitTime = 0.35f;
    [SerializeField] float attackDuration = 0.8f;
    [SerializeField] float attackCooldown = 1f;

    [Header("References")]
    [SerializeField] Animator animator;
    
    [Header("FX & Audio Settings")]
    [SerializeField] ParticleSystem[] hitVFXParticles;
    [SerializeField] AudioClip hitSFX;
    [SerializeField] GameObject deathVFXPrefab;
    [SerializeField] AudioClip deathSFX;
    [SerializeField] AudioSource audioSource;
    
    float nextTargetUpdateTime;
    
    bool isStunned = false;
    float nextAttackTime;
    bool isAttacking = false;
    Coroutine stunCoroutine;
    Coroutine attackCoroutine;

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

        if (isStunned || isAttacking)
            return;

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

        if (closestPlayer == null)
            return;

        float distance = Vector3.Distance(transform.position, closestPlayer.transform.position);

        if (distance <= attackRange)
        {
            if (Time.time >= nextAttackTime)
            {
                attackCoroutine = StartCoroutine(AttackRoutine(closestPlayer.GetComponent<PlayerState>()));
            }
        }

        else
        {
            agent.SetDestination(closestPlayer.transform.position);
        }
    }

    IEnumerator AttackRoutine(PlayerState target)
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // Face the player once
        if (target != null)
        {
            Vector3 lookPos = target.transform.position - transform.position;
            lookPos.y = 0f;

            if (lookPos.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookPos);
        }

        PlayAttackAnimationRpc();

        yield return new WaitForSeconds(attackHitTime);

        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance <= attackRange)
            {
                target.Damage(attackDamage);
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackHitTime));

        isAttacking = false;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    [Rpc(SendTo.Everyone)]
    void PlayAttackAnimationRpc()
    {
        animator.SetTrigger("Attack");
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
        if (newValue < previousValue && newValue > 0f)
        {
            if (audioSource != null && hitSFX != null)
            {
                audioSource.PlayOneShot(hitSFX);
            }

            foreach (ParticleSystem ps in hitVFXParticles)
            {
                ps.Play();
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
            GameObject deathFX = Instantiate(deathVFXPrefab, transform.position, transform.rotation);
            foreach (Rigidbody rb in deathFX.GetComponentsInChildren<Rigidbody>())
            {
                rb.AddForce(-transform.forward * 10f, ForceMode.Impulse);
            }

            Destroy(deathFX, 10f);
        }
    }

    void Die()
    {
        OnEnemyKilled?.Invoke(this);
        GetComponent<NetworkObject>().Despawn();
    }
}