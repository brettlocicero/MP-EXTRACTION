using Unity.Netcode;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] float interactionRange = 4f;
	[SerializeField] LayerMask interactionLayerMask;
	[SerializeField] Transform cam;

	void Start()
	{
		if (!NetworkManager.Singleton.IsClient)
		{
			enabled = false;
			return;
		}
	}

	void Update()
	{
		HandleInteract();
	}

    void FixedUpdate()
    {
        HandleHoverInteracts();
    }

    void HandleInteract()
	{
		if (InputManager.Actions.Player.Interact.WasPressedThisFrame())
		{
			if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, interactionRange, interactionLayerMask))
			{
				if (hit.collider.TryGetComponent(out IInteractable interactable))
				{
					interactable.Interact();
					Debug.Log($"Interacted with {hit.collider.gameObject.name}...");
				}
			}
		}
	}

	void HandleHoverInteracts()
	{
		if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, interactionRange, interactionLayerMask))
		{
			if (hit.collider.TryGetComponent(out IInteractable interactable))
			{
				interactable.Hover("Press 'E' to interact with " + hit.collider.gameObject.name);
			}
		}

		else
		{
			// PopupManager.instance.HidePopup();
		}
	}
}
