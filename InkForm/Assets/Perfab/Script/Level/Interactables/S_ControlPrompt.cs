using System.Collections;
using UnityEngine;

/// <summary>
/// Mountable "control challenge" zone. When the player enters this object's 2D trigger, a
/// side dialog slides in (the shared S_UITutorialPrompt) showing an inspector-authored title,
/// description and required-action checklist. Once the player performs every required action
/// (or skips), a configurable S_GameEvent is fired.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class S_ControlPrompt : MonoBehaviour
{
    [Header("Dialog")]
    [SerializeField] private string promptTitle = "Controls";
    [TextArea(2, 6)]
    [SerializeField] private string promptDescription = "WASD / Arrow Keys - Move\nSpace - Jump";
    [Tooltip("Actions the player must perform before the dialog dismisses. Leave empty to dismiss on any key.")]
    [SerializeField] private TutorialRequiredAction[] requiredActions;

    [Header("Activation")]
    [Tooltip("Fire only the first time the player completes the challenge.")]
    [SerializeField] private bool fireOnce = true;
    [Tooltip("Optional explicit prompt. Leave empty to find the scene's S_UITutorialPrompt automatically.")]
    [SerializeField] private S_UITutorialPrompt promptOverride;

    [Header("On Complete")]
    [SerializeField] private S_GameEventInvoker onComplete = new S_GameEventInvoker();

    private bool running;
    private bool completed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (running || (fireOnce && completed))
            return;

        if (S_PlayerLookup.IsPlayer(other))
            StartCoroutine(RunChallenge());
    }

    private IEnumerator RunChallenge()
    {
        S_UITutorialPrompt prompt = promptOverride != null
            ? promptOverride
            : FindAnyObjectByType<S_UITutorialPrompt>();

        if (prompt == null)
        {
            Debug.LogError("[ControlPrompt] No S_UITutorialPrompt found. Add Pre_TutorialController to the scene or assign Prompt Override.", this);
            yield break;
        }

        running = true;

        if (requiredActions != null && requiredActions.Length > 0)
            yield return prompt.ShowAndWaitForAllInputs(promptTitle, promptDescription, requiredActions);
        else
            yield return prompt.ShowAndWaitForInput(promptTitle, promptDescription);

        running = false;
        completed = true;

        onComplete.Invoke();
    }
}
