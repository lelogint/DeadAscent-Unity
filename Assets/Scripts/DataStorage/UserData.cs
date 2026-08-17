using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


[Serializable]
public class GameData
{
    public int money;
    public List<string> keys = new List<string>();
    public List<bool> values = new List<bool>();
    public void FromDictionary(Dictionary<string, bool> dict) // Convert dictionary to lists because it can't be stored as a dictionary :C
    {
        keys.Clear();
        values.Clear();

        foreach (var kvp in dict)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public Dictionary<string, bool> ToDictionary() // Rebuild dictionary
    {
        var dict = new Dictionary<string, bool>();
        for (int i = 0; i < keys.Count; i++)
            dict[keys[i]] = values[i];
        return dict;
    }
}

public class GameSettings
{
    public int resIndex; // resolution index from array in ui script
    public int fpsCapIndex;
    public bool fullscreen;
}
public static class UserData
{
    private static string filePath = Application.persistentDataPath + "/userData.json"; // We want file stored inn persistent data with the file name userData.json
    private static string statsPath = Application.persistentDataPath + "/userSettings.json";
    public static void SaveUserData(int money, Dictionary<string, bool> itemDictionary)
    {
        GameData saveData = new GameData();
        saveData.money = money;
        saveData.FromDictionary(itemDictionary);


        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(filePath, json); // Save data as json!
    }
    public static (int, Dictionary<string, bool>, bool) LoadData()
    {
        if (!File.Exists(filePath))
        {
            Debug.Log("No save file found!");
            return (0, new Dictionary<string, bool>(), false); // The 0 int is for the money, then player item dictionary, then a bool representing if properly retrieved the data
        }

        string json = File.ReadAllText(filePath); // Read stored ddata
        GameData data = JsonUtility.FromJson<GameData>(json);
        return (data.money, data.ToDictionary(), true); // Properly returned data from file
    }

    public static void SaveUserSettings(int resIndex, int fpsCapIndex, bool fullscreen)
    {
        GameSettings saveSettings = new GameSettings();
        saveSettings.resIndex = resIndex;
        saveSettings.fpsCapIndex = fpsCapIndex;
        saveSettings.fullscreen = fullscreen;
        string json = JsonUtility.ToJson(saveSettings, true);
        File.WriteAllText(statsPath, json); // Save data as json!
    }
    public static (int, int, bool, bool) LoadSettings()
    {
        if (!File.Exists(statsPath))
        {
            Debug.Log("No settings file found!");
            return (0, 0, false, false); // The 0 int is for the money, then player item dictionary, then a bool representing if properly retrieved the data
        }

        string json = File.ReadAllText(statsPath); // Read stored ddata
        GameSettings data = JsonUtility.FromJson<GameSettings>(json);
        return (data.resIndex, data.fpsCapIndex, data.fullscreen, true); // Properly returned data from file
    }
    public static void DeleteSave() // Unused but I kept it because "future updates" may require it anyways
    {
        if (File.Exists(filePath)){
            File.Delete(filePath);
        }
    }
}
