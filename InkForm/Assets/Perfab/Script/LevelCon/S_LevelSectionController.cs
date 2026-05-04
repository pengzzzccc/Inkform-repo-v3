using UnityEngine;

public class S_LevelSectionController : MonoBehaviour
{
    [SerializeField] private S_LevelSection[] sections;

    private int currentSectionIndex = 0;

    void Start()
    {
        if (sections == null || sections.Length == 0) return;

        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i] == null) continue;
            sections[i].HideSection();
        }

        currentSectionIndex = 0;
    }

    void OnEnable()
    {
        S_GameEvent.OnSectionStart += HandleSectionStart;
        S_GameEvent.OnSectionEnd += HandleSectionEnd;
    }

    void OnDisable()
    {
        S_GameEvent.OnSectionStart -= HandleSectionStart;
        S_GameEvent.OnSectionEnd -= HandleSectionEnd;
    }

    void HandleSectionStart(int index)
    {
        if (index != currentSectionIndex) return;
        if (sections == null || currentSectionIndex >= sections.Length) return;

        sections[index].RevealSection();
    }

    void HandleSectionEnd(int index)
    {
        if (index != currentSectionIndex) return;
        if (sections == null || currentSectionIndex >= sections.Length) return;

        sections[index].HideSection();
        sections[index].MarkCompleted();

        currentSectionIndex++;

        if (currentSectionIndex < sections.Length)
        {
            sections[currentSectionIndex].RevealSection();
        }
    }

    public int GetCurrentSectionIndex() => currentSectionIndex;

    public int GetTotalSections() => sections != null ? sections.Length : 0;
}