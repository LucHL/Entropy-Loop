using System;

[Serializable]
public struct DialogueLine
{
    public string backgroundName;
    public string charLeftImg;
    public string charRightImg;
    public string speakerName;
    public string text;
}

[Serializable]
public class DialoguesWrapper
{
    public DialogueLine[] dialogues;
}
