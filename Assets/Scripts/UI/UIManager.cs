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
    [SerializeField] TextMeshProUGUI soulsText;
    [SerializeField] GameObject crosshair;
    
    [Header("Damage Numbers")]
    [SerializeField] DamageNumberUI damageNumberPrefab;
    [SerializeField] float damageNumberJiggle = 20f;

    [Header("Objects")]
    [SerializeField] TextMeshProUGUI playerNametagPrefab;

    readonly List<InstancedUIPair> instancedUI = new();

    public static UIManager Instance { get; private set; }
    public TextMeshProUGUI SoulsText => soulsText;

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

        HandleCrosshairVisibility();
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

        AttachUIElement(playerTextObj.rectTransform, playerState.GetNameplateTransform(), playerState);

        void HandleNameChanged(FixedString64Bytes _, FixedString64Bytes newName)
        {
            if (playerTextObj != null)
                playerTextObj.text = newName.ToString();
        }
    }

    public void DeletePlayerNameplate(PlayerState playerState)
    {
        if (playerState.IsOwner)
            return;

        foreach (InstancedUIPair pair in instancedUI)
        {
            if (pair.player == playerState)
            {
                pair.Destroy();
                instancedUI.Remove(pair);
                break;
            }
        }
    }

    void AttachUIElement(RectTransform uiElement, Transform worldTransform, PlayerState player)
    {
        instancedUI.Add(new InstancedUIPair(uiElement, worldTransform, player));
    }

    public void UpdateHealthBar(int health, int maxHealth)
    {
        healthBar.localScale = new Vector3((float)health / maxHealth, 1f, 1f);
        healthText.text = $"{health} / {maxHealth}";
    }

    void HandleCrosshairVisibility()
    {
        if (GameManager.Instance.LocalPlayer == null) return;
        
        crosshair.SetActive(!GameManager.Instance.LocalPlayer.IsSprinting());
    }
    
    public void DisplayDamageNumber(Transform target, Vector3 hitPoint, float damage)
    {
        if (!GameManager.Instance.LocalPlayer)
            return;

        Camera cam = GameManager.Instance.LocalPlayer.camera;
        Vector3 screenPos = cam.WorldToScreenPoint(hitPoint);

        if (screenPos.z < 0)
            return;

        DamageNumberUI damageNumber = Instantiate(damageNumberPrefab, instancedUIRoot);
        damageNumber.Display(damage, target, cam, hitPoint);
    }
}