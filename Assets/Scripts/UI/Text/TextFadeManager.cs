using System.Collections;
using UnityEngine;
using TMPro;

public class LevelInformationFadeTextManager : MonoBehaviour
{
    public static LevelInformationFadeTextManager instance;

    [SerializeField] TextMeshProUGUI currentLevel;
    [SerializeField] TextMeshProUGUI difficulty;

    private float delayBeforeFade = 1f;
    private float fadeDuration = 0.25f;

    void Awake()
    {
        instance = this;
    }

    public void DisplayTextWithFade(string level, string dif)
    {
        currentLevel.text = "Niveau " + level;
        difficulty.text = dif;
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        Color originalColorLevel = currentLevel.color;
        Color originalColorDifficulty = currentLevel.color;
        currentLevel.color = new Color(originalColorLevel.r, originalColorLevel.g, originalColorLevel.b, 1f);
        difficulty.color = new Color(originalColorDifficulty.r, originalColorDifficulty.g, originalColorDifficulty.b, 1f);

        yield return new WaitForSeconds(delayBeforeFade);

        float currentTime = 0f;
        while (currentTime < fadeDuration) {
            currentTime += Time.deltaTime;
            
            float alpha = Mathf.Lerp(1f, 0f, currentTime / fadeDuration);
            
            currentLevel.color = new Color(originalColorLevel.r, originalColorLevel.g, originalColorLevel.b, alpha);
            difficulty.color = new Color(originalColorDifficulty.r, originalColorDifficulty.g, originalColorDifficulty.b, alpha);
            
            yield return null;
        }

        currentLevel.color = new Color(originalColorLevel.r, originalColorLevel.g, originalColorLevel.b, 0f);
        difficulty.color = new Color(originalColorDifficulty.r, originalColorDifficulty.g, originalColorDifficulty.b, 0f);
    }
}
