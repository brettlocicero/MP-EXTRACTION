using TMPro;
using UnityEngine;

public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance { get; private set; }

    [SerializeField] GameObject combatUIRoot;
    [SerializeField] TMP_Text killCountText;

    void Awake()
    {
        Instance = this;
        SetRegionActive(false);
        UpdateKillCount(0);
    }

    public void SetRegionActive(bool active)
    {
        combatUIRoot.SetActive(active);
    }

    public void UpdateKillCount(int count)
    {
        killCountText.text = $"Kills: {count}";
    }
}
