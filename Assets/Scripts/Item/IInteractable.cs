using UnityEngine;

public interface IInteractable
{
	public void Interact();

	public void Hover(string message)
	{
		InteractionText.Instance.ShowPopup(message);
	}

	public void ExitHover()
	{
		InteractionText.Instance.HidePopup();
	}
}
