#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S_TrainingSceneSetup
{
    private const string TrainingSceneDirectory = "Assets/Scenes/For_game/Training Room";
    private const string ConfigDirectory = "Assets/Perfab/Tutorial/LevelConfigs";
    private const string TutorialPrefabPath = "Assets/Perfab/Tutorial/Pre_TutorialController.prefab";
    private const string ManagerRootPath = "Assets/Perfab/Player/ManagerRoot.prefab";
    private const string PlayerPrefabPath = "Assets/Perfab/Player/Pre_MainChar.prefab";
    private const string CameraPrefabPath = "Assets/Perfab/Player/Pre_Cam.prefab";
    private const string CheckpointPrefabPath = "Assets/Perfab/level_pre/Pre_Checkpoint.prefab";
    private const string PlatformSpriteGuid = "5254e10f517b0b74cac75d40265eccd3";

    private static readonly TrainingSceneSpec[] SceneSpecs =
    {
        new TrainingSceneSpec("Train 1", TutorialType.TeachAndPractice, new string[0], "Movement Controls", "WASD / Arrow Keys - Move\nSpace - Jump", false, 30f),
        new TrainingSceneSpec("Train 2", TutorialType.TeachAndPractice, new[] { "Sprint" }, "Sprint Controls", "Hold Sprint - Charge\nRelease Sprint - Dash through weak blocks", false, 30f),
        new TrainingSceneSpec("Train 3", TutorialType.PracticeOnly, new[] { "Sprint" }, "Sprint Practice", "Use Sprint to reach the exit before time runs out.", false, 30f),
        new TrainingSceneSpec("Train 4", TutorialType.TeachAndPractice, new[] { "Sprint", "FluidClimb" }, "Fluid Climb Controls", "Hold Grip near a wall - Climb\nUse Sprint and Climb together to cross gaps", false, 35f),
        new TrainingSceneSpec("Train 5", TutorialType.PracticeOnly, new[] { "Sprint", "FluidClimb" }, "Climb Practice", "Combine Sprint and Fluid Climb to reach the goal.", false, 35f),
        new TrainingSceneSpec("Train 6", TutorialType.None, new[] { "Sprint", "FluidClimb" }, "Final Training", "Reach the exit while avoiding patrol routes.", true, 40f),
    };

    [MenuItem("InkForm/Setup/Rebuild Training Scenes")]
    public static void RebuildTrainingScenes()
    {
        EnsureDirectories();

        Dictionary<string, S_LevelConfig> configs = CreateOrUpdateLevelConfigs();
        GameObject tutorialPrefab = CreateOrUpdateTutorialPrefab();
        ConfigureManagerRootPrefab();

        foreach (TrainingSceneSpec spec in SceneSpecs)
            CreateOrUpdateTrainingScene(spec, configs[spec.SceneName], tutorialPrefab);

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TrainingSetup] Training scenes, configs, tutorial prefab, ManagerRoot, and Build Settings rebuilt.");
    }

    private static void EnsureDirectories()
    {
        EnsureDirectory("Assets/Perfab/Tutorial");
        EnsureDirectory(ConfigDirectory);
        EnsureDirectory(TrainingSceneDirectory);
    }

    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            EnsureDirectory(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static Dictionary<string, S_LevelConfig> CreateOrUpdateLevelConfigs()
    {
        Dictionary<string, S_LevelConfig> configs = new Dictionary<string, S_LevelConfig>();

        foreach (TrainingSceneSpec spec in SceneSpecs)
        {
            string path = $"{ConfigDirectory}/{spec.SceneName}_LevelConfig.asset";
            S_LevelConfig config = AssetDatabase.LoadAssetAtPath<S_LevelConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<S_LevelConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            config.tutorialType = spec.TutorialType;
            config.skillsToUnlock = spec.Skills;
            config.promptTitle = spec.PromptTitle;
            config.promptDescription = spec.PromptDescription;
            config.familiarizeSubtitle = "Now get familiar with the controls.";
            config.countdownSubtitle = $"Reach the goal within {Mathf.CeilToInt(spec.TimeLimit)} seconds.";
            config.timeLimit = spec.TimeLimit;
            config.hasNPC = spec.HasNpc;
            EditorUtility.SetDirty(config);

            configs[spec.SceneName] = config;
        }

        return configs;
    }

    private static GameObject CreateOrUpdateTutorialPrefab()
    {
        GameObject root = new GameObject("Pre_TutorialController");
        root.AddComponent<AudioSource>().playOnAwake = false;
        root.AddComponent<S_VoiceLinePlayer>();
        root.AddComponent<S_UITutorialPrompt>();
        root.AddComponent<S_CountdownTimer>();

        S_TutorialController controller = root.AddComponent<S_TutorialController>();
        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("voiceLinePlayer").objectReferenceValue = root.GetComponent<S_VoiceLinePlayer>();
        controllerObject.FindProperty("tutorialPrompt").objectReferenceValue = root.GetComponent<S_UITutorialPrompt>();
        controllerObject.FindProperty("countdownTimer").objectReferenceValue = root.GetComponent<S_CountdownTimer>();
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TutorialPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void ConfigureManagerRootPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(ManagerRootPath);
        S_SkillTree skillTree = prefabRoot.GetComponentInChildren<S_SkillTree>(true);
        S_GameManager gameManager = prefabRoot.GetComponentInChildren<S_GameManager>(true);

        if (skillTree != null)
        {
            SerializedObject skillObject = new SerializedObject(skillTree);
            SerializedProperty allSkills = skillObject.FindProperty("allSkills");
            allSkills.arraySize = 3;
            allSkills.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<S_SkillBase>("Assets/Perfab/Skills/Sprint.asset");
            allSkills.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<S_SkillBase>("Assets/Perfab/Skills/FluidClimb.asset");
            allSkills.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<S_SkillBase>("Assets/Perfab/Skills/CameraControl.asset");
            skillObject.ApplyModifiedPropertiesWithoutUndo();
        }

        if (gameManager != null)
        {
            SerializedObject managerObject = new SerializedObject(gameManager);
            SerializedProperty levels = managerObject.FindProperty("levelScenes");
            levels.arraySize = SceneSpecs.Length + 1;
            for (int i = 0; i < SceneSpecs.Length; i++)
                SetSceneReference(levels.GetArrayElementAtIndex(i), $"{TrainingSceneDirectory}/{SceneSpecs[i].SceneName}.unity");
            SetSceneReference(levels.GetArrayElementAtIndex(SceneSpecs.Length), "Assets/Scenes/For_game/END.unity");
            managerObject.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, ManagerRootPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void CreateOrUpdateTrainingScene(TrainingSceneSpec spec, S_LevelConfig config, GameObject tutorialPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        InstantiatePrefabAt(ManagerRootPath, "ManagerRoot", new Vector3(-28f, 9f, 0f));
        GameObject player = InstantiatePrefabAt(PlayerPrefabPath, "Pre_MainChar", new Vector3(-12f, -1f, 0f));
        GameObject cameraRig = InstantiatePrefabAt(CameraPrefabPath, "Pre_Cam", Vector3.zero);
        AssignCameraTarget(cameraRig, player);
        InstantiatePrefabAt(CheckpointPrefabPath, "StartCheckpoint", new Vector3(-12f, -0.2f, 0f));

        GameObject layoutRoot = new GameObject("TrainingLayout");
        Sprite platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(PlatformSpriteGuid));
        CreateBlock("StartPlatform", layoutRoot.transform, new Vector2(-12f, -2f), new Vector2(8f, 1f), platformSprite, new Color(0.35f, 0.42f, 0.46f, 1f));
        CreateBlock("BuildSpaceFloor", layoutRoot.transform, new Vector2(0f, -2f), new Vector2(18f, 1f), platformSprite, new Color(0.27f, 0.32f, 0.37f, 1f));
        CreateBlock("GoalPlatform", layoutRoot.transform, new Vector2(12f, -2f), new Vector2(8f, 1f), platformSprite, new Color(0.35f, 0.42f, 0.46f, 1f));
        CreateBlock("LeftBoundary", layoutRoot.transform, new Vector2(-17f, 2.5f), new Vector2(1f, 10f), platformSprite, new Color(0.22f, 0.26f, 0.3f, 1f));
        CreateBlock("RightBoundary", layoutRoot.transform, new Vector2(17f, 2.5f), new Vector2(1f, 10f), platformSprite, new Color(0.22f, 0.26f, 0.3f, 1f));

        GameObject goal = CreateGoalTrigger(new Vector3(14f, 0f, 0f));
        GameObject cameraTarget = new GameObject("CameraPanTarget");
        cameraTarget.transform.position = goal.transform.position + new Vector3(0f, 2.5f, 0f);

        GameObject tutorial = PrefabUtility.InstantiatePrefab(tutorialPrefab, scene) as GameObject;
        tutorial.name = "TutorialController";
        ConfigureTutorialInstance(tutorial, config, cameraTarget.transform);

        if (spec.HasNpc)
            CreateNpcPlaceholder(layoutRoot.transform);

        Selection.activeGameObject = player;
        EditorSceneManager.SaveScene(scene, $"{TrainingSceneDirectory}/{spec.SceneName}.unity");
    }

    private static GameObject InstantiatePrefabAt(string path, string name, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.name = name;
        instance.transform.position = position;
        return instance;
    }

    private static void AssignCameraTarget(GameObject cameraRig, GameObject player)
    {
        if (cameraRig == null || player == null)
            return;

        S_CameraMove cameraMove = cameraRig.GetComponent<S_CameraMove>();
        if (cameraMove == null)
            return;

        Transform body = player.transform.Find("body");
        SerializedObject cameraObject = new SerializedObject(cameraMove);
        cameraObject.FindProperty("target").objectReferenceValue = body != null ? body.gameObject : player;
        cameraObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBlock(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
    {
        GameObject block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.position = position;
        block.layer = LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 0;

        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;

        BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static GameObject CreateGoalTrigger(Vector3 position)
    {
        GameObject goal = new GameObject("EndGoal_LevelExit");
        goal.layer = LayerMask.NameToLayer("Trigger") >= 0 ? LayerMask.NameToLayer("Trigger") : 0;
        goal.transform.position = position;

        BoxCollider2D collider = goal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2f, 3f);

        S_SectionGoal sectionGoal = goal.AddComponent<S_SectionGoal>();
        SerializedObject goalObject = new SerializedObject(sectionGoal);
        goalObject.FindProperty("sectionIndex").intValue = 0;
        goalObject.FindProperty("triggerType").enumValueIndex = (int)SectionTriggerType.End;
        goalObject.FindProperty("requestLevelExitOnEnd").boolValue = true;
        goalObject.ApplyModifiedPropertiesWithoutUndo();
        return goal;
    }

    private static void CreateNpcPlaceholder(Transform parent)
    {
        GameObject placeholder = new GameObject("NPC_Placement_Placeholder");
        placeholder.transform.SetParent(parent, false);
        placeholder.transform.position = new Vector3(4f, -0.9f, 0f);
    }

    private static void ConfigureTutorialInstance(GameObject tutorial, S_LevelConfig config, Transform cameraTarget)
    {
        S_TutorialController controller = tutorial.GetComponent<S_TutorialController>();
        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("levelConfig").objectReferenceValue = config;
        controllerObject.FindProperty("cameraPanTarget").objectReferenceValue = cameraTarget;
        controllerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/For_game/Start.unity", true)
        };

        foreach (TrainingSceneSpec spec in SceneSpecs)
            scenes.Add(new EditorBuildSettingsScene($"{TrainingSceneDirectory}/{spec.SceneName}.unity", true));

        scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/For_game/END.unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void SetSceneReference(SerializedProperty sceneReference, string scenePath)
    {
        sceneReference.FindPropertyRelative("scenePath").stringValue = scenePath;
        sceneReference.FindPropertyRelative("sceneName").stringValue = Path.GetFileNameWithoutExtension(scenePath);

        SerializedProperty sceneAsset = sceneReference.FindPropertyRelative("sceneAsset");
        if (sceneAsset != null)
            sceneAsset.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
    }

    private readonly struct TrainingSceneSpec
    {
        public readonly string SceneName;
        public readonly TutorialType TutorialType;
        public readonly string[] Skills;
        public readonly string PromptTitle;
        public readonly string PromptDescription;
        public readonly bool HasNpc;
        public readonly float TimeLimit;

        public TrainingSceneSpec(string sceneName, TutorialType tutorialType, string[] skills, string promptTitle, string promptDescription, bool hasNpc, float timeLimit)
        {
            SceneName = sceneName;
            TutorialType = tutorialType;
            Skills = skills;
            PromptTitle = promptTitle;
            PromptDescription = promptDescription;
            HasNpc = hasNpc;
            TimeLimit = timeLimit;
        }
    }
}
#endif
