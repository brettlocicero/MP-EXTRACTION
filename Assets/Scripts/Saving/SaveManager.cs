using System;
using System.Text;
using Steamworks;
using UnityEngine;

public static class SaveManager
{
    const string FileName = "playerdata.json";

    public static PlayerSaveData Load()
    {
        if (!SteamClient.IsValid)
            return new PlayerSaveData(); // offline/editor fallback

        try
        {
            if (!SteamRemoteStorage.FileExists(FileName))
                return new PlayerSaveData();

            byte[] raw = SteamRemoteStorage.FileRead(FileName);
            return JsonUtility.FromJson<PlayerSaveData>(Encoding.UTF8.GetString(raw));
        }

        catch (Exception e)
        {
            Debug.LogError($"SaveManager: load failed - {e}");
            return new PlayerSaveData();
        }
    }

    public static void Save(PlayerSaveData data)
    {
        if (!SteamClient.IsValid)
            return;

        try
        {
            byte[] raw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            SteamRemoteStorage.FileWrite(FileName, raw);
        }

        catch (Exception e)
        {
            Debug.LogError($"SaveManager: save failed - {e}");
        }
    }
}