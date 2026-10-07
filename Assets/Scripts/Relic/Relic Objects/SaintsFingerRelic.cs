using UnityEngine;

[CreateAssetMenu(fileName = "SaintsFingerSO", menuName = "Scriptable Objects/Relics/SaintsFingerSO")]
public class SaintsFingerSO : RelicSO
{
    [SerializeField] int hitsRequired = 5;
    [SerializeField] float strikeDamage = 40f;
    [SerializeField] float strikeStunTime = 0.5f;

    int hitCount;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        hitCount++;

        if (hitCount >= hitsRequired)
        {
            hitCount = 0;
            
            foreach (EnemyAI target in weaponContext.HitEnemies) 
            {                
                target.TakeDamage(strikeDamage, strikeStunTime, AttackDirection.Middle);
                RelicVFXManager.Instance.PlayVFX(Id, target.transform.position);
            }
        }
    }
}