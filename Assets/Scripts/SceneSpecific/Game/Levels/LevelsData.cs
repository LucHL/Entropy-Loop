using System;
using UnityEngine;

[Serializable]
public class SpawnAlgoData
{
    public Strategy[] spawnStartegy;
    public string[] deck;
    public int[] manaCost;
    public string difficulty;
}

[Serializable]
public class LevelData
{
    [Header("Classic data")]
    public int currentlevel;
    public string chaptersBeforeGame;
    public string chaptersAfterGame;

    [Header("Detail Panel")]
    public string levelTitle;
    [TextArea(2, 5)] 
    public string levelDescription;
    public string corruptionType;
    public string currentPhase;
    public int totalPhases = 5;
    [TextArea(1, 3)] 
    public string subDescription;

    [Header("Spawn Algo")]
    public SpawnAlgoData spawnAlgo;
}

[Serializable]
public class LevelsWrapper
{
    public LevelData[] levels;
}
