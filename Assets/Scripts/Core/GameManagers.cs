using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Levels informations")]
    public LevelData[] alllevelData;
    public LevelData currentLevelData = null;

    [Header("Story")]
    public string nextStory = "";

    [Header("Levels")]
    public SpawnAlgoData spawnAlgoData;
    public int nbrSubLevelTotal = 0;
    public int nbrSubLevelRemaining = 0;

    // strategy
    public Strategy currentStrategy;

    // deck Enemy
    public string currentEnemyDeck;
    public int currentManaCost;


    [Header("Game")]
    public bool isPaused = false;

    [Header("Player")]
    public int maxLevelReach;
    public int maxLevelFinish;

    private void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadLevelsConfig();

        } else if (instance != this)
            Destroy(gameObject);
    }

    // ---- PAUSE / RESUME ----

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    // ---- LEVELS ----

    void LoadLevelsConfig()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Levels/levels_config");

        if (jsonFile != null) {
            LevelsWrapper wrapper = JsonUtility.FromJson<LevelsWrapper>(jsonFile.text);
            alllevelData = wrapper.levels;
        } else
            BugTracker.Critical("Failed to load levels configs from 'levels_config.json'.");

        SaveSystem.Load();
    }

    public void SaveLevelConfig(LevelData levelData)
    {
        BugTracker.Info("Save current level data, current level: " + levelData.currentlevel + ".");
        currentLevelData = levelData;
        SetStrategy();

        nextStory = levelData.chaptersBeforeGame;
    }

    public void SetNextLevel()
    {
        if (nbrSubLevelRemaining >= 1) { // sublevel
            SetNextStrategy();
            LoadingScene.Instance.ChangeScene("Game");
            return;
        }

        // next level
        int index = Array.IndexOf(alllevelData, currentLevelData);

        maxLevelFinish = currentLevelData.currentlevel;

        index+= 1;
        if (index >= alllevelData.Length) {
            BugTracker.Info("No level available, end of the game.");
            LoadingScene.Instance.ChangeScene("Menu");
            return;
        }
        currentLevelData = alllevelData[index];

        BugTracker.Info("Loading level: " + currentLevelData.currentlevel + ".");
        SetStrategy();

        SaveSystem.Save();

        LoadingScene.Instance.ChangeScene("Game");
    }

    private void SetStrategy()
    {
        spawnAlgoData = currentLevelData.spawnAlgo;

        currentStrategy = spawnAlgoData.spawnStartegy[0];
        currentEnemyDeck = spawnAlgoData.deck[0];
        currentManaCost = spawnAlgoData.manaCost[0];

        nbrSubLevelTotal = spawnAlgoData.spawnStartegy.Length - 1;

        nbrSubLevelRemaining = nbrSubLevelTotal;
        BugTracker.Info("Number total of sub levels: " + nbrSubLevelTotal + ".");
        BugTracker.Info("Number remaining of sub levels: " + nbrSubLevelRemaining + ".");
    }

    private void SetNextStrategy()
    {
        nbrSubLevelRemaining -= 1;

        currentStrategy = spawnAlgoData.spawnStartegy[nbrSubLevelTotal - nbrSubLevelRemaining];
        currentEnemyDeck = spawnAlgoData.deck[nbrSubLevelTotal - nbrSubLevelRemaining];
        currentManaCost = spawnAlgoData.manaCost[nbrSubLevelTotal - nbrSubLevelRemaining];

        BugTracker.Info("Loading next sub levels, remaining: " + (nbrSubLevelRemaining - 1) + ".");
    }
}
