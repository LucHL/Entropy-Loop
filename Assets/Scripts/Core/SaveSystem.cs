using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int currentLevel;
    public int maxLevelReach;
    public int maxLevelFinish;
}

public static class SaveSystem
{
    private static string saveFilePath;

    public static void Save(bool reset = false)
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "GameData.json");
        SaveData saveData = new();

        saveData.currentLevel = GameManager.instance.currentLevelData.currentlevel;
        saveData.maxLevelReach = GameManager.instance.maxLevelReach;
        saveData.maxLevelFinish = GameManager.instance.maxLevelFinish;

        if (reset) {
            saveData.currentLevel = 1;
            saveData.maxLevelReach = 0;
            saveData.maxLevelFinish = 0;
            BugTracker.Info($"[SaveSystem] Game has been reset.");
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, json);

        if (reset)
            Load();

        BugTracker.Info($"[SaveSystem] Game saved ! File: {saveFilePath}");
    }

    public static void Load()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "GameData.json");

        if (!File.Exists(saveFilePath)) {
            BugTracker.Warning("[SaveSystem] Failed to load save file: no file found.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        GameManager.instance.currentLevelData.currentlevel = saveData.currentLevel;
        GameManager.instance.maxLevelReach = saveData.maxLevelReach;
        GameManager.instance.maxLevelFinish = saveData.maxLevelFinish;

        BugTracker.Info("[SaveSystem] Game successfully load.");
    }

    public static void ResetFile()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "GameData.json");
        File.Delete(saveFilePath);

        Save(true);
    }
}
