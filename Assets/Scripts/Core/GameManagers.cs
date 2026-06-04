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
        } else if (instance != this) {
            Destroy(gameObject);
        }
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

    public void SaveLevelConfig(LevelData levelData)
    {
        BugTracker.Info("Save current level data, current level: '" + levelData.currentlevel + "'.");
        currentLevelData = levelData;
        nextStory = levelData.chaptersBeforeGame;
    }
}
