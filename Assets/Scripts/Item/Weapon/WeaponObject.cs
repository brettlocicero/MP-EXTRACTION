using System.Collections;
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

    protected override void Start()
    {
        base.Start();
        weapon = item as WeaponSO;
        audioSource = GetComponent<AudioSource>();
        cinemachineShake = GetComponentInParent<CinemachineShake>();
        inventoryManager = InventoryManager.Instance;
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

    // List reasons why we cannot attack, and then invert the result - no reason to do it this way other than it makes the most sense to me.
    bool CanAttack()
    {
        return !(
            inAttack || 
            inventoryManager.IsInventoryOpen() || 
            playerController.IsSensitivityLocked() || 
            playerController.IsSprinting()
        );
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

        playerController.PlayAttackAnimation();

        attackAnimation.Rewind();
        attackAnimation.Play(attack.animationClip.name);

        comboIndex = ++comboIndex % weapon.attacks.Length;

        yield return new WaitForSeconds(attack.attackDelay);


        cinemachineShake.ShakeCamera(attack.camShakeIntensity, attack.camShakeDuration, 0.5f, 80f);
        PlayAttackAudio();
        
        // Build out the attack modularly
        if (attack.projectile) LaunchProjectile(attack);
        if (attack.useHitbox) TriggerAttackHitbox(attack);

        inAttack = false;
    }

    void LaunchProjectile(Attack attack)
    {
        Vector3 forwardVec = cameraTransform.forward;
        playerController.LaunchProjectileRpc(weapon.id, comboIndex, hitSpot.position, hitSpot.rotation, forwardVec);
    }

    void TriggerAttackHitbox(Attack attack) 
    {
        Collider[] hits = Physics.OverlapSphere(hitSpot.position, weapon.range, LayerMask.GetMask("Enemy"));
        if (hits.Length > 0) 
        {
            float hitstopDuration = 0.1f;
            audioSource.PlayOneShot(weapon.contactSound);
            StartCoroutine(HitstopWorker(attack.animationClip.name, hitstopDuration));

            foreach (Collider enemyCollider in hits) 
            {
                if (enemyCollider.TryGetComponent(out EnemyAI enemy)) 
                {
                    enemy.TakeDamage(attack.damage, attack.stunTime, attack.direction);
                }
            }
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
}