using UnityEngine;

[System.Serializable]
public class Attack
{
    public string attackName;
    public AnimationClip animationClip;
    public AttackDirection direction;
    public float attackDelay = 0.2f;
    public float damage = 10f;
    public float stunTime = 1f;
}
