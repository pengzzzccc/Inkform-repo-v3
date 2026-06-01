using UnityEngine;

public enum SectionTriggerType
{
    Start,
    End
}

public class S_SectionGoal : MonoBehaviour
{
    [SerializeField] private int sectionIndex = 0;
    [SerializeField] private SectionTriggerType triggerType = SectionTriggerType.Start;
    [SerializeField] private bool completeLevelOnEnd = false;

    private Vector3 fixedWorldPos;

    void Start()
    {
        fixedWorldPos = transform.position;
    }

    void LateUpdate()
    {
        if (transform.position != fixedWorldPos)
            transform.position = fixedWorldPos;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!S_PlayerLookup.IsPlayer(collision)) return;

        if (triggerType == SectionTriggerType.Start)
        {
            S_GameEvent.SectionStart(sectionIndex);
        }
        else
        {
            S_GameEvent.SectionEnd(sectionIndex);
            if (completeLevelOnEnd)
                S_GameEvent.LevelCompleted(S_LevelCompletionReason.SectionEnd);
        }
    }
}
