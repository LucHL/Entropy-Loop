using System.Data;
using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    public string backgroundName;
    public string charLeftImg;
    public string charRightImg;
    public string speakerName;
    public string text;
}

[System.Serializable]
public class DialoguesWrapper
{
    public DialogueLine[] dialogues;
}


[System.Serializable]
public class LevelData
{
    public int currentlevel;
    public string chaptersBeforeGame;
    public string chaptersAfterGame;
    public int nbrSublevel;
    public bool isBoss;

    // Implement later

    // public MapType mapType;
    // public DeckData enemyDeck;
}

[System.Serializable]
public class LevelsWrapper
{
    public LevelData[] levels;
}
