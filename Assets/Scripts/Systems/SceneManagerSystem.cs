using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScene : MonoBehaviour
{
    public GameObject loadingScreen;
    public Image loadingBar;

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

    IEnumerator LoadSceneAsync(string scene)
    {
        loadingScreen.SetActive(true);
        AsyncOperation op = SceneManager.LoadSceneAsync(scene);

        while (!op.isDone) {
            float progressValue = Mathf.Clamp01(op.progress / 0.9f);

            loadingBar.fillAmount = progressValue;

            yield return null;
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
