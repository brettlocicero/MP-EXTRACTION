using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SoulsPickup : NetworkBehaviour
{
    int soulsValue;
    bool collected = false;

    public void Init(int amount) => soulsValue = amount;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer || collected) return;

        if (other.TryGetComponent<PlayerCurrency>(out var currency))
        {
            collected = true;
            currency.AddSouls(soulsValue);
            GetComponent<NetworkObject>().Despawn();
        }
    }
}