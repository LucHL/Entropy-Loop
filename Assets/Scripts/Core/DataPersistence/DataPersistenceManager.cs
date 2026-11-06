using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : MonoBehaviour
{
    [Header("File Storage Config")]
    [SerializeField] private string fileName = "GameData.json";
    [SerializeField] private bool useEncryption = true;

    private GameData gameData;
    private List<IDataPersistence> dataPersistenceObjects;
    private FileDataHandler dataHandler;

    public static DataPersistenceManager instance;

    void Awake()
    {
        if (instance != null) {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, useEncryption);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;
    }

    public void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
    {
        dataPersistenceObjects = FindAllDataPersistenceInScene();
        LoadGame();
    }

    public void NewGame()
    {
        gameData = new();
    }

    public void LoadGame()
    {
        gameData = dataHandler.Load();

        if (gameData == null) {
            BugTracker.Warning("[DataPersistenceManager] No data was found. Initializing Default Data.");
            NewGame();
            SaveGame();
        }

        foreach (IDataPersistence dataPersistence in dataPersistenceObjects) {
            dataPersistence.LoadData(gameData);
        }
        BugTracker.Info("[DataPersistenceManager] Game successfully loaded.");
    }

    public void SaveGame()
    {
        if (gameData == null) {
            BugTracker.Warning("[DataPersistenceManager] No active gameData found during save. Initializing a new one.");
            NewGame();
        }

        if (dataPersistenceObjects != null) {
            foreach (IDataPersistence dataPersistence in dataPersistenceObjects) {
                if (dataPersistence != null)
                    dataPersistence.SaveData(gameData);
            }
        }
        
        dataHandler.Save(gameData);
        BugTracker.Info("[DataPersistenceManager] Game successfully saved.");
    }

    public void ResetSaveFile()
    {
        BugTracker.Info("[DataPersistenceManager] Resetting save file...");

        dataHandler.Delete();

        NewGame();

        if (dataPersistenceObjects != null) {
            foreach (IDataPersistence dataPersistence in dataPersistenceObjects) {
                if (dataPersistence != null)
                    dataPersistence.LoadData(gameData);
            }
        }

        dataHandler.Save(gameData);

        BugTracker.Info("[DataPersistenceManager] Save file has been fully reset.");
    }

    List<IDataPersistence> FindAllDataPersistenceInScene()
    {
        IEnumerable<IDataPersistence> dataPersistences = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDataPersistence>();

        return new List<IDataPersistence>(dataPersistences);
    }

    public bool HasGameData()
    {
        return (gameData != null);
    }
}
