using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class RangedEnemyAttack : EnemyAttack
{
    [Header("Projectile")]
    [SerializeField] EnemyProjectile projectilePrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float projectileForce = 20f;
    [SerializeField] float damage = 10f;
    [SerializeField] float windupTime = 0.3f;

    EnemyAI enemyAI;
    Coroutine attackCoroutine;

    void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    public override void Execute(PlayerState target)
    {
        nextAttackTime = Time.time + cooldown;
        attackCoroutine = StartCoroutine(AttackRoutine(target));
    }

    public override void Cancel()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        IsAttacking = false;
    }

    IEnumerator AttackRoutine(PlayerState target)
    {
        IsAttacking = true;

        enemyAI.PlayAttackAnimation();

        yield return new WaitForSeconds(windupTime);

        if (target != null)
            FireProjectile(target);

        IsAttacking = false;
    }

    void FireProjectile(PlayerState target)
    {
        Vector3 direction = (target.transform.position - firePoint.position).normalized;

        EnemyProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        projectile.GetComponent<NetworkObject>().Spawn();
        projectile.GetComponent<Rigidbody>().AddForce(direction * projectileForce, ForceMode.Impulse);
        projectile.Init(damage);
    }
}