using Unity.Netcode;
using UnityEngine;

public abstract class RelicEffect : MonoBehaviour
{
    protected PlayerCombatEvents events;
    protected ulong ownerId;

    protected virtual void Awake()
    {
        events = GetComponentInParent<PlayerCombatEvents>();
        ownerId = GetComponentInParent<NetworkObject>().OwnerClientId;
    }
}