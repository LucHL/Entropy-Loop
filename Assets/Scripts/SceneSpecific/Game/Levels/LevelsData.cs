using System;
using UnityEngine;

[Serializable]
public class SpawnAlgoData
{
    public Strategy[] spawnStartegy;
    public string[] deck;
    public int[] manaCost;
    public string difficulty;
    public string[] music;
}

[Serializable]
public class PanelDetailLevel
{
    [Header("Detail Panel")]
    public string levelTitle;
    [TextArea(2, 5)]
    public string levelDescription;
    public int totalPhases;
    [TextArea(1, 3)]
    public string levelZone;
}

[Serializable]
public class LevelData
{
    [Header("Current Level data")]
    public int currentlevel;
    public string chaptersBeforeGame;
    public string chaptersAfterGame;

    [Header("Panel Detail")]
    public PanelDetailLevel panelDetail;

    [Header("Spawn Algo")]
    public SpawnAlgoData spawnAlgo;
}

[Serializable]
public class LevelsWrapper
{
    public LevelData[] levels;
}
