using TMPro;
using UnityEngine;

public class InteractionText : MonoBehaviour
{
    public static InteractionText Instance;

    [SerializeField] TextMeshProUGUI interactionText;
    [SerializeField] GameObject interactionUiObject;

    void Awake()
    {
        Instance = this;
    }

    public void ShowPopup(string message)
    {
        interactionText.text = message;
        interactionUiObject.SetActive(true);
    }

    public void HidePopup()
    {
        interactionUiObject.SetActive(false);
    }
}
