using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Minimal reusable dialogue box. The bottom panel (speaker name + typed-out body
/// line) is an authored prefab placed under ManagerRoot; advances on the player's
/// Interact input. Gameplay input is locked for the duration via the shared
/// input-lock events.
///
/// Singleton: callers use S_DialogueUI.EnsureExists().Begin(...). Driven by
/// S_NPCDialogue, but any system can show a linear dialogue through Begin().
/// </summary>
public class S_DialogueUI : MonoBehaviour
{
    private const string InputLockId = "Dialogue";

    public static S_DialogueUI Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] private float defaultTextSpeed = 0.04f;

    [Header("UI Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text bodyLabel;

    private Coroutine dialogueRoutine;
    private bool inputLockHeld;

    public bool IsActive { get; private set; }

    public static S_DialogueUI EnsureExists()
    {
        if (Instance != null)
            return Instance;

        S_DialogueUI found = FindAnyObjectByType<S_DialogueUI>(FindObjectsInactive.Include);
        if (found != null)
            return found;

        Debug.LogError("[DialogueUI] No S_DialogueUI found. Place the DialogueUI prefab under ManagerRoot.");
        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HidePanel();
    }

    private void OnDisable()
    {
        StopActiveDialogue();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Show a linear dialogue. Ignored if a dialogue is already running.</summary>
    public void Begin(string speaker, IReadOnlyList<string> lines, float textSpeed, Action onFinished = null)
    {
        if (IsActive || lines == null || lines.Count == 0)
            return;

        float speed = textSpeed > 0f ? textSpeed : defaultTextSpeed;
        dialogueRoutine = StartCoroutine(RunDialogue(speaker, lines, speed, onFinished));
    }

    private IEnumerator RunDialogue(string speaker, IReadOnlyList<string> lines, float textSpeed, Action onFinished)
    {
        IsActive = true;
        PushInputLock();

        if (speakerLabel != null)
            speakerLabel.text = speaker;

        if (panel != null)
            panel.SetActive(true);

        try
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i] ?? string.Empty;

                // Type the line out; pressing Interact mid-type reveals it instantly.
                bool revealed = false;
                if (bodyLabel != null)
                    bodyLabel.text = string.Empty;

                for (int c = 0; c < line.Length; c++)
                {
                    if (S_PlayerInteractInput.WasPressedThisFrame())
                    {
                        revealed = true;
                        break;
                    }

                    if (bodyLabel != null)
                        bodyLabel.text += line[c];

                    yield return new WaitForSecondsRealtime(textSpeed);
                }

                if (revealed && bodyLabel != null)
                    bodyLabel.text = line;

                // Wait for an Interact press to advance to the next line.
                // Skip the frame the line was just revealed on so the same press
                // does not both reveal and advance.
                if (revealed)
                    yield return null;

                while (!S_PlayerInteractInput.WasPressedThisFrame())
                    yield return null;
            }
        }
        finally
        {
            HidePanel();
            PopInputLock();
            IsActive = false;
            dialogueRoutine = null;
            onFinished?.Invoke();
        }
    }

    private void StopActiveDialogue()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }

        HidePanel();
        PopInputLock();
        IsActive = false;
    }

    private void PushInputLock()
    {
        if (inputLockHeld)
            return;

        inputLockHeld = true;
        S_GameEvent.PushGameplayInputLock(InputLockId);
    }

    private void PopInputLock()
    {
        if (!inputLockHeld)
            return;

        inputLockHeld = false;
        S_GameEvent.PopGameplayInputLock(InputLockId);
    }

    private void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);

        if (bodyLabel != null)
            bodyLabel.text = string.Empty;
    }

}
