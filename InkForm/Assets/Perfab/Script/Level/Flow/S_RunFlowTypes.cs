using System;
using UnityEngine;

public enum S_LevelKind
{
    FixedTraining,
    RandomTraining,
    FacilityRoom,
    Ending
}

public enum S_LevelCompletionReason
{
    Goal,
    InkPodEntry,
    SectionEnd,
    ManualSceneTrigger
}

[Serializable]
public class S_LevelSceneEntry
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private S_LevelKind levelKind;
    [SerializeField] private S_SceneReference scene = new S_SceneReference();

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
    public S_LevelKind LevelKind => levelKind;
    public S_SceneReference Scene => scene;
    public string SceneKey => scene != null ? scene.RuntimeKey : string.Empty;
    public bool HasScene => scene != null && scene.IsValid;

    public bool MatchesScene(string sceneKey)
    {
        return scene != null && scene.Matches(sceneKey);
    }
}
