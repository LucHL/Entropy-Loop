using UnityEngine;

public struct DialogueLine
{
    public Sprite background;
    public Sprite characterLeft;
    public Sprite characterRight;
    public string speakerName;
    public string text;
}

public class LevelData
{
    public DialogueLine[] lines;
    public int currentLevel;
    public bool hasStory = false;
    public string[] chapters;
}
