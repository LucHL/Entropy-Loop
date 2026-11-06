using System;
using UnityEngine;

public class GameManager : MonoBehaviour, IDataPersistence
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

    // ---- Save / Load ----

    public void LoadData(GameData data)
    {
        maxLevelReach = data.maxLevelReach;
        maxLevelFinish = data.maxLevelFinish;
        currentLevelData = alllevelData[data.maxLevelReach];

        BugTracker.Info("[GameManager] Data loaded into GameManager.");
    }

    public void SaveData(GameData data)
    {
        if (currentLevelData != null)
            data.currentLevel = currentLevelData.currentlevel;

        data.maxLevelReach = maxLevelReach;
        data.maxLevelFinish = maxLevelFinish;
        // data.selectedDeckName = GameModeManager.selectedDeck.deckName ?? "";

        BugTracker.Info("[GameManager] Data saved from GameManager.");
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
    }

    public void SaveLevelConfig(LevelData levelData)
    {
        BugTracker.Info("[GameManager] Save current level data, current level: " + levelData.currentlevel + ".");
        currentLevelData = levelData;
        SetStrategy();

        nextStory = levelData.chaptersBeforeGame;

        DataPersistenceManager.instance.SaveGame();
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

        if (currentLevelData.currentlevel > maxLevelFinish) {
            maxLevelFinish = currentLevelData.currentlevel;
            maxLevelReach = currentLevelData.currentlevel + 1;
        }

        index+= 1;
        if (index >= alllevelData.Length) {
            BugTracker.Info("[GameManager] No level available, end of the game.");
            LoadingScene.Instance.ChangeScene("Menu");
            return;
        }
        currentLevelData = alllevelData[index];

        BugTracker.Info("[GameManager] Loading level: " + currentLevelData.currentlevel + ".");
        SetStrategy();

        DataPersistenceManager.instance.SaveGame();

        if (currentLevelData.chaptersBeforeGame != "") {
            nextStory = currentLevelData.chaptersBeforeGame;
            LoadingScene.Instance.LoadStory();
            return;
        }

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
        BugTracker.Info("[GameManager] Number total of sub levels: " + nbrSubLevelTotal + ".");
        BugTracker.Info("[GameManager] Number remaining of sub levels: " + nbrSubLevelRemaining + ".");
    }

    private void SetNextStrategy()
    {
        nbrSubLevelRemaining -= 1;

        currentStrategy = spawnAlgoData.spawnStartegy[nbrSubLevelTotal - nbrSubLevelRemaining];
        currentEnemyDeck = spawnAlgoData.deck[nbrSubLevelTotal - nbrSubLevelRemaining];
        currentManaCost = spawnAlgoData.manaCost[nbrSubLevelTotal - nbrSubLevelRemaining];

        BugTracker.Info("[GameManager] Loading next sub levels, remaining: " + (nbrSubLevelRemaining - 1) + ".");
    }
}
