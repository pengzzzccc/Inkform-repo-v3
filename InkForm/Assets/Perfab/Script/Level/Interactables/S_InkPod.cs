using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ink Pod, a cylindrical maintenance chamber for InkForm units.
/// Spawn mode opens the door, drains fluid, walks the player out, then restores input.
/// Entry mode detects the player, walks them in, closes the door, fills fluid, then completes the level.
/// </summary>
[DefaultExecutionOrder(-50)]
public class S_InkPod : MonoBehaviour
{
    private const string InkPodInputLockId = "InkPod";

    public enum PodMode { Spawn, Entry }
    private static bool sceneHookRegistered;

    [Header("Mode")]
    [SerializeField] private PodMode mode = PodMode.Spawn;

    [Header("Walk Settings")]
    [SerializeField, Min(0.1f)] private float walkDistance = 2f;
    [SerializeField, Min(0.1f)] private float walkSpeed = 3f;

    [Header("Pod Points")]
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform entryPoint;

    [Header("Door")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float doorOpenOffsetY = 1.5f;
    [SerializeField, Min(0.1f)] private float doorOpenTime = 1.0f;
    [SerializeField, Min(0.1f)] private float doorCloseTime = 0.8f;

    [Header("Ink Fluid")]
    [SerializeField] private SpriteRenderer inkFluid;
    [SerializeField, Min(0.1f)] private float fluidAnimTime = 0.8f;

    [Header("Entry Detection")]
    [SerializeField, Min(0.1f)] private float entryTriggerRadius = 1.2f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    private Vector3 doorClosedPos;
    private bool isRunning;
    private string InputLockId => $"{InkPodInputLockId}:{GetInstanceID()}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        sceneHookRegistered = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (!sceneHookRegistered)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHookRegistered = true;
        }

        WarnIfPlayableSceneHasNoSpawnPod(SceneManager.GetActiveScene());
    }

    private void Awake()
    {
        if (doorTransform != null)
            doorClosedPos = doorTransform.localPosition;
    }

    private void Start()
    {
        if (mode == PodMode.Spawn)
        {
            if (ShouldRunSpawnSequence())
                StartCoroutine(SpawnSequence());
        }
        else
        {
            if (doorTransform != null)
                doorTransform.localPosition = doorClosedPos + Vector3.up * doorOpenOffsetY;
            if (inkFluid != null)
                SetFluidFill(0f);
        }
    }

    private void OnDisable()
    {
        S_GameEvent.PopGameplayInputLock(InputLockId);
    }

    private void Update()
    {
        if (mode != PodMode.Entry || isRunning)
            return;

        if (!S_PlayerLookup.TryGetActive(out IPlayerActor player))
            return;

        float dist = Vector2.Distance(transform.position, player.BodyTransform.position);
        if (dist <= entryTriggerRadius)
        {
            isRunning = true;
            StartCoroutine(EntrySequence());
        }
    }

    private IEnumerator SpawnSequence()
    {
        isRunning = true;
        S_GameEvent.PushGameplayInputLock(InputLockId);
        try
        {
            IPlayerActor player = null;
            while (!TryGetPlayer(out player, false))
                yield return null;

            player.Teleport(GetSpawnPosition());

            if (doorTransform != null)
                doorTransform.localPosition = doorClosedPos;
            if (inkFluid != null)
                SetFluidFill(1f);

            yield return AnimateDoor(doorClosedPos + Vector3.up * doorOpenOffsetY, doorOpenTime);
            yield return AnimateFluid(1f, 0f, fluidAnimTime);
            yield return WalkPlayerOut(player);
        }
        finally
        {
            isRunning = false;
            S_GameEvent.PopGameplayInputLock(InputLockId);
        }
    }

    private bool ShouldRunSpawnSequence()
    {
        S_RunFlowController runFlow = S_RunFlowController.Instance;
        if (runFlow == null || runFlow.Phase != S_RunFlowController.RunPhase.Facility)
            return true;

        return runFlow.TryConsumeInkPodSpawnForCurrentScene();
    }

    private IEnumerator EntrySequence()
    {
        isRunning = true;
        S_GameEvent.PushGameplayInputLock(InputLockId);
        try
        {
            yield return WalkPlayerIn();
            yield return AnimateDoor(doorClosedPos, doorCloseTime);
            yield return AnimateFluid(0f, 1f, fluidAnimTime);
            yield return new WaitForSecondsRealtime(0.3f);

            S_GameEvent.LevelCompleted(S_LevelCompletionReason.InkPodEntry);
        }
        finally
        {
            isRunning = false;
            S_GameEvent.PopGameplayInputLock(InputLockId);
        }
    }

    private IEnumerator WalkPlayerOut(IPlayerActor player)
    {
        if (player == null)
            yield break;

        Vector2 startPos = player.BodyTransform.position;
        Vector2 endPos = exitPoint != null
            ? (Vector2)exitPoint.position
            : startPos + (Vector2)(transform.right * walkDistance);

        yield return MovePlayerTo(player, startPos, endPos);
    }

    private IEnumerator WalkPlayerIn()
    {
        if (!TryGetPlayer(out IPlayerActor player, true))
            yield break;

        Vector2 startPos = player.BodyTransform.position;
        Vector2 endPos = entryPoint != null ? (Vector2)entryPoint.position : (Vector2)transform.position;

        yield return MovePlayerTo(player, startPos, endPos);
    }

    private bool TryGetPlayer(out IPlayerActor player, bool logWarnings)
    {
        if (!S_PlayerLookup.TryGetActive(out player))
        {
            if (logWarnings)
                Debug.LogWarning($"{nameof(S_InkPod)} on {name} could not find an active player.");
            return false;
        }

        if (player.Rigidbody == null)
        {
            if (logWarnings)
                Debug.LogWarning($"{nameof(S_InkPod)} on {name} found a player without a Rigidbody2D.");
            return false;
        }

        return true;
    }

    private Vector2 GetSpawnPosition()
    {
        return entryPoint != null ? (Vector2)entryPoint.position : (Vector2)transform.position;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WarnIfPlayableSceneHasNoSpawnPod(scene);
    }

    private static void WarnIfPlayableSceneHasNoSpawnPod(Scene scene)
    {
        if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
            return;

        bool sceneHasPlayer = false;
        S_Player[] players = FindObjectsByType<S_Player>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].gameObject.scene == scene)
            {
                sceneHasPlayer = true;
                break;
            }
        }

        if (!sceneHasPlayer)
            return;

        S_InkPod[] pods = FindObjectsByType<S_InkPod>(FindObjectsSortMode.None);
        for (int i = 0; i < pods.Length; i++)
        {
            if (pods[i] != null && pods[i].gameObject.scene == scene && pods[i].mode == PodMode.Spawn)
                return;
        }

        Debug.LogWarning($"[InkPod] Playable scene '{scene.name}' has a player but no Spawn InkPod. Player will keep the scene placement as fallback.");
    }

    private IEnumerator MovePlayerTo(IPlayerActor player, Vector2 startPos, Vector2 endPos)
    {
        float totalDist = Vector2.Distance(startPos, endPos);
        if (totalDist <= 0.01f)
        {
            player.Teleport(endPos);
            yield break;
        }

        float dist = 0f;
        while (dist < totalDist)
        {
            dist += walkSpeed * Time.deltaTime;
            float t = Mathf.Clamp01(dist / totalDist);
            player.Teleport(Vector2.Lerp(startPos, endPos, t));
            yield return null;
        }

        player.Teleport(endPos);
    }

    private IEnumerator AnimateDoor(Vector3 targetLocalPos, float duration)
    {
        if (doorTransform == null || duration <= 0f)
        {
            if (doorTransform != null)
                doorTransform.localPosition = targetLocalPos;
            yield break;
        }

        Vector3 startLocalPos = doorTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            doorTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        doorTransform.localPosition = targetLocalPos;
    }

    private IEnumerator AnimateFluid(float from, float to, float duration)
    {
        if (inkFluid == null || duration <= 0f)
        {
            SetFluidFill(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetFluidFill(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetFluidFill(to);
    }

    private void SetFluidFill(float normalized)
    {
        if (inkFluid == null)
            return;

        Vector3 scale = inkFluid.transform.localScale;
        scale.y = Mathf.Max(0.01f, normalized);
        inkFluid.transform.localScale = scale;

        Color c = inkFluid.color;
        c.a = normalized;
        inkFluid.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        if (mode == PodMode.Entry)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, entryTriggerRadius);
        }

        Gizmos.color = Color.cyan;
        Vector3 exitTarget = exitPoint != null ? exitPoint.position : transform.position + transform.right * walkDistance;
        Gizmos.DrawLine(transform.position, exitTarget);
        Gizmos.DrawSphere(exitTarget, 0.1f);

        if (entryPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(entryPoint.position, 0.15f);
        }
    }
}
