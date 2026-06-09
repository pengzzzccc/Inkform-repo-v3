using System;
using UnityEngine;

/// <summary>
/// Serializable "fire one S_GameEvent" config, editable in the inspector. Pick a Kind and fill
/// the parameter it needs, then call Invoke(). StoryTrigger(stringParam) is the universal escape
/// hatch — anything listening to S_GameEvent.OnStoryTrigger can react to it. Extend Kind as needed.
/// </summary>
[Serializable]
public class S_GameEventInvoker
{
    public enum Kind
    {
        None,
        StoryTrigger,           // stringParam
        SceneLoadRequested,     // stringParam
        SkillUsed,              // stringParam
        NPCInteract,            // stringParam
        SectionStart,           // intParam
        SectionEnd,             // intParam
        LevelCompleted,
        EndingRequested,
        FacilityEntered,
        RunStartRequested,
        RespawnRequested,
        KeyCollected,
        ArrestTriggered,
        SuspicionResetRequested,
        TutorialPhaseChanged,
        OnZoneEntry
    }

    [Tooltip("Which S_GameEvent to fire when Invoke() is called.")]
    public Kind kind = Kind.StoryTrigger;

    [Tooltip("Used by StoryTrigger / SceneLoadRequested / SkillUsed / NPCInteract.")]
    public string stringParam;

    [Tooltip("Used by SectionStart / SectionEnd.")]
    public int intParam;

    public void Invoke()
    {
        switch (kind)
        {
            case Kind.None:
                break;
            case Kind.StoryTrigger:
                S_GameEvent.StoryTrigger(stringParam);
                break;
            case Kind.SceneLoadRequested:
                S_GameEvent.SceneLoadRequested(stringParam);
                break;
            case Kind.SkillUsed:
                S_GameEvent.SkillUsed(stringParam);
                break;
            case Kind.NPCInteract:
                S_GameEvent.NPCInteract(stringParam);
                break;
            case Kind.SectionStart:
                S_GameEvent.SectionStart(intParam);
                break;
            case Kind.SectionEnd:
                S_GameEvent.SectionEnd(intParam);
                break;
            case Kind.LevelCompleted:
                S_GameEvent.LevelCompleted();
                break;
            case Kind.EndingRequested:
                S_GameEvent.EndingRequested();
                break;
            case Kind.FacilityEntered:
                S_GameEvent.FacilityEntered();
                break;
            case Kind.RunStartRequested:
                S_GameEvent.RunStartRequested();
                break;
            case Kind.RespawnRequested:
                S_GameEvent.RespawnRequested();
                break;
            case Kind.KeyCollected:
                S_GameEvent.KeyCollected();
                break;
            case Kind.ArrestTriggered:
                S_GameEvent.ArrestTriggered();
                break;
            case Kind.SuspicionResetRequested:
                S_GameEvent.SuspicionResetRequested();
                break;
            case Kind.TutorialPhaseChanged:
                S_GameEvent.TutorialPhaseChanged();
                break;
            case Kind.OnZoneEntry:
                S_GameEvent.ZoneEntry();
                break;
        }
    }
}
