using Unity.Netcode;
using UnityEngine;

public class FloorShrine : MonoBehaviour, IInteractable
{
    bool used = false;

    public void Interact()
    {
        if (!used && NetworkManager.Singleton.IsHost)
        {
            RegionGenerator.Instance.GenerateNextFloor();
            used = true;
        }
    }
}
