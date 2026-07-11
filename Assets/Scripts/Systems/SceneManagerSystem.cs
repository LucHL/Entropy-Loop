using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingScene : MonoBehaviour
{
    public static LoadingScene Instance { get; private set; }

    [SerializeField] GameObject loadingScreen;
    [SerializeField] Image loadingBar;

    void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    //
    // Summary:
    //     Change scene WITHOUT a loading screen
    public void ChangeScene(string scene)
    {
        SceneManager.LoadSceneAsync(scene);
    }

    //
    // Summary:
    //     Change scene WITH a loading screen
    public void LoadScene(string scene)
    {

        StartCoroutine(LoadSceneAsync(scene));
    }

    //
    // Summary:
    //     Load Game scene WITH a loading screen
    public void LoadGame(bool isTutorial = false)
    {
        BugTracker.Info("Scene change to 'Game' tutorial is '" + isTutorial + "'.");

        GameModeManager.isTutorial = isTutorial;
        StartCoroutine(LoadSceneAsync("Game"));
    }

    public void LoadStory()
    {
        BugTracker.Info("Scene change to 'Story'.");
        SceneManager.LoadSceneAsync("Story");
    }

    private IEnumerator LoadSceneAsync(string scene)
    {
        loadingScreen.SetActive(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(scene);
        op.allowSceneActivation = true;

        while (!op.isDone) {
            float progressValue = Mathf.Clamp01(op.progress / 0.9f);
            loadingBar.fillAmount = progressValue;
            yield return null;
        }
        loadingBar.fillAmount = 1f;
        yield return new WaitForEndOfFrame();

        loadingScreen.SetActive(false);
    }
}
