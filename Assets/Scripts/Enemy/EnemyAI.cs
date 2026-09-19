using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : NetworkBehaviour
{
    public System.Action<EnemyAI> OnEnemyKilled;

    [Header("Components")]
    [SerializeField] EnemyMovement movement;
    [SerializeField] EnemyAttack attack;

    [Header("Stats")]
    [SerializeField] float maxHealth = 100f;

    [Header("AI Settings")]
    [SerializeField] float targetUpdateInterval = 0.5f;

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

    readonly Dictionary<int, GameObject> debuffVFXInstances = new();
    readonly List<ActiveDebuff> activeDebuffs = new();

    int nextDebuffInstanceId = 0;

    ulong lastAttackerId;
    float nextTargetUpdateTime;

    bool isStunned = false;
    bool isDead = false;
    Coroutine stunCoroutine;

    NetworkVariable<float> currentHealth = new(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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
        activeDebuffs.Clear();
        debuffVFXInstances.Clear();
    }

    // --- Brain ---

    void Update()
    {
        if (!IsServer) return;

        UpdateDebuffs(Time.deltaTime);

        bool canThink = !isDead && !isStunned && !attack.IsAttacking;

        if (canThink && Time.time >= nextTargetUpdateTime)
        {
            nextTargetUpdateTime = Time.time + targetUpdateInterval;
            Think();
        }
    }

    void Think()
    {
        PlayerState target = FindClosestPlayer();

        if (target == null)
        {
            movement.Stop();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance <= attack.Range)
        {
            movement.Stop();

            if (attack.IsReady)
            {
                movement.Face(target.transform.position);
                attack.Execute(target);
            }
        }

        else
        {
            movement.MoveTo(target.transform.position);
        }
    }

    PlayerState FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        PlayerState closest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < shortestDistance && player.TryGetComponent(out PlayerState playerState))
            {
                shortestDistance = distance;
                closest = playerState;
            }
        }

        return closest;
    }

    // --- Debuffs ---

    public void AddDebuff(DebuffSO debuff)
    {
        if (isDead || debuff == null) return;

        if (IsServer)
        {
            ApplyDebuff(debuff, NetworkManager.Singleton.LocalClientId);
        }

        else
        {
            AddDebuffServerRpc(debuff.debuffId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void AddDebuffServerRpc(string debuffId, RpcParams rpcParams = default)
    {
        DebuffSO debuff = DebuffDatabase.Instance.GetDebuffSO(debuffId);

        if (debuff != null)
            ApplyDebuff(debuff, rpcParams.Receive.SenderClientId);
    }

    void ApplyDebuff(DebuffSO debuff, ulong sourceClientId)
    {
        if (!IsServer || isDead) return;

        ActiveDebuff activeDebuff = new(debuff, sourceClientId, nextDebuffInstanceId++);
        activeDebuffs.Add(activeDebuff);

        PlayDebuffVFXRpc(debuff.debuffId, activeDebuff.instanceId);
    }

    void UpdateDebuffs(float deltaTime)
    {
        for (int i = activeDebuffs.Count - 1; i >= 0; i--)
        {
            ActiveDebuff debuff = activeDebuffs[i];
            debuff.Tick(this, deltaTime);

            if (isDead)
                return;

            if (debuff.Expired && i < activeDebuffs.Count && activeDebuffs[i] == debuff)
            {
                StopDebuffVFXRpc(debuff.instanceId);
                activeDebuffs.RemoveAt(i);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    void PlayDebuffVFXRpc(string debuffId, int instanceId)
    {
        GameObject prefab = DebuffDatabase.Instance.GetDebuffVFX(debuffId);

        if (prefab == null)
            return;

        GameObject vfxInstance = Instantiate(prefab, transform);
        debuffVFXInstances[instanceId] = vfxInstance;
    }

    [Rpc(SendTo.Everyone)]
    void StopDebuffVFXRpc(int instanceId)
    {
        if (!debuffVFXInstances.TryGetValue(instanceId, out GameObject vfxInstance))
            return;

        if (vfxInstance != null)
            Destroy(vfxInstance);

        debuffVFXInstances.Remove(instanceId);
    }

    // --- Damage ---

    public void TakeDamage(float damage, float stunTime, AttackDirection attackDirection, Vector3 hitPoint = default)
    {
        if (hitPoint.Equals(Vector3.zero))
            hitPoint = transform.position;

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

    public void ApplyDebuffDamage(float damage, ulong sourceClientId)
    {
        ModifyHealth(damage, 0f, AttackDirection.None, transform.position, sourceClientId);
    }

    // --- Stun ---

    void TriggerStun(float duration)
    {
        attack.Cancel();
        movement.Stop();

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        yield return new WaitForSeconds(duration);

        isStunned = false;
    }

    // --- Animation ---

    public void PlayAttackAnimation()
    {
        PlayAttackAnimationRpc();
    }

    [Rpc(SendTo.Everyone)]
    void PlayAttackAnimationRpc()
    {
        animator.SetTrigger("Attack");
    }

    [Rpc(SendTo.Everyone)]
    void PlayHitAnimationRpc(AttackDirection attackDirection)
    {
        animator.ResetTrigger("Attack");

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

    // --- Death ---

    void Die()
    {
        attack.Cancel();
        movement.Stop();

        activeDebuffs.Clear();
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