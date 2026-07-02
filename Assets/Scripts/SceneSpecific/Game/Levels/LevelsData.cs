using System;

[Serializable]
public class SpawnAlgoData
{
    public Strategy[] spawnStartegy;
    public string[] deck;
    public int[] manaCost;
}

[Serializable]
public class LevelData
{
    public int currentlevel;
    public string chaptersBeforeGame;
    public string chaptersAfterGame;
    public SpawnAlgoData spawnAlgo;
}

[Serializable]
public class LevelsWrapper
{
    public LevelData[] levels;
}
