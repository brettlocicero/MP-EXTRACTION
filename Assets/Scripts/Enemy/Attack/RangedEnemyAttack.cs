using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class RangedEnemyAttack : EnemyAttack
{
    [Header("Projectile")]
    [SerializeField] float attackDuration = 1.5f;
    [SerializeField] RangedAttack[] rangedAttacks;

    EnemyAI enemyAI;

    void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    public override void Execute(PlayerState target)
    {
        nextAttackTime = Time.time + cooldown;
        StartCoroutine(AttackRoutine(target));
    }

    public override void Cancel()
    {
        StopAllCoroutines();
        IsAttacking = false;
        nextAttackTime = Time.time + cooldown;
    }

    IEnumerator AttackRoutine(PlayerState target)
    {
        IsAttacking = true;

        enemyAI.PlayAttackAnimation();

        foreach (RangedAttack rangedAttack in rangedAttacks)
            StartCoroutine(FireAtTime(target, rangedAttack));

        yield return new WaitForSeconds(attackDuration);

        IsAttacking = false;
    }

    IEnumerator FireAtTime(PlayerState target, RangedAttack rangedAttack)
    {
        yield return new WaitForSeconds(rangedAttack.windupTime);

        if (target != null)
            FireProjectile(target, rangedAttack);
    }

    void FireProjectile(PlayerState target, RangedAttack rangedAttack)
    {
        Vector3 direction = (target.transform.position - rangedAttack.firePoint.position).normalized;

        EnemyProjectile projectile = Instantiate(rangedAttack.projectilePrefab, rangedAttack.firePoint.position, Quaternion.LookRotation(direction));
        projectile.GetComponent<NetworkObject>().Spawn();
        projectile.GetComponent<Rigidbody>().AddForce(direction * rangedAttack.projectileForce, ForceMode.Impulse);
        projectile.Init(rangedAttack.damage);
    }

    [System.Serializable]
    class RangedAttack
    {
        public EnemyProjectile projectilePrefab;
        public float projectileForce = 20f;
        public float damage = 10f;
        public float windupTime = 0.3f;
        public Transform firePoint;
    }
}