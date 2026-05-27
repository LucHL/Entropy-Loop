using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    public Sprite background;
    public Sprite characterLeft;
    public Sprite characterRight;
    public string speakerName;
    public string text;
}

[System.Serializable]
public class LevelData
{
    public int currentlevel;
    public string chaptersBeforeGame;
    public string chaptersAfterGame;
    public int nbrSublevel;
    public bool isBoss;
}

[System.Serializable]
public class LevelsWrapper
{
    public LevelData[] levels;
}
