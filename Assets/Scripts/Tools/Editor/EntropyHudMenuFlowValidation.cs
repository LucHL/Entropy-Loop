using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EntropyHudMenuFlowValidation
{
    const string Key = "EntropyHud.MenuFlow.Active";
    static int stage, ticks, passed, draws;
    static bool sizeSetInPlay;
    static double started;
    static string output;
    static EntropyHudMenuFlowValidation()
    {
        if (Application.isBatchMode && SessionState.GetBool(Key, false)) Install();
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Dedicated batchmode only");
        SetTestResolution();
        SessionState.SetBool(Key, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Menu.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        Install();
        EditorApplication.isPlaying = true;
    }
    public static void SetTestResolution()
    {
        // Dedicated QA editor only: use a desktop viewport rather than the saved phone preview.
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var viewType = assembly.GetType("UnityEditor.GameView");
        var currentGroup = viewType.GetProperty("currentSizeGroupType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var groupValue = currentGroup != null ? currentGroup.GetValue(null) : Enum.Parse(groupType, BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString());
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupValue });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var modeType = assembly.GetType("UnityEditor.GameViewSizeType");
        var ctor = sizeType.GetConstructor(new[] { modeType, typeof(int), typeof(int), typeof(string) });
        var size = ctor.Invoke(new object[] { Enum.Parse(modeType, "FixedResolution"), 1588, 991, "Entropy HUD QA" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        var view = EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(view, count - 1);
        view.Show();
        view.Focus();
        view.Repaint();
    }

    static void Install()
    {
        var args = Environment.GetCommandLineArgs();
        int outputIndex = Array.IndexOf(args, "-entropyHudFlowOutput");
        output = outputIndex >= 0 && outputIndex + 1 < args.Length ? args[outputIndex + 1] : Path.Combine(Path.GetTempPath(), "EntropyHudMenuFlowValidation");
        Directory.CreateDirectory(output);
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }
    sealed class PendingUiFrame : Exception { }

    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        passed++;
        Debug.Log("FLOW PASS: " + message);
    }
    static string PathOf(Transform transform)
    {
        string path = transform.name;
        for (var t = transform.parent; t; t = t.parent) path = t.name + "/" + path;
        return path;
    }
    static void Click(RectTransform target, PointerEventData.InputButton mouseButton = PointerEventData.InputButton.Left)
    {
        Canvas.ForceUpdateCanvases();
        var graphic = target.GetComponent<Graphic>();
        if (graphic && graphic.depth < 0) throw new PendingUiFrame();
        var canvas = target.GetComponentInParent<Canvas>().rootCanvas;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(cam, target.TransformPoint(target.rect.center)), button = mouseButton };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0, "Pointer reaches " + PathOf(target));
        var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Debug.Log("FLOW HIT: " + PathOf(target) + " -> " + PathOf(hits[0].gameObject.transform));
        var expectedLevel = target.GetComponent<LevelButtonUI>();
        var hitLevel = handler ? handler.GetComponent<LevelButtonUI>() : null;
        bool sameLevel = expectedLevel && hitLevel && expectedLevel.levelText.text == hitLevel.levelText.text;
        Check(handler && (sameLevel || handler.transform == target || handler.transform.IsChildOf(target) || target.IsChildOf(handler.transform)), "No invisible layer blocks " + PathOf(target));
        ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
    }
    static RectTransform Find(string path) { return GameObject.Find(path).transform as RectTransform; }
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup - started > 180) { Finish("Flow timed out at stage " + stage); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        EditorApplication.QueuePlayerLoopUpdate();
        if (!sizeSetInPlay) { SetTestResolution(); sizeSetInPlay = true; return; }
        if (++ticks < 6) return;
        ticks = 0;
        try
        {
            if (stage == 0)
            {
                Debug.Log("FLOW SCREEN: " + Screen.width + "x" + Screen.height);
                Check(SceneManager.GetActiveScene().name == "Menu", "Play starts on the original Menu scene");
                Check(GameObject.Find("MainMenu") != null, "Original main menu is visible");
                Check(!GameObject.Find("Entropy Loop HUD"), "Combat HUD is absent from main menu");
                Click(Find("MenuCanvas/MainMenu/Button Deck Manager"));
                stage++;
            }
            else if (stage == 1)
            {
                var previews = UnityEngine.Object.FindObjectsByType<DeckPreview>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var item in previews) Debug.Log("FLOW DECK: " + item.name + " active=" + item.gameObject.activeInHierarchy + " label=" + (item.deckNameText ? item.deckNameText.text : "null"));
                var dataField = typeof(DeckPreview).GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var preview = previews.First(p => ((DeckData)dataField.GetValue(p))?.deckName == "Keronas");
                Click(preview.selectButton.transform as RectTransform);
                Check(GameModeManager.selectedDeck && GameModeManager.selectedDeck.deckName == "Keronas", "Deck can be chosen through its real UI button");
                stage++;
            }
            else if (stage == 2)
            {
                var play = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Play");
                Click(play.transform as RectTransform);
                stage++;
            }
            else if (stage == 3)
            {
                var level = UnityEngine.Object.FindObjectsByType<LevelButtonUI>(FindObjectsSortMode.None).First(b => b.levelText.text == "Level 1");
                Click(level.transform as RectTransform);
                stage++;
            }
            else if (stage == 4)
            {
                if (SceneManager.GetActiveScene().name != "Game" || !GameObject.Find("Entropy Loop HUD")) return;
                SetTestResolution();
                stage = 40;
            }
            else if (stage == 40)
            {
                Check(!DeckSelectionManager.instance.gameObject.activeSelf, "Level uses the deck selected from menu");
                Click(Find("Canvas/Tutorial/Step1/Button/Next"));
                stage = 5;
            }
            else if (stage == 5)
            {
                Click(Find("Canvas/Tutorial/Step2/Button/Next"));
                stage++;
            }
            else if (stage == 6)
            {
                var next = GameObject.Find("Canvas/Tutorial/Step3/Next");
                if (draws < 4)
                {
                    Click(next.transform as RectTransform);
                    draws++;
                    if (draws > 4) throw new Exception("Tutorial draw step failed to advance");
                }
                else
                {
                    Check(DeckManager.instance.handSlots.All(s => !s.IsEmpty()), "Tutorial draws fill four correctly positioned cards");
                    Click(Find("Canvas/Tutorial/Step4/Button/Next"));
                    stage++;
                }
            }
            else if (stage == 7)
            {
                Click(DeckManager.instance.handSlots[0].transform as RectTransform, PointerEventData.InputButton.Right);
                stage++;
            }
            else if (stage == 8)
            {
                Check(GameObject.Find("Canvas/Deck/CardSelected") != null, "Right-click opens original card preview");
                Finish("");
            }
        }
        catch (PendingUiFrame) { }
        catch (Exception e) { Finish(e.ToString()); }
    }
    static void Finish(string error)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        File.WriteAllText(output + "/result.txt", "Passed: " + passed + "\n" + error);
        Debug.Log("FLOW COMPLETE: " + passed + " passed. " + error);
        EditorApplication.Exit(string.IsNullOrEmpty(error) ? 0 : 1);
    }
}
