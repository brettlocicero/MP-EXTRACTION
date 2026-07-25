using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform instancedUIRoot;
    [SerializeField] Transform healthBar;
    [SerializeField] TextMeshProUGUI healthText;

    [Header("Objects")]
    [SerializeField] TextMeshProUGUI playerNametagPrefab;

    readonly List<InstancedUIPair> instancedUI = new();

    public static UIManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        foreach (InstancedUIPair pair in instancedUI)
        {
            if (pair.uiElement != null)
                Destroy(pair.uiElement.gameObject);
        }

        instancedUI.Clear();

        if (Instance == this)
            Instance = null;
    }

    void LateUpdate()
    {
        if (!GameManager.Instance.LocalPlayer)
            return;

        Camera cam = GameManager.Instance.LocalPlayer.camera;

        foreach (InstancedUIPair pair in instancedUI)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(pair.worldTransform.position);

            if (screenPos.z < 0)
            {
                pair.uiElement.gameObject.SetActive(false);
                continue;
            }

            pair.uiElement.gameObject.SetActive(true);
            pair.uiElement.position = screenPos;
        }
    }

    public void SpawnPlayerNameplate(PlayerState playerState)
    {
        if (playerState.IsOwner)
            return;

        if (instancedUI.Any(x => x.worldTransform == playerState.transform))
            return;

        TextMeshProUGUI playerTextObj = Instantiate(playerNametagPrefab, instancedUIRoot);
        playerTextObj.text = playerState.PlayerName.Value.ToString();

        playerState.PlayerName.OnValueChanged += HandleNameChanged;

        AttachUIElement(playerTextObj.rectTransform, playerState.GetNameplateTransform());

        void HandleNameChanged(FixedString64Bytes _, FixedString64Bytes newName)
        {
            if (playerTextObj != null)
                playerTextObj.text = newName.ToString();
        }
    }

    void AttachUIElement(RectTransform uiElement, Transform worldTransform)
    {
        instancedUI.Add(new InstancedUIPair(uiElement, worldTransform));
    }

    public void UpdateHealthBar(int health, int maxHealth)
    {
        healthBar.localScale = new Vector3((float)health / maxHealth, 1f, 1f);
        healthText.text = $"{health} / {maxHealth}";
    }
}