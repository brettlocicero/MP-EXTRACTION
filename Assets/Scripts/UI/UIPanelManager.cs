using System.Collections.Generic;
using UnityEngine;

public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager Instance;

    HashSet<Object> openPanels = new();
    HashSet<Object> movementLockingPanels = new();
    HashSet<Object> sensitivityLockingPanels = new();

    void Awake()
    {
        Instance = this;
    }

    public void PanelOpened(Object panel, bool lockMovement = false, bool lockSensitivity = false)
    {
        bool wasEmpty = openPanels.Count == 0;
        openPanels.Add(panel);

        if (wasEmpty)
            CursorManager.UnlockCursor();

        if (lockMovement)
        {
            bool wasUnlocked = movementLockingPanels.Count == 0;
            movementLockingPanels.Add(panel);

            if (wasUnlocked)
                GameManager.Instance.LocalPlayer.LockMovement();
        }

        if (lockSensitivity)
        {
            bool wasUnlocked = sensitivityLockingPanels.Count == 0;
            sensitivityLockingPanels.Add(panel);

            if (wasUnlocked)
                GameManager.Instance.LocalPlayer.LockSensitivity();
        }
    }

    public void PanelClosed(Object panel)
    {
        openPanels.Remove(panel);

        if (openPanels.Count == 0)
            CursorManager.LockCursor();

        movementLockingPanels.Remove(panel);

        if (movementLockingPanels.Count == 0)
            GameManager.Instance.LocalPlayer.UnlockMovement();

        sensitivityLockingPanels.Remove(panel);

        if (sensitivityLockingPanels.Count == 0)
            GameManager.Instance.LocalPlayer.UnlockSensitivity();
    }
}