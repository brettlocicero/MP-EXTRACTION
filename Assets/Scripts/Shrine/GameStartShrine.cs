using Unity.Netcode;
using UnityEngine;

public class GameStartShrine : MonoBehaviour, IInteractable
{
    bool used = false;

    void Start()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            enabled = false;
        }
    }

    public void Interact()
    {
        if (!used)
        {
            GameManager.Instance.StartGameSession();
            used = true;
        }
    }
}
