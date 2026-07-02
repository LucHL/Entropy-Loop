using Newtonsoft.Json;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Current Level informations")]
    public LevelData[] alllevelData;
    public LevelData currentLevelData = null;
    public string nextStory = "";

    public bool isPaused = false;

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
    }

    public void SaveLevelConfig(LevelData levelData)
    {
        BugTracker.Info("Save current level data, current level: '" + levelData.currentlevel + "'.");
        currentLevelData = levelData;
        nextStory = levelData.chaptersBeforeGame;
    }
}
