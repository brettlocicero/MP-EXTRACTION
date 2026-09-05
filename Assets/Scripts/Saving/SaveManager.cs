using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "playerdata.json");

    public static PlayerSaveData Load()
    {
        try
        {
            return File.Exists(SavePath)
                ? JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(SavePath)) ?? new PlayerSaveData()
                : new PlayerSaveData();
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: load failed - {e}");
            return new PlayerSaveData();
        }
    }

    public static void Save(PlayerSaveData data)
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data));
            if (File.Exists(SavePath)) File.Replace(temporaryPath, SavePath, null);
            else File.Move(temporaryPath, SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: save failed - {e}");
        }
    }
}
