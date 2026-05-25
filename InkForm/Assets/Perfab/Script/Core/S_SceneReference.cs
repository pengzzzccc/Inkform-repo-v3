using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class S_SceneReference
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif
    [SerializeField, HideInInspector] private string scenePath;
    [SerializeField, HideInInspector] private string sceneName;

    public string ScenePath => scenePath;
    public string SceneName => sceneName;
    public string RuntimeKey => !string.IsNullOrWhiteSpace(scenePath) ? scenePath : sceneName;
    public bool IsValid => !string.IsNullOrWhiteSpace(RuntimeKey);

    public S_SceneReference()
    {
    }

    public S_SceneReference(string legacySceneName)
    {
        SetLegacyName(legacySceneName);
    }

    public void SetLegacyName(string legacySceneName)
    {
        string trimmed = NormalizeSeparators(legacySceneName);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            scenePath = string.Empty;
            sceneName = string.Empty;
            return;
        }

        if (trimmed.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            SetPath(trimmed);
            return;
        }

        scenePath = string.Empty;
        sceneName = trimmed;
    }

    public bool Matches(string sceneKey)
    {
        return SceneKeysMatch(RuntimeKey, sceneKey);
    }

    public static bool CanLoadScene(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
            return false;

        if (Application.CanStreamedLevelBeLoaded(sceneKey))
            return true;

        if (SceneUtility.GetBuildIndexByScenePath(sceneKey) >= 0)
            return true;

        string nameOnly = Path.GetFileNameWithoutExtension(sceneKey);
        return !string.IsNullOrWhiteSpace(nameOnly)
            && !string.Equals(nameOnly, sceneKey, StringComparison.OrdinalIgnoreCase)
            && Application.CanStreamedLevelBeLoaded(nameOnly);
    }

    public static bool SceneKeysMatch(string first, string second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
            return false;

        string normalizedFirst = NormalizeSeparators(first);
        string normalizedSecond = NormalizeSeparators(second);
        if (string.Equals(normalizedFirst, normalizedSecond, StringComparison.OrdinalIgnoreCase))
            return true;

        string firstName = Path.GetFileNameWithoutExtension(normalizedFirst);
        string secondName = Path.GetFileNameWithoutExtension(normalizedSecond);
        return !string.IsNullOrWhiteSpace(firstName)
            && string.Equals(firstName, secondName, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return RuntimeKey;
    }

    private void SetPath(string path)
    {
        scenePath = NormalizeSeparators(path);
        sceneName = Path.GetFileNameWithoutExtension(scenePath);
    }

    private static string NormalizeSeparators(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Replace('\\', '/');
    }

#if UNITY_EDITOR
    public void EditorSyncAsset()
    {
        if (sceneAsset == null)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
                sceneName = Path.GetFileNameWithoutExtension(scenePath);

            return;
        }

        string path = AssetDatabase.GetAssetPath(sceneAsset);
        if (!string.IsNullOrWhiteSpace(path))
            SetPath(path);
    }

    public bool EditorTryAssignByKey(string sceneKey)
    {
        string path = EditorFindScenePath(sceneKey);
        if (string.IsNullOrWhiteSpace(path))
        {
            SetLegacyName(sceneKey);
            return false;
        }

        sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        SetPath(path);
        return sceneAsset != null;
    }

    public bool EditorIsInEnabledBuildScenes()
    {
        EditorSyncAsset();
        if (string.IsNullOrWhiteSpace(scenePath))
            return false;

        string normalizedPath = NormalizeSeparators(scenePath);
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.enabled && string.Equals(NormalizeSeparators(buildScene.path), normalizedPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static string EditorFindScenePath(string sceneKey)
    {
        string key = NormalizeSeparators(sceneKey);
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (key.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
            && AssetDatabase.LoadAssetAtPath<SceneAsset>(key) != null)
            return key;

        string wantedName = Path.GetFileNameWithoutExtension(key);
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.enabled && string.Equals(Path.GetFileNameWithoutExtension(buildScene.path), wantedName, StringComparison.OrdinalIgnoreCase))
                return buildScene.path;
        }

        string[] sceneGuids = AssetDatabase.FindAssets(wantedName + " t:SceneAsset");
        string firstMatch = string.Empty;

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetFileNameWithoutExtension(path), wantedName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(firstMatch))
                firstMatch = path;

            if (path.IndexOf("/For_game/", StringComparison.OrdinalIgnoreCase) >= 0)
                return path;
        }

        return firstMatch;
    }
#endif
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(S_SceneReference))]
public class S_SceneReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty assetProperty = property.FindPropertyRelative("sceneAsset");
        SerializedProperty pathProperty = property.FindPropertyRelative("scenePath");
        SerializedProperty nameProperty = property.FindPropertyRelative("sceneName");

        if (assetProperty == null)
        {
            EditorGUI.LabelField(position, label.text, "Scene references are editor-only.");
            return;
        }

        if (assetProperty.objectReferenceValue == null && pathProperty != null && !string.IsNullOrWhiteSpace(pathProperty.stringValue))
            assetProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathProperty.stringValue);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        UnityEngine.Object selected = EditorGUI.ObjectField(position, label, assetProperty.objectReferenceValue, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            assetProperty.objectReferenceValue = selected;
            string path = selected != null ? AssetDatabase.GetAssetPath(selected) : string.Empty;

            if (pathProperty != null)
                pathProperty.stringValue = path;

            if (nameProperty != null)
                nameProperty.stringValue = Path.GetFileNameWithoutExtension(path);
        }

        EditorGUI.EndProperty();
    }
}
#endif
