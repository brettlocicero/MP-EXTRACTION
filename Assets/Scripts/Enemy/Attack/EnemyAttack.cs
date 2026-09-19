using UnityEngine;

public abstract class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] protected float range = 2f;
    [SerializeField] protected float cooldown = 1f;

    protected float nextAttackTime;

    public float Range => range;
    public bool IsAttacking { get; protected set; }
    public bool IsReady => Time.time >= nextAttackTime;

    public abstract void Execute(PlayerState target);
    public abstract void Cancel();
}