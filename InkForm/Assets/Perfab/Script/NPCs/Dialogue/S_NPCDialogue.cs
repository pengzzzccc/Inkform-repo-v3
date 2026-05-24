using UnityEngine;

/// <summary>
/// NPC with dialogue capabilities. Supports linear and branching dialogue.
/// Used for Ruth (Ch1 Branch1), Arthur (Ch3), and other story NPCs.
/// Branching mode changes dialogue flow based on collected documents and Branch 1/2 history.
/// </summary>
public class S_NPCDialogue : S_NPCbase
{
    [Header("Dialogue Mode")]
    [SerializeField] private DialogueMode mode = DialogueMode.Linear;

    [Header("Linear Dialogue")]
    [SerializeField] private string[] linearLines;

    [Header("Branching Dialogue")]
    [SerializeField] private string[] branchA_Lines;
    [SerializeField] private string[] branchB_Lines;

    [Header("Dialogue UI")]
    [SerializeField] private float textSpeed = 0.05f;

    private enum DialogueMode { Linear, Branching }
    private int currentLineIndex = 0;
    private bool dialogueActive = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        S_GameEvent.OnStoryTrigger += HandleStoryTrigger;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        S_GameEvent.OnStoryTrigger -= HandleStoryTrigger;
    }

    private void Update()
    {
        if (!isActive || !dialogueActive) return;
        // TODO: Handle dialogue advancement input (skip, next line)
    }

    /// <summary>Start dialogue when player interacts.</summary>
    public override void OnInteract()
    {
        if (!canInteract || !isActive) return;
        base.OnInteract();

        if (dialogueAsset != null)
        {
            // TODO: Parse and load dialogue from TextAsset
        }

        StartDialogue();
    }

    /// <summary>Begin the dialogue sequence.</summary>
    public void StartDialogue()
    {
        dialogueActive = true;
        currentLineIndex = 0;

        string[] lines = GetDialogueLines();
        if (lines == null || lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        // TODO: Show dialogue UI, display first line
        S_GameEvent.StoryTrigger("Dialogue_Start_" + npcID);
    }

    /// <summary>Advance to next line, or end if complete.</summary>
    public void AdvanceDialogue()
    {
        currentLineIndex++;
        string[] lines = GetDialogueLines();

        if (currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        // TODO: Display next line
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        currentLineIndex = 0;
        // TODO: Hide dialogue UI
        S_GameEvent.StoryTrigger("Dialogue_End_" + npcID);
    }

    /// <summary>Selects dialogue lines based on mode and game state.</summary>
    private string[] GetDialogueLines()
    {
        switch (mode)
        {
            case DialogueMode.Linear:
                return linearLines;

            case DialogueMode.Branching:
                // TODO: Check player's branch history and collected documents
                // Return branchA_Lines or branchB_Lines
                return branchA_Lines;

            default:
                return linearLines;
        }
    }

    private void HandleStoryTrigger(string triggerID)
    {
        // TODO: React to story triggers (e.g., start dialogue automatically)
        if (triggerID == "Trigger_Dialogue_" + npcID)
        {
            StartDialogue();
        }
    }
}