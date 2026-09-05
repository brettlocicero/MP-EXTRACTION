using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
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
    [SerializeField] Transform attackHitSpot;
    [SerializeField] float attackRange = 2f;
    [SerializeField] int attackDamage = 10;
    [SerializeField] float attackHitTime = 0.35f;
    [SerializeField] float attackDuration = 0.8f;
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] LayerMask playerLayerMask;

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

    float nextTargetUpdateTime;

    bool isStunned = false;
    float nextAttackTime;
    bool isAttacking = false;
    bool isDead = false;
    Coroutine stunCoroutine;
    Coroutine attackCoroutine;

    bool isMoving = false;
    Vector3 moveTarget;

    float currentHealthValue = 100f;
    float currentHealth
    {
        get => currentHealthValue;
        set
        {
            if (currentHealthValue == value) return;
            float previous = currentHealthValue;
            currentHealthValue = value;
            OnHealthChanged(previous, value);
        }
    }

    void Awake()
    {
        currentHealthValue = maxHealth;
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnDestroy()
    {
        activeDebuffs.Clear();
        debuffVFXInstances.Clear();
    }

    public void AddDebuff(DebuffSO debuff)
    {
        if (isDead || debuff == null) return;

        ApplyDebuff(debuff);
    }

    void ApplyDebuff(DebuffSO debuff)
    {
        if (isDead) return;

        ActiveDebuff activeDebuff = new(debuff, nextDebuffInstanceId++);
        activeDebuffs.Add(activeDebuff);

        PlayDebuffVFX(debuff.debuffId, activeDebuff.instanceId);
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
                StopDebuffVFX(debuff.instanceId);
                activeDebuffs.RemoveAt(i);
            }
        }
    }

    void PlayDebuffVFX(string debuffId, int instanceId)
    {
        GameObject prefab = DebuffDatabase.Instance.GetDebuffVFX(debuffId);

        if (prefab == null)
            return;

        GameObject vfxInstance = Instantiate(prefab, transform);
        debuffVFXInstances[instanceId] = vfxInstance;
    }

    void StopDebuffVFX(int instanceId)
    {
        if (!debuffVFXInstances.TryGetValue(instanceId, out GameObject vfxInstance))
            return;

        if (vfxInstance != null)
            Destroy(vfxInstance);

        debuffVFXInstances.Remove(instanceId);
    }

    void Update()
    {
        UpdateDebuffs(Time.deltaTime);

        if (isDead || isStunned || isAttacking) return;

        if (Time.time >= nextTargetUpdateTime)
        {
            nextTargetUpdateTime = Time.time + targetUpdateInterval;
            TargetPlayer();
        }
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        MoveTowards(moveTarget);
    }

    void TargetPlayer()
    {
        PlayerController player = GameManager.Instance.LocalPlayer;
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= attackRange)
        {
            isMoving = false;

            if (Time.time >= nextAttackTime)
            {
                attackCoroutine = StartCoroutine(AttackRoutine(player.GetComponent<PlayerState>()));
            }
        }

        else
        {
            moveTarget = player.transform.position;
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

        FaceTarget(target);
        PlayAttackAnimation();

        yield return new WaitForSeconds(attackHitTime);

        TriggerAttackHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackHitTime));

        isAttacking = false;
    }

    void FaceTarget(PlayerState target)
    {
        if (target == null)
            return;

        Vector3 lookPos = target.transform.position - transform.position;
        lookPos.y = 0f;

        if (lookPos.sqrMagnitude > 0.001f)
            rb.MoveRotation(Quaternion.LookRotation(lookPos));
    }

    void TriggerAttackHitbox()
    {
        Collider[] hits = Physics.OverlapSphere(attackHitSpot.position, attackRange, playerLayerMask);

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out PlayerState player))
                player.Damage(attackDamage);
        }
    }

    void PlayAttackAnimation()
    {
        animator.SetTrigger("Attack");
    }

    public void TakeDamage(float damage, float stunTime, AttackDirection attackDirection, Vector3 hitPoint = default)
    {
        if (hitPoint.Equals(Vector3.zero))
            hitPoint = transform.position;

        ModifyHealth(damage, stunTime, attackDirection, hitPoint);
    }

    void ModifyHealth(float damage, float stunTime, AttackDirection attackDirection, Vector3 hitPoint)
    {
        if (isDead) return;

        currentHealth -= damage;

        ShowDamageNumber(damage, hitPoint);

        if (currentHealth <= 0)
        {
            isDead = true;
            PlayDeathFX();
            Die();
            return;
        }

        if (stunTime > 0f)
        {
            PlayHitAnimation(attackDirection);
            TriggerStun(stunTime);
        }
    }
    
    void ShowDamageNumber(float damage, Vector3 hitPoint)
    {
        UIManager.Instance.DisplayDamageNumber(transform, hitPoint, damage);
    }

    public void ApplyDebuffDamage(float damage)
    {
        ModifyHealth(damage, 0f, AttackDirection.None, transform.position);
    }
    
    void TriggerStun(float customStunDuration)
    {
        if (customStunDuration <= 0f) return;

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
            isAttacking = false;
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

    void PlayHitAnimation(AttackDirection attackDirection)
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

    void PlayDeathFX()
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
        activeDebuffs.Clear();
        OnEnemyKilled?.Invoke(this);
        AwardSouls();
        SpawnLootDrops();
        Destroy(gameObject);
    }

    void AwardSouls()
    {
        PlayerController player = GameManager.Instance.LocalPlayer;
        if (player == null) return;

        if (!player.TryGetComponent<PlayerCurrency>(out PlayerCurrency currency)) return;

        currency.AddSouls(soulsDropAmount);
        SpawnSoulsFX();
    }

    void SpawnSoulsFX()
    {
        if (soulsFlyVFXPrefab == null) return;

        GameObject fx = Instantiate(soulsFlyVFXPrefab, transform.position, Quaternion.identity);
        fx.GetComponent<SoulsFlyVFX>().SetTarget(GameManager.Instance.LocalPlayer.transform);
    }

    void SpawnLootDrops()
    {
        foreach (LootDrop lootDrop in lootDrops)
        {
            if (lootDrop.RollDrop)
            {
                LootItem drop = Instantiate(lootDrop.LootItem, transform.position, Quaternion.identity);

                // drop.Init();
            }
        }
    }
}
