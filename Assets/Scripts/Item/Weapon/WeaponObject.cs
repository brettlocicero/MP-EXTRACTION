using System.Collections;
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

    protected override void Start()
    {
        base.Start();
        weapon = item as WeaponSO;
    }

    protected override void Update()
    {
        base.Update();
        HandleAttack();
    }

    void HandleAttack()
    {
        attackTimer += Time.deltaTime;

        if (inAttack) return;

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

        attackAnimation.Rewind();
        attackAnimation.Play(attack.animationClip.name);

        comboIndex = ++comboIndex % weapon.attacks.Length;

        yield return new WaitForSeconds(attack.attackDelay);

        TriggerAttackHitbox(attack);

        inAttack = false;
    }
    
    void TriggerAttackHitbox(Attack attack) 
    {
        Collider[] hits = Physics.OverlapSphere(hitSpot.position, weapon.range, LayerMask.NameToLayer("Enemy"));
        if (hits.Length > 0) 
        {
            foreach (Collider enemyCollider in hits) 
            {
                if (enemyCollider.TryGetComponent(out EnemyAI enemy)) 
                {
                    enemy.TakeDamage(attack.damage);
                }
            }
        }
    }
}