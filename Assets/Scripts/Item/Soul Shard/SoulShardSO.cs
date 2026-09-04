using System;
using UnityEngine;

public abstract class SoulShardSO : ItemSO
{
    [Header("Shard Settings")]
    [SerializeField] WeaponEvent weaponEvent;
    [SerializeField] protected GameObject vfx;

    public void Trigger(WeaponEvent weaponEvent, WeaponContext weaponContext)
    {
        if (this.weaponEvent.Equals(weaponEvent))
        {
            ApplyEffect(weaponContext);
            Debug.Log("Triggering effect");
        }
    }

    protected virtual void ApplyEffect(WeaponContext weaponContext)
    {
        throw new NotImplementedException();
    }
}
