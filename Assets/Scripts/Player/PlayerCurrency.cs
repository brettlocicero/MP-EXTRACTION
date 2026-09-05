using TMPro;
using UnityEngine;

public class PlayerCurrency : MonoBehaviour
{
    public int Souls { get; private set; }
    TextMeshProUGUI soulsText;
    bool loaded;

    void Start()
    {
        soulsText = UIManager.Instance.SoulsText;
        Souls = SaveManager.Load().souls;
        loaded = true;
        UpdateSoulsTextUI();
    }

    public void AddSouls(int amount)
    {
        Souls += amount;
        UpdateSoulsTextUI();
        PersistCurrentSouls();
    }

    public bool SpendSouls(int amount)
    {
        if (amount < 0 || Souls < amount) return false;
        Souls -= amount;
        UpdateSoulsTextUI();
        PersistCurrentSouls();
        return true;
    }

    public void ClearSoulsOnRunLoss()
    {
        Souls = 0;
        UpdateSoulsTextUI();
        PersistCurrentSouls();
    }

    public void PersistCurrentSouls()
    {
        if (loaded) SaveManager.Save(new PlayerSaveData { souls = Souls });
    }

    void OnApplicationQuit() => PersistCurrentSouls();
    void OnDestroy() => PersistCurrentSouls();
    void UpdateSoulsTextUI()
    {
        if (soulsText) soulsText.text = Souls.ToString();
    }
}
