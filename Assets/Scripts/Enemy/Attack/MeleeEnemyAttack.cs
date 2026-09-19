using System.Collections;
using UnityEngine;

public class MeleeEnemyAttack : EnemyAttack
{
    [Header("Melee")]
    [SerializeField] EnemyAI enemy;
    [SerializeField] Transform hitSpot;
    [SerializeField] LayerMask playerLayerMask;
    [SerializeField] int damage = 10;
    [SerializeField] float hitTime = 0.35f;
    [SerializeField] float duration = 0.8f;

    Coroutine attackRoutine;

    public override void Execute(PlayerState target)
    {
        IsAttacking = true;
        nextAttackTime = Time.time + cooldown;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    public override void Cancel()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        IsAttacking = false;
    }

    IEnumerator AttackRoutine()
    {
        enemy.PlayAttackAnimation();

        yield return new WaitForSeconds(hitTime);

        TriggerHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, duration - hitTime));

        attackRoutine = null;
        IsAttacking = false;
    }

    void TriggerHitbox()
    {
        Collider[] hits = Physics.OverlapSphere(hitSpot.position, range, playerLayerMask);

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out PlayerState player))
                player.Damage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (hitSpot == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hitSpot.position, range);
    }
}