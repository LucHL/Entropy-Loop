using UnityEngine;

[System.Serializable]
public class GameData
{
    public int maxLevelReach;
    public int maxLevelFinish;
    public string selectedDeckName;

    public GameData()
    {
        maxLevelReach = 0;
        maxLevelFinish = 0;
        selectedDeckName = "";
    }
}
