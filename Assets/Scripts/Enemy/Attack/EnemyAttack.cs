using UnityEngine;

public abstract class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] protected float range = 2f;
    [SerializeField] protected float minRange = 0f;
    [SerializeField] protected float cooldown = 1f;
    [SerializeField] protected bool attacksWhileMoving = false;

    protected float nextAttackTime;

    public float Range => range;
    public float MinRange => minRange;
    public bool AttacksWhileMoving => attacksWhileMoving;
    public bool IsAttacking { get; protected set; }
    public bool IsReady => Time.time >= nextAttackTime;

    public abstract void Execute(PlayerState target);
    public abstract void Cancel();
}