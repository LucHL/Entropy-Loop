using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EntropyHudValidation
{
    const string Key = "EntropyHudValidation.Active";
    static string Output
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-entropyHudOutput");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Path.GetTempPath(), "EntropyLoopHudValidation");
        }
    }
    static int step, frames, passed;
    static double started;
    static string failures = "";
    static Camera captureCamera;
    static RenderTexture captureTarget;
    static string captureName;
    static Action afterCapture;

    static EntropyHudValidation()
    {
        if (Application.isBatchMode && SessionState.GetBool(Key, false)) Install();
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this validation in a dedicated Unity batchmode process.");
        EntropyHudMenuFlowValidation.SetTestResolution();
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Game.unity");
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        Install();
        EditorApplication.isPlaying = true;
    }

    static void Install()
    {
        Directory.CreateDirectory(Output);
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeGame()
    {
        if (!Application.isBatchMode || !SessionState.GetBool(Key, false)) return;
        if (Environment.GetCommandLineArgs().Contains("-entropyHudDirect")) return;
        var manager = GameManager.instance;
        if (!manager)
        {
            var obj = new GameObject("Validation GameManager");
            obj.SetActive(false);
            manager = obj.AddComponent<GameManager>();
            manager.currentLevelData = new LevelData();
            obj.SetActive(true);
        }
        manager.SaveLevelConfig(manager.alllevelData[1]);
        GameModeManager.isTutorial = false;
        GameModeManager.selectedDeck = Resources.Load<DeckData>("Decks/KeronasDeck");
    }

    static void Check(bool result, string description)
    {
        if (result) { passed++; Debug.Log("HUD PASS: " + description); }
        else { failures += description + "\n"; Debug.LogError("HUD FAIL: " + description); }
    }

    static Button Button(string name)
    {
        var root = GameObject.Find("Entropy Loop HUD");
        return root.transform.Find(name).GetComponent<Button>();
    }

    static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var rect = button.transform as RectTransform;
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center)) };
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
        var target = hits.Count > 0 ? UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject) : null;
        Debug.Log("POINTER DETAIL " + button.name + " position=" + pointer.position + " screen=" + Screen.width + "x" + Screen.height + " active=" + button.gameObject.activeInHierarchy + " interactable=" + button.interactable + " raycast=" + (button.targetGraphic && button.targetGraphic.raycastTarget) + " canvas=" + canvas.name);
        Check(target == button.gameObject, "Real pointer reaches " + button.name);
        if (target != button.gameObject) throw new Exception("Click blocked for " + button.name + " by " + (hits.Count > 0 ? hits[0].gameObject.name : "no UI target"));
        UnityEngine.EventSystems.ExecuteEvents.Execute(target, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup - started > 240) { Finish("Validation timed out"); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        EditorApplication.QueuePlayerLoopUpdate();
        frames++;
        if (frames < 5) return;
        try
        {
            if (step == 0)
            {
                if (!GameObject.Find("Entropy Loop HUD")) return;
                step++; frames = 0;
                Check(GameManager.instance && GameManager.instance.currentLevelData != null, "Direct scene launch initializes manager and full level configuration");
                if (GameModeManager.selectedDeck == null)
                    DeckSelectionManager.instance.SelectDeck(Resources.Load<DeckData>("Decks/KeronasDeck"));
                Check(GameModeManager.selectedDeck != null, "Keronas deck loaded");
                var deck = DeckManager.instance;
                Check(Button("Combat").onClick.GetPersistentEventCount() > 0 && Button("Pioche").onClick.GetPersistentEventCount() > 0
                    && Button("Boutique").onClick.GetPersistentEventCount() > 0 && Button("Journal").onClick.GetPersistentEventCount() > 0,
                    "Original serialized button callbacks preserved");
                Check(Button("Menu").GetComponent<ChangeSceneButton>().enabled, "Original return-to-menu component unchanged");
                Check(deck.handSlots.Length == 4, "Four original hand slots retained");
                step = 9;
            }
            else if (step == 9)
            {
                for (int i = 0; i < 4; i++) Click(Button("Pioche"));
                step = 1; frames = 0;
            }
            else if (step == 1)
            {
                step++; frames = 0;
                var deck = DeckManager.instance;
                Check(deck.handSlots.All(s => !s.IsEmpty()), "Four real draws fill four slots");
                Check(deck.handSlots.All(s => s.GetComponent<CardUI>().cardImage.sprite == s.GetComponent<CardUI>().cardData.cardImage), "Original card artwork retained");
                Check(deck.handSlots.All(s => s.GetComponent<CardUI>().cardImage.preserveAspect), "Card aspect ratios preserved");
                var positions = deck.handSlots.Select(s => ((RectTransform)s.transform).anchoredPosition).ToArray();
                Check(positions.Zip(positions.Skip(1), (a,b) => b.x - a.x).All(x => x >= 190), "Cards ordered left to right without overlaps");
                var data = deck.handSlots[1].GetComponent<CardUI>().cardData;
                deck.handSlots[1].ClearSlot();
                deck.AddCardInDeck(data);
                deck.DrawCard();
                Check(((RectTransform)deck.handSlots[1].transform).anchoredPosition == positions[1], "Refill keeps the same hand position");
                Check(!Button("Pioche").interactable, "Draw disabled when hand is full");
                var before = deck.deck.Count;
                deck.DrawCard();
                Check(deck.deck.Count == before, "Full hand does not consume a card");
                Capture("entropy-hud-reference.png", 1588, 991, () => { step = 2; frames = 0; });
                step = 100;
            }
            else if (step == 2)
            {
                Click(Button("Journal"));
                var window = UnityEngine.Object.FindFirstObjectByType<ActionLogWindow>();
                Check(window.actionLogPanel.activeSelf, "Journal button opens existing log");
                GameLogManager.Instance.AddLog("Vérification de l’ATH : journal opérationnel.");
                step = 3; frames = 0;
            }
            else if (step == 3)
            {
                var window = UnityEngine.Object.FindFirstObjectByType<ActionLogWindow>();
                Check(window.actionLogPanel.GetComponentInChildren<LogTemplateUI>() != null, "Journal receives live entries");
                Click(window.actionLogPanel.GetComponentsInChildren<Button>().First(b => b.name == "Close Button"));
                Check(!window.actionLogPanel.activeSelf, "Original journal close action works");
                Click(Button("Boutique"));
                Check(ShopManager.instance.shopPopup.activeSelf, "Boutique button opens existing shop");
                step = 31; frames = 0;
            }
            else if (step == 31)
            {
                Click(ShopManager.instance.closeButton);
                Check(!ShopManager.instance.shopPopup.activeSelf, "Shop close button works");
                var mana = ManaManager.instance;
                mana.SpendMana(3);
                step = 4; frames = 0;
            }
            else if (step == 4)
            {
                var text = GameObject.Find("Entropy Loop HUD").transform.Find("Mana").GetComponentInChildren<TMPro.TMP_Text>();
                Check(text.text.Contains(ManaManager.instance.currentMana.ToString()), "Mana label reflects actual spent mana");
                var loop = GameLoopManager.instance;
                var count = DeckManager.instance.deck.Count;
                ManaManager.instance.AddMana(10);
                var card = DeckManager.instance.handSlots[0].GetComponent<CardUI>();
                Click(card.GetComponent<Button>());
                var cell = UnityEngine.Object.FindObjectsByType<GridCell>(FindObjectsSortMode.None).First(c => c.name.Split('_').Length >= 3 && int.TryParse(c.name.Split('_')[2], out int row) && row < 4);
                var spawned = (bool)typeof(GridCell).GetMethod("SpawnUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(cell, null);
                Check(spawned && loop.playerUnits.Count > 0, "Selected card deploys its real unit to the board");
                if (spawned)
                {
                    ManaManager.instance.SpendMana(card.cardData.manaCost);
                    card.GetComponent<HandSlot>().ClearSlot();
                    loop.DeselectCard();
                }
                step = 41; frames = 0;
            }
            else if (step == 41)
            {
                Click(Button("Combat"));
                Check(GameLoopManager.instance.isGameRunning, "Combat button starts the real game loop");
                step = 5; frames = 0;
            }
            else if (step == 5)
            {
                int count = DeckManager.instance.deck.Count;
                var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                UnityEngine.EventSystems.ExecuteEvents.Execute(Button("Pioche").gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                UnityEngine.EventSystems.ExecuteEvents.Execute(DeckManager.instance.handSlots[1].gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                Check(GameLoopManager.instance.selectedCard == null && DeckManager.instance.deck.Count == count, "HUD blocks clicks on draw and hand during combat");
                Check(!Button("Pioche").interactable && !Button("Combat").interactable, "Combat controls display disabled states");
                Check(DeckManager.instance.handSlots.All(s => !s.GetComponent<Button>().interactable), "Hand input disabled during combat");
                GameLoopManager.instance.isGameRunning = false;
                Capture("entropy-hud-1280x1024.png", 1280, 1024, () => { step = 6; frames = 0; });
                step = 100;
            }
            else if (step == 6) Finish("");
            else if (step == 100 && captureCamera)
            {
                Canvas.ForceUpdateCanvases();
                captureCamera.Render();
                var old = RenderTexture.active;
                RenderTexture.active = captureTarget;
                var texture = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0,0,captureTarget.width,captureTarget.height),0,0);
                texture.Apply();
                File.WriteAllBytes(Output + "/" + captureName, texture.EncodeToPNG());
                RenderTexture.active = old;
                UnityEngine.Object.Destroy(texture);
                captureCamera.targetTexture = null;
                captureTarget.Release();
                UnityEngine.Object.Destroy(captureTarget);
                captureCamera = null;
                afterCapture();
            }
        }
        catch (Exception e) { Finish(e.ToString()); }
    }

    static void Capture(string name, int width, int height, Action done)
    {
        captureCamera = Camera.main;
        captureTarget = new RenderTexture(width, height, 24);
        captureCamera.targetTexture = captureTarget;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = captureCamera;
                canvas.planeDistance = 1;
            }
        }
        captureName = name;
        afterCapture = done;
        frames = 0;
    }

    static void Finish(string error)
    {
        failures += error;
        File.WriteAllText(Output + "/entropy-hud-validation.txt", "Passed: " + passed + "\n" + failures);
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Debug.Log("HUD VALIDATION COMPLETE: " + passed + " passed. " + failures);
        EditorApplication.Exit(string.IsNullOrEmpty(failures) ? 0 : 1);
    }
}
