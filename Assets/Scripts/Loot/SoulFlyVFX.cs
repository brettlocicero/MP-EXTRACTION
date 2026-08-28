using Unity.Netcode;
using UnityEngine;

public class SoulsFlyVFX : MonoBehaviour
{
    [SerializeField] float flySpeed = 6f;
    [SerializeField] float arriveDistance = 0.3f;

    NetworkObject targetPlayer;

    public void SetTarget(ulong clientId)
    {
        foreach (NetworkObject netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.IsPlayerObject && netObj.OwnerClientId == clientId)
            {
                targetPlayer = netObj;
                break;
            }
        }
    }

    void FixedUpdate()
    {
        if (targetPlayer == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, targetPlayer.transform.position, flySpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= arriveDistance)
            Destroy(gameObject);
    }
}