using UnityEngine;

public class DebuffDatabase : MonoBehaviour
{
    public static DebuffDatabase Instance;

    [SerializeField] DebuffSO[] debuffs;

    void Awake()
    {
        Instance = this;
    }

    public DebuffSO GetDebuffSO(string debuffId)
    {
        foreach (DebuffSO debuff in debuffs)
        {
            if (debuff.debuffId.Equals(debuffId))
            {
                return debuff;
            }
        }

        Debug.LogError($"Debuff ID {debuffId} not found in the database!");
        return null;
    }

    public GameObject GetDebuffVFX(string debuffId)
    {
        return GetDebuffSO(debuffId).DebuffVFX;
    }
}