using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class WeaponObject : ItemObject
{
    [Header("Weapon References")]
    [SerializeField] Animation attackAnimation;
    [SerializeField] Transform hitSpot;

    bool inAttack = false;

    WeaponSO weapon;
    float attackTimer = 0f;
    int comboIndex = 0;

    AudioSource audioSource;
    CinemachineShake cinemachineShake;

    InventoryManager inventoryManager;
    PlayerRelics relics;

    float chargeTime = 0f;


    protected override void Start()
    {
        base.Start();
        weapon = item as WeaponSO;
        audioSource = GetComponent<AudioSource>();
        cinemachineShake = GetComponentInParent<CinemachineShake>();
        inventoryManager = InventoryManager.Instance;
        relics = GetComponentInParent<PlayerRelics>();
    }

    void OnEnable()
    {
        attackAnimation.Rewind();
        inAttack = false;
    }

    protected override void Update()
    {
        base.Update();
        HandleAttack();
    }

    // List reasons why we cannot attack, and then invert the result.
    // No reason to do it this way, other than it makes the most sense to me.
    bool CanAttack()
    {
        return !(
            inAttack || 
            inventoryManager.IsInventoryOpen() || 
            playerController.IsSensitivityLocked() || 
            playerController.IsSprinting()
        );
    }

    WeaponContext CreateWeaponContext(Attack attack)
    {
        WeaponContext weaponContext = new WeaponContext
        {
            SourceClientId = NetworkManager.Singleton.LocalClientId,
            Damage = attack.damage,
            StunTime = attack.stunTime
        };
        

        return weaponContext;
    }

    void HandleAttack()
    {
        attackTimer += Time.deltaTime;

        if (!CanAttack()) return;

        bool pressedAttack = InputManager.Actions.Player.Attack.WasPressedThisFrame();
        if (pressedAttack && attackTimer >= weapon.attackRate)
        {
            StartCoroutine(AttackWorker(weapon.attacks[comboIndex]));
            attackTimer = 0f;
        }
    }

    IEnumerator AttackWorker(Attack attack)
    {
        inAttack = true;
        WeaponContext weaponContext = CreateWeaponContext(attack);

        // Prepare VFX
        attackAnimation.Rewind();
        attackAnimation.Play(attack.animationClip.name);
        playerController.PlayAttackAnimation();

        comboIndex = ++comboIndex % weapon.attacks.Length;

        // Wind-up, slowed or held while the attack button is down
        yield return WindupWorker(attack);

        float chargePercent = attack.maxChargeTime > 0f ? chargeTime / attack.maxChargeTime : 0f;
        weaponContext.Damage = attack.damage * Mathf.Lerp(1f, attack.chargeDamageMultiplier, chargePercent);

        TriggerSoulShards(WeaponEvent.OnAttack, weaponContext);
        relics.NotifyAttackServerRpc();

        cinemachineShake.ShakeCamera(attack.camShakeIntensity, attack.camShakeDuration, 0.5f, 80f);
        PlayAttackAudio();

        if (attack.projectile) LaunchProjectile(attack, weaponContext);
        if (attack.useHitbox) TriggerAttackHitbox(attack, weaponContext);

        inAttack = false;
    }

    IEnumerator WindupWorker(Attack attack)
    {
        AnimationState state = attackAnimation[attack.animationClip.name];
        bool charging = attack.maxChargeTime > 0f;
        chargeTime = 0f;

        while (charging || state.time < attack.attackDelay)
        {
            charging = charging && chargeTime < attack.maxChargeTime && InputManager.Actions.Player.Attack.IsPressed();

            if (charging)
            {
                bool atApex = state.time >= attack.attackDelay;
                state.speed = atApex ? 0f : attack.chargeSpeed;
                chargeTime += Time.deltaTime;
            }

            else
            {
                state.speed = 1f;
            }

            yield return null;
        }

        state.speed = 1f;
    }

    void LaunchProjectile(Attack attack, WeaponContext weaponContext)
    {
        Vector3 forwardVec = cameraTransform.forward;
        playerController.LaunchProjectileRpc(weapon.id, comboIndex, hitSpot.position, hitSpot.rotation, forwardVec);
    }

    void TriggerAttackHitbox(Attack attack, WeaponContext weaponContext) 
    {
        Collider[] hits = Physics.OverlapSphere(hitSpot.position, weapon.range, LayerMask.GetMask("Enemy"));
        HashSet<EnemyAI> hitEnemies = new();
        if (hits.Length > 0) 
        {
            float hitstopDuration = 0.1f;
            audioSource.PlayOneShot(weapon.contactSound);
            StartCoroutine(HitstopWorker(attack.animationClip.name, hitstopDuration));

            foreach (Collider enemyCollider in hits) 
            {
                if (enemyCollider.TryGetComponent(out EnemyAI enemy)) 
                {
                    Vector3 hitPoint = enemyCollider.ClosestPoint(hitSpot.position);
                    enemy.TakeDamage(weaponContext.Damage, weaponContext.StunTime, attack.direction, hitPoint);
                    hitEnemies.Add(enemy);
                }
            }

            weaponContext.HitEnemies = hitEnemies.ToArray();
            TriggerSoulShards(WeaponEvent.OnHit, weaponContext);
        }
    }

    IEnumerator HitstopWorker(string clipName, float duration)
    {
        AnimationState state = attackAnimation[clipName];
        state.speed = 0f;

        yield return new WaitForSeconds(duration);

        state.speed = 1f;
    }

    void PlayAttackAudio()
    {
        audioSource.pitch = Random.Range(weapon.attackSoundPitchRange.x, weapon.attackSoundPitchRange.y);
        audioSource.PlayOneShot(weapon.attackSound);
    }

    void TriggerSoulShards(WeaponEvent weaponEvent, WeaponContext weaponContext)
    {
        foreach (SoulShardSO soulShardSO in Instance.soulShards)
        {
            soulShardSO.Trigger(weaponEvent, weaponContext);
        }
    }
}
