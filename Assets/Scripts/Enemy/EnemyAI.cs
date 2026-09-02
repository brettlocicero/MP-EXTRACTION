using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : NetworkBehaviour
{
    public System.Action<EnemyAI> OnEnemyKilled;

    Rigidbody rb;

    [Header("Stats")]
    [SerializeField] float maxHealth = 100f;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float rotationSpeed = 10f;

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

    [Header("Loot")]
    [SerializeField] LootDrop[] lootDrops;
    [SerializeField] GameObject soulsFlyVFXPrefab;
    [SerializeField] int soulsDropAmount = 10;

    ulong lastAttackerId;
    float nextTargetUpdateTime;

    bool isStunned = false;
    float nextAttackTime;
    bool isAttacking = false;
    bool isDead = false;
    Coroutine stunCoroutine;
    Coroutine attackCoroutine;

    bool isMoving = false;
    Vector3 moveTarget;

    NetworkVariable<float> currentHealth = new(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
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

    void FixedUpdate()
    {
        if (!IsServer) return;
        if (!isMoving) return;

        MoveTowards(moveTarget);
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
            isMoving = false;

            if (Time.time >= nextAttackTime)
            {
                attackCoroutine = StartCoroutine(AttackRoutine(closestPlayer.GetComponent<PlayerState>()));
            }
        }

        else
        {
            moveTarget = closestPlayer.transform.position;
            isMoving = true;
        }
    }

    void MoveTowards(Vector3 destination)
    {
        Vector3 direction = destination - rb.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        Vector3 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    IEnumerator AttackRoutine(PlayerState target)
    {
        isAttacking = true;
        isMoving = false;
        nextAttackTime = Time.time + attackCooldown;

        // Face the player once
        if (target != null)
        {
            Vector3 lookPos = target.transform.position - transform.position;
            lookPos.y = 0f;

            if (lookPos.sqrMagnitude > 0.001f)
                rb.MoveRotation(Quaternion.LookRotation(lookPos));
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
    }

    [Rpc(SendTo.Everyone)]
    void PlayAttackAnimationRpc()
    {
        animator.SetTrigger("Attack");
    }

    public void TakeDamage(float damage, float stunTime, AttackDirection attackDirection, Vector3 hitPoint = default)
    {
        if (IsServer)
        {
            ModifyHealth(damage, stunTime, attackDirection, hitPoint, NetworkManager.Singleton.LocalClientId);
        }

        else
        {
            TakeDamageServerRpc(damage, stunTime, attackDirection, hitPoint);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void TakeDamageServerRpc(float damage, float stunTime, AttackDirection attackDirection, Vector3 hitPoint, RpcParams rpcParams = default)
    {
        ModifyHealth(damage, stunTime, attackDirection, hitPoint, rpcParams.Receive.SenderClientId);
    }

    void ModifyHealth(float damage, float stunTime, AttackDirection attackDirection, Vector3 hitPoint, ulong attackerId)
    {
        if (!IsServer || isDead) return;

        lastAttackerId = attackerId;
        currentHealth.Value -= damage;

        ShowDamageNumberRpc(damage, hitPoint, RpcTarget.Single(attackerId, RpcTargetUse.Temp));

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            PlayDeathFXRpc();
            Die();
            return;
        }

        if (stunTime > 0f)
        {
            PlayHitAnimationRpc(attackDirection);
            TriggerStun(stunTime);
        }
    }
    
    [Rpc(SendTo.SpecifiedInParams)]
    void ShowDamageNumberRpc(float damage, Vector3 hitPoint, RpcParams rpcParams)
    {
        UIManager.Instance.DisplayDamageNumber(transform, hitPoint, damage);
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
        isMoving = false;

        yield return new WaitForSeconds(duration);

        isStunned = false;
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
                animator.SetTrigger("SmallHit");
                break;
        }
    }

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

    [Rpc(SendTo.Everyone)]
    void PlayDeathFXRpc()
    {
        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, 0.2f);
        }

        if (deathVFXPrefab != null)
        {
            GameObject deathFX = Instantiate(deathVFXPrefab, transform.position, transform.rotation);
            foreach (Rigidbody deathRb in deathFX.GetComponentsInChildren<Rigidbody>())
            {
                deathRb.AddForce(-transform.forward * 10f, ForceMode.Impulse);
            }

            Destroy(deathFX, 10f);
        }
    }

    void Die()
    {
        OnEnemyKilled?.Invoke(this);
        AwardSouls();
        SpawnLootDrops();
        GetComponent<NetworkObject>().Despawn();
    }

    void AwardSouls()
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerId, out NetworkClient client)) return;

        if (!client.PlayerObject.TryGetComponent<PlayerCurrency>(out PlayerCurrency currency)) return;

        currency.AddSouls(soulsDropAmount);
        SpawnSoulsFXRpc(lastAttackerId);
    }

    [Rpc(SendTo.Everyone)]
    void SpawnSoulsFXRpc(ulong killerClientId)
    {
        if (soulsFlyVFXPrefab == null) return;

        GameObject fx = Instantiate(soulsFlyVFXPrefab, transform.position, Quaternion.identity);
        fx.GetComponent<SoulsFlyVFX>().SetTarget(killerClientId);
    }

    void SpawnLootDrops()
    {
        if (!IsServer) return;

        foreach (LootDrop lootDrop in lootDrops)
        {
            if (lootDrop.RollDrop)
            {
                LootItem drop = Instantiate(lootDrop.LootItem, transform.position, Quaternion.identity);
                drop.GetComponent<NetworkObject>().Spawn();
                // drop.Init();
            }
        }
    }
}