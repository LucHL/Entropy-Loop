using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [SerializeField] GameObject tutorial;

    private readonly List<GameObject> tutorialSteps = new();
    private bool isTutorial = false;
    private int currentStep = 0;

    private int btnIncrementation = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        isTutorial = GameModeManager.isTutorial;

        if (isTutorial) {
            foreach (Transform t in tutorial.GetComponentInChildren<Transform>()) {
                if (t != tutorial.transform)
                    tutorialSteps.Add(t.gameObject);
            }

            tutorialSteps[currentStep].SetActive(true);
        }
    }

    public void NextStep()
    {
        if (tutorialSteps.Count == currentStep + 1) {
            tutorialSteps[currentStep].SetActive(false);
            isTutorial = false;
            GameModeManager.isTutorial = false;

            BugTracker.Info("[Tutorial] Tutorial finish.");
            return;
        }

        if (CheckEntitySpawn()) {
            FloatingTextManager.instance.Show("Aucune créature le terrain !");
            return;
        }

        tutorialSteps[currentStep].SetActive(false);
        currentStep++;
        tutorialSteps[currentStep].SetActive(true);
        BugTracker.Info("[Tutorial] Tutorial next Step '" + tutorialSteps[currentStep].name + "'.");
    }

    public void StepBack()
    {
        if (currentStep == 0)
            return;

        tutorialSteps[currentStep].SetActive(false);
        currentStep--;
        tutorialSteps[currentStep].SetActive(true);
        BugTracker.Info("[Tutorial] Tutorial Step back to '" + tutorialSteps[currentStep].name + "'.");
    }

    public void SetIsTutorial(bool tuto)
    {
        isTutorial = tuto;
    }

    public void ButtonNextAfterANumberOfClick(int numberOfClickMax)
    {
        if (btnIncrementation >= numberOfClickMax) {
            btnIncrementation = 1;
            NextStep();
            return;
        }

        btnIncrementation += 1;
    }

    public bool CheckEntitySpawn()
    {
        if (currentStep != 8) // poser une carte sur le board
            return false;

        int isPlayerChampionSpawn = GameLoopManager.instance.playerUnits.Count();

        if (isPlayerChampionSpawn > 0)
            return false;

        return true;
    }
}
