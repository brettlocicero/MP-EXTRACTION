using UnityEngine;

public class GameStartShrine : MonoBehaviour, IInteractable
{
    bool used = false;

    public void Interact()
    {
        if (!used)
        {
            GameManager.Instance.StartGameSession();
            used = true;
        }
    }
}
