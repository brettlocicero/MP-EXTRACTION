using UnityEngine;

[System.Serializable]
public class InstancedUIPair
{
    public RectTransform uiElement;
    public Transform worldTransform;

    public InstancedUIPair(RectTransform uiElement, Transform worldTransform)
    {
        this.uiElement = uiElement;
        this.worldTransform = worldTransform;
    }
}
