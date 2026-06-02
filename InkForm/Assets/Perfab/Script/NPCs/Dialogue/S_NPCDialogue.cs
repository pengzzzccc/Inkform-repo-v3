using UnityEngine;

/// <summary>
/// NPC with dialogue capabilities. Supports linear and branching dialogue.
/// Used for Ruth (Ch1 Branch1), Arthur (Ch3), and other story NPCs.
/// Branching mode changes dialogue flow based on collected documents and Branch 1/2 history.
///
/// Interaction: when the player is within interactRange and presses Interact, the lines
/// are shown through the shared S_DialogueUI dialogue box (which handles typing,
/// advancing, and the gameplay input lock).
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
        if (!isActive || !canInteract)
            return;

        if (S_DialogueUI.Instance != null && S_DialogueUI.Instance.IsActive)
            return;

        if (PlayerInRange() && S_PlayerInteractInput.WasPressedThisFrame())
            StartDialogue();
    }

    /// <summary>Start dialogue when the player interacts (also callable by a future interaction system).</summary>
    public override void OnInteract()
    {
        if (!canInteract || !isActive) return;
        base.OnInteract();
        StartDialogue();
    }

    /// <summary>Begin the dialogue sequence through the shared dialogue box.</summary>
    public void StartDialogue()
    {
        string[] lines = GetDialogueLines();
        if (lines == null || lines.Length == 0)
            return;

        if (S_DialogueUI.Instance != null && S_DialogueUI.Instance.IsActive)
            return;

        S_GameEvent.StoryTrigger("Dialogue_Start_" + npcID);
        S_DialogueUI.EnsureExists().Begin(
            npcName,
            lines,
            textSpeed,
            onFinished: () => S_GameEvent.StoryTrigger("Dialogue_End_" + npcID));
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
        if (triggerID == "Trigger_Dialogue_" + npcID)
            StartDialogue();
    }
}
