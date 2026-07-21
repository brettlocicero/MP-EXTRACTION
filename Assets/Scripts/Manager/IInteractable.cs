using UnityEngine;

public interface IInteractable
{
	public void Interact();

	public void Hover(string text)
	{
		// PopupManager.instance.DisplayPopup(text);
	}

	public void ExitHover()
	{
		// PopupManager.instance.HidePopup();
	}
}
