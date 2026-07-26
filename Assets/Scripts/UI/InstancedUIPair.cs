using UnityEngine;

[System.Serializable]
public class InstancedUIPair
{
    public RectTransform uiElement;
    public Transform worldTransform;
    public PlayerState player;

    public InstancedUIPair(RectTransform uiElement, Transform worldTransform, PlayerState player)
    {
        this.uiElement = uiElement;
        this.worldTransform = worldTransform;
        this.player = player;
    }

    public void Destroy()
    {
        Object.Destroy(uiElement.gameObject);
    }
}
