/*using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

public class AnimatedPrefabCreator : EditorWindow
{
    private Object modelAsset;
    private AnimationClip idleClip;
    private AnimationClip runClip;
    private AnimationClip attackClip;
    private AnimationClip dieClip;
    private string customPrefabName = "";

    private bool showOptionalAnimations = true;
    private bool addExitBackToIdle = true;
    private float exitHasExitTime = 0.95f;
    private float transitionDuration = 0.1f;

    private GUIStyle titleStyle, headerStyle, smallLabelStyle, bigButtonStyle, subtleLabelStyle;

    [MenuItem("Tools/Animated Prefab Creator")]
    public static void ShowWindow()
    {
        var win = GetWindow<AnimatedPrefabCreator>("Animated Prefab Creator");
        win.minSize = new Vector2(560, 680);
        var p = win.position;
        if (p.width < 560f || p.height < 680f)
            win.position = new Rect(p.x, p.y, 720f, 780f);
    }

    private void OnEnable()
    {
        minSize = new Vector2(560, 680);
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleLeft };

        if (headerStyle == null)
            headerStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 10, 12) };

        if (smallLabelStyle == null)
            smallLabelStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

        if (subtleLabelStyle == null)
        {
            subtleLabelStyle = new GUIStyle(EditorStyles.label) { fontSize = 10 };
            var c = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.6f) : new Color(0f, 0f, 0f, 0.6f);
            subtleLabelStyle.normal.textColor = c;
        }

        if (bigButtonStyle == null)
        {
            bigButtonStyle = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold, fixedHeight = 36 };
            bigButtonStyle.margin = new RectOffset(0, 0, 4, 4);
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUIContent animIcon = EditorGUIUtility.IconContent("d_AnimatorController Icon");
        if (animIcon == null || animIcon.image == null)
            animIcon = EditorGUIUtility.IconContent("AnimatorController Icon") ?? GUIContent.none;

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(animIcon, GUILayout.Width(24), GUILayout.Height(24));
            GUILayout.Label("Auto-create an animated prefab", titleStyle ?? EditorStyles.boldLabel);
        }
        EditorGUILayout.LabelField("Pick a model, required animations, then generate a ready-to-use prefab.", smallLabelStyle ?? EditorStyles.miniLabel);
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("3D Model", EditorStyles.boldLabel);
        var modelContent = new GUIContent("Model (.fbx or Prefab)", "Drag & drop an FBX or a model Prefab.");
        modelAsset = EditorGUILayout.ObjectField(modelContent, modelAsset, typeof(Object), false);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("Required Animations", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        var idleContent = new GUIContent("Idle", "Idle/rest animation (required)");
        var runContent = new GUIContent("Run", "Run/locomotion animation (required)");
        idleClip = (AnimationClip)EditorGUILayout.ObjectField(idleContent, idleClip, typeof(AnimationClip), false);
        runClip  = (AnimationClip)EditorGUILayout.ObjectField(runContent,  runClip,  typeof(AnimationClip), false);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4);
        showOptionalAnimations = EditorGUILayout.Foldout(showOptionalAnimations, "Optional Animations", true);
        if (showOptionalAnimations)
        {
            EditorGUI.indentLevel++;
            var atkContent = new GUIContent("Attack", "Optional: will be wired to Trigger 'IsAttacking'.");
            var dieContent = new GUIContent("Die", "Optional: will be wired to Trigger 'IsDead'.");
            attackClip = (AnimationClip)EditorGUILayout.ObjectField(atkContent, attackClip, typeof(AnimationClip), false);
            dieClip    = (AnimationClip)EditorGUILayout.ObjectField(dieContent, dieClip, typeof(AnimationClip), false);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        addExitBackToIdle = EditorGUILayout.Toggle(new GUIContent("Return to Idle after Attack/Die", "Adds transitions back to Idle with Has Exit Time."), addExitBackToIdle);
        exitHasExitTime = Mathf.Clamp01(EditorGUILayout.Slider(new GUIContent("Exit Time (Attack/Die)", "Normalized time (0-1) when the state can exit back to Idle."), exitHasExitTime, 0.6f, 1f));
        transitionDuration = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Transition Duration (s)", "Blend duration for state transitions."), transitionDuration));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("Name & Output", EditorStyles.boldLabel);
        customPrefabName = EditorGUILayout.TextField(new GUIContent("Prefab Name", "Leave empty to use <ModelName>_Animated"), customPrefabName);

        string modelName = modelAsset != null ? modelAsset.name : "ModelName";
        string finalName = string.IsNullOrEmpty(customPrefabName) ? modelName + "_Animated" : customPrefabName;

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Final name preview:", subtleLabelStyle ?? EditorStyles.miniLabel);
        EditorGUILayout.LabelField(finalName, EditorStyles.miniBoldLabel);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Files that will be created:", subtleLabelStyle ?? EditorStyles.miniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(EditorGUIUtility.IconContent("AnimatorController Icon"), GUILayout.Width(16), GUILayout.Height(16));
            EditorGUILayout.SelectableLabel($"Assets/Animators/{finalName}_Controller.controller", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(EditorGUIUtility.IconContent("Prefab Icon"), GUILayout.Width(16), GUILayout.Height(16));
            EditorGUILayout.SelectableLabel($"Assets/Prefabs/{finalName}.prefab", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        bool missingModel = modelAsset == null;
        bool missingIdle  = idleClip == null;
        bool missingRun   = runClip == null;
        bool canGenerate  = !missingModel && !missingIdle && !missingRun;

        if (!canGenerate)
        {
            string msg = "Missing required fields:\n"
                            + (missingModel ? "• Model\n" : "")
                            + (missingIdle  ? "• Idle\n"   : "")
                            + (missingRun   ? "• Run\n"    : "");
            EditorGUILayout.HelpBox(msg.TrimEnd('\n','\r'), MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("All set. Click ‘Generate Prefab’.", MessageType.None);
        }

        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            if (GUILayout.Button("Generate Prefab", bigButtonStyle ?? EditorStyles.miniButton))
            {
                CreatePrefabWithAnimator();
            }
        }
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Animator parameters created: IsMoving (Bool), IsAttacking (Trigger), IsDead (Trigger)", smallLabelStyle ?? EditorStyles.miniLabel);

        EditorGUILayout.Space(6);
    }

    private void CreatePrefabWithAnimator()
    {
        if (modelAsset == null || idleClip == null || runClip == null)
        {
            Debug.LogWarning("Please specify a model and at least Idle/Run animations.");
            return;
        }

        string modelName = modelAsset.name;
        string finalName = string.IsNullOrEmpty(customPrefabName) ? modelName + "_Animated" : customPrefabName;

        EnsureFolder("Assets/Animators");
        EnsureFolder("Assets/Prefabs");

        string animatorPath = GetUniqueAssetPath($"Assets/Animators/{finalName}_Controller.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(animatorPath);
        Undo.RegisterCreatedObjectUndo(controller, "Create Animator Controller");

        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState runState = sm.AddState("Run");
        runState.motion = runClip;

        AnimatorState attackState = null;
        if (attackClip != null)
        {
            attackState = sm.AddState("Attack");
            attackState.motion = attackClip;
            var anyToAttack = sm.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            anyToAttack.duration = transitionDuration;
            anyToAttack.hasExitTime = false;

            if (addExitBackToIdle)
            {
                var back = attackState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = Mathf.Clamp01(exitHasExitTime);
                back.duration = transitionDuration;
                back.canTransitionToSelf = false;
            }
        }

        AnimatorState dieState = null;
        if (dieClip != null)
        {
            dieState = sm.AddState("Die");
            dieState.motion = dieClip;
            var anyToDie = sm.AddAnyStateTransition(dieState);
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            anyToDie.duration = transitionDuration;
            anyToDie.hasExitTime = false;

            if (addExitBackToIdle)
            {
                var back = dieState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = Mathf.Clamp01(exitHasExitTime);
                back.duration = transitionDuration;
                back.canTransitionToSelf = false;
            }
        }

        sm.defaultState = idleState;
        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toRun.duration = transitionDuration;
        toRun.canTransitionToSelf = false;

        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.duration = transitionDuration;
        toIdle.canTransitionToSelf = false;

        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        if (modelInstance == null)
        {
            Debug.LogError("Failed to instantiate the model.");
            return;
        }

        try
        {
            Undo.RegisterCreatedObjectUndo(modelInstance, "Create Animated Prefab Instance");

            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
                animator = modelInstance.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;

            if (animator.avatar == null)
                Debug.LogWarning("Animator has no Avatar assigned. If using Humanoid, ensure your model provides an Avatar.");

            string prefabPath = GetUniqueAssetPath($"Assets/Prefabs/{finalName}.prefab");
            var saved = PrefabUtility.SaveAsPrefabAsset(modelInstance, prefabPath);
            if (saved == null)
            {
                Debug.LogError("Failed to save the prefab.");
                return;
            }

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("Animated prefab created successfully: " + prefabPath);
        }
        finally
        {
            if (modelInstance != null)
                DestroyImmediate(modelInstance);
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }

    private static string GetUniqueAssetPath(string path)
    {
        if (!File.Exists(path)) return path;
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        int i = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({i++}){ext}");
        } while (File.Exists(candidate));
        return candidate.Replace('\\', '/');
    }
}*/


using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

public class AnimatedPrefabCreator : EditorWindow
{
    private Object modelAsset;
    private AnimationClip idleClip;
    private AnimationClip runClip;
    private AnimationClip attackClip;
    private AnimationClip dieClip;
    private string customPrefabName = "";

    private bool showOptionalAnimations = true;
    private bool addExitBackToIdle = true;
    private float exitHasExitTime = 0.95f;
    private float transitionDuration = 0.1f;

    private GUIStyle titleStyle, headerStyle, smallLabelStyle, bigButtonStyle, subtleLabelStyle;

    [MenuItem("Tools/Animated Prefab Creator")]
    public static void ShowWindow()
    {
        var win = GetWindow<AnimatedPrefabCreator>("Animated Prefab Creator");
        win.minSize = new Vector2(560, 680);
        var p = win.position;
        if (p.width < 560f || p.height < 680f)
            win.position = new Rect(p.x, p.y, 720f, 780f);
    }

    private void OnEnable()
    {
        minSize = new Vector2(560, 680);
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleLeft };

        if (headerStyle == null)
            headerStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 10, 12) };

        if (smallLabelStyle == null)
            smallLabelStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

        if (subtleLabelStyle == null)
        {
            subtleLabelStyle = new GUIStyle(EditorStyles.label) { fontSize = 10 };
            var c = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.6f) : new Color(0f, 0f, 0f, 0.6f);
            subtleLabelStyle.normal.textColor = c;
        }

        if (bigButtonStyle == null)
        {
            bigButtonStyle = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold, fixedHeight = 36 };
            bigButtonStyle.margin = new RectOffset(0, 0, 4, 4);
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUIContent animIcon = EditorGUIUtility.IconContent("d_AnimatorController Icon");
        if (animIcon == null || animIcon.image == null)
            animIcon = EditorGUIUtility.IconContent("AnimatorController Icon") ?? GUIContent.none;

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(animIcon, GUILayout.Width(24), GUILayout.Height(24));
            GUILayout.Label("Auto-create an animated prefab", titleStyle ?? EditorStyles.boldLabel);
        }
        EditorGUILayout.LabelField("Pick a model, required animations, then generate a ready-to-use prefab.", smallLabelStyle ?? EditorStyles.miniLabel);
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("3D Model", EditorStyles.boldLabel);
        var modelContent = new GUIContent("Model (.fbx or Prefab)", "Drag & drop an FBX or a model Prefab.");
        modelAsset = EditorGUILayout.ObjectField(modelContent, modelAsset, typeof(Object), false);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("Required Animations", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        var idleContent = new GUIContent("Idle", "Idle/rest animation (required)");
        var runContent = new GUIContent("Run", "Run/locomotion animation (required)");
        idleClip = (AnimationClip)EditorGUILayout.ObjectField(idleContent, idleClip, typeof(AnimationClip), false);
        runClip  = (AnimationClip)EditorGUILayout.ObjectField(runContent,  runClip,  typeof(AnimationClip), false);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4);
        showOptionalAnimations = EditorGUILayout.Foldout(showOptionalAnimations, "Optional Animations", true);
        if (showOptionalAnimations)
        {
            EditorGUI.indentLevel++;
            var atkContent = new GUIContent("Attack", "Optional: will be wired to Trigger 'IsAttacking'.");
            var dieContent = new GUIContent("Die", "Optional: will be wired to Trigger 'IsDead'.");
            attackClip = (AnimationClip)EditorGUILayout.ObjectField(atkContent, attackClip, typeof(AnimationClip), false);
            dieClip    = (AnimationClip)EditorGUILayout.ObjectField(dieContent, dieClip, typeof(AnimationClip), false);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        addExitBackToIdle = EditorGUILayout.Toggle(new GUIContent("Return to Idle after Attack/Die", "Adds transitions back to Idle with Has Exit Time."), addExitBackToIdle);
        exitHasExitTime = Mathf.Clamp01(EditorGUILayout.Slider(new GUIContent("Exit Time (Attack/Die)", "Normalized time (0-1) when the state can exit back to Idle."), exitHasExitTime, 0.6f, 1f));
        transitionDuration = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Transition Duration (s)", "Blend duration for state transitions."), transitionDuration));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(headerStyle ?? EditorStyles.helpBox);
        EditorGUILayout.LabelField("Name & Output", EditorStyles.boldLabel);
        customPrefabName = EditorGUILayout.TextField(new GUIContent("Prefab Name", "Leave empty to use <ModelName>_Animated"), customPrefabName);

        string modelName = modelAsset != null ? modelAsset.name : "ModelName";
        string finalName = string.IsNullOrEmpty(customPrefabName) ? modelName + "_Animated" : customPrefabName;

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Final name preview:", subtleLabelStyle ?? EditorStyles.miniLabel);
        EditorGUILayout.LabelField(finalName, EditorStyles.miniBoldLabel);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Files that will be created:", subtleLabelStyle ?? EditorStyles.miniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(EditorGUIUtility.IconContent("AnimatorController Icon"), GUILayout.Width(16), GUILayout.Height(16));
            EditorGUILayout.SelectableLabel($"Assets/Animators/{finalName}_Controller.controller", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(EditorGUIUtility.IconContent("Prefab Icon"), GUILayout.Width(16), GUILayout.Height(16));
            EditorGUILayout.SelectableLabel($"Assets/Prefabs/{finalName}.prefab", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        bool missingModel = modelAsset == null;
        bool missingIdle  = idleClip == null;
        bool missingRun   = runClip == null;
        bool canGenerate  = !missingModel && !missingIdle && !missingRun;

        if (!canGenerate)
        {
            string msg = "Missing required fields:\n"
                            + (missingModel ? "• Model\n" : "")
                            + (missingIdle  ? "• Idle\n"   : "")
                            + (missingRun   ? "• Run\n"    : "");
            EditorGUILayout.HelpBox(msg.TrimEnd('\n','\r'), MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("All set. Click ‘Generate Prefab’.", MessageType.None);
        }

        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            if (GUILayout.Button("Generate Prefab", bigButtonStyle ?? EditorStyles.miniButton))
            {
                CreatePrefabWithAnimator();
            }
        }
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Animator parameters created: IsMoving (Bool), IsAttacking (Trigger), IsDead (Trigger)", smallLabelStyle ?? EditorStyles.miniLabel);

        EditorGUILayout.Space(6);
    }

    private void CreatePrefabWithAnimator()
    {
        if (modelAsset == null || idleClip == null || runClip == null)
        {
            BugTracker.Warning("Please specify a model and at least Idle/Run animations.");
            return;
        }

        string modelName = modelAsset.name;
        string finalName = string.IsNullOrEmpty(customPrefabName) ? modelName + "_Animated" : customPrefabName;

        EnsureFolder("Assets/Animators");
        EnsureFolder("Assets/Prefabs");

        string animatorPath = GetUniqueAssetPath($"Assets/Animators/{finalName}_Controller.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(animatorPath);
        Undo.RegisterCreatedObjectUndo(controller, "Create Animator Controller");

        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState runState = sm.AddState("Run");
        runState.motion = runClip;

        AnimatorState attackState = null;
        if (attackClip != null)
        {
            attackState = sm.AddState("Attack");
            attackState.motion = attackClip;
            var anyToAttack = sm.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            anyToAttack.duration = transitionDuration;
            anyToAttack.hasExitTime = false;

            if (addExitBackToIdle)
            {
                var back = attackState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = Mathf.Clamp01(exitHasExitTime);
                back.duration = transitionDuration;
                back.canTransitionToSelf = false;
            }
        }

        AnimatorState dieState = null;
        if (dieClip != null)
        {
            dieState = sm.AddState("Die");
            dieState.motion = dieClip;
            var anyToDie = sm.AddAnyStateTransition(dieState);
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            anyToDie.duration = transitionDuration;
            anyToDie.hasExitTime = false;

            if (addExitBackToIdle)
            {
                var back = dieState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = Mathf.Clamp01(exitHasExitTime);
                back.duration = transitionDuration;
                back.canTransitionToSelf = false;
            }
        }

        sm.defaultState = idleState;
        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toRun.duration = transitionDuration;
        toRun.canTransitionToSelf = false;

        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.duration = transitionDuration;
        toIdle.canTransitionToSelf = false;

        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        if (modelInstance == null)
        {
            BugTracker.Error("Failed to instantiate the model.");
            return;
        }

        try
        {
            Undo.RegisterCreatedObjectUndo(modelInstance, "Create Animated Prefab Instance");

            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
                animator = modelInstance.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;

            if (animator.avatar == null)
                BugTracker.Warning("Animator has no Avatar assigned. If using Humanoid, ensure your model provides an Avatar.");

            string prefabPath = GetUniqueAssetPath($"Assets/Prefabs/{finalName}.prefab");
            var saved = PrefabUtility.SaveAsPrefabAsset(modelInstance, prefabPath);
            if (saved == null)
            {
                BugTracker.Error("Failed to save the prefab.");
                return;
            }

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            BugTracker.Info("Animated prefab created successfully: " + prefabPath);
        }
        finally
        {
            if (modelInstance != null)
                DestroyImmediate(modelInstance);
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }

    private static string GetUniqueAssetPath(string path)
    {
        if (!File.Exists(path)) return path;
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        int i = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({i++}){ext}");
        } while (File.Exists(candidate));
        return candidate.Replace('\\', '/');
    }
}