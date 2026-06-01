using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to a trigger collider on any object (cabinets, pillars, machines).
/// The player can toggle hiding while inside the trigger, becoming invisible to NPC detection.
/// </summary>
public class S_HideSpot : MonoBehaviour
{
    private enum HideRenderDepth
    {
        Front,
        Behind
    }

    [Header("Hide Settings")]
    [SerializeField] private Vector2 hideOffset = Vector2.zero;
    [SerializeField] private Vector2 exitOffset = new Vector2(1f, 0f);

    [Header("Render Depth")]
    [SerializeField] private HideRenderDepth hideRenderDepth = HideRenderDepth.Front;
    [SerializeField, Min(1)] private int frontSortingGap = 1;
    [SerializeField, Min(1)] private int backSortingGap = 1;

    private struct RendererState
    {
        public Renderer renderer;
        public int sortingLayerID;
        public int sortingOrder;
    }

    private bool isHiding = false;
    private bool playerInRange = false;

    private IPlayerActor player;
    private Rigidbody2D playerRig;
    private Transform playerBody;
    private Renderer hideSpotRenderer;
    private RendererState[] rendererStates;

    private InputAction hideAction;

    private bool fallbackRigidBodyLocked = false;
    private float fallbackGravityScale;
    private RigidbodyConstraints2D fallbackConstraints;
    private float hiddenPlayerY;

    private void Awake()
    {
        CacheHideSpotRenderer();
    }

    private void OnEnable()
    {
        EnsureHideAction();
        S_GameEvent.OnRespawnRequested += HandleRespawnRequested;
    }

    private void Update()
    {
        EnsureHideAction();

        if (isHiding)
        {
            MaintainHiddenState();
            S_GameEvent.HiddenSuspicionDecayRequested(Time.deltaTime);
        }

        if ((playerInRange || isHiding) && HidePressedThisFrame())
        {
            if (isHiding)
                ExitHide();
            else
                EnterHide();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerInRange = true;
        CachePlayerComponents(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerInRange = true;

        if (player == null || playerBody == null)
            CachePlayerComponents(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (isHiding)
            return;

        playerInRange = false;
        ClearPlayerCache();
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        return S_PlayerLookup.IsPlayer(other);
    }

    private void CachePlayerComponents(Collider2D playerCollider)
    {
        if (S_PlayerLookup.TryGet(playerCollider, out player))
        {
            playerRig = player.Rigidbody;
            playerBody = player.BodyTransform;
            return;
        }

        playerRig = playerCollider.attachedRigidbody;
        playerBody = playerRig != null ? playerRig.transform : playerCollider.transform;
    }

    private void ClearPlayerCache()
    {
        player = null;
        playerRig = null;
        playerBody = null;
        rendererStates = null;
    }

    private void EnterHide()
    {
        if (isHiding || playerBody == null)
            return;

        isHiding = true;
        S_GameEvent.PlayerHiddenChangeRequested(true);
        hiddenPlayerY = playerBody.position.y;

        CacheRendererStates();
        MovePlayerTo(GetHidePosition());
        LockPlayerMovement();
        ApplyHiddenSorting();
    }

    private void ExitHide(bool applyExitOffset = true)
    {
        if (!isHiding)
            return;

        isHiding = false;
        S_GameEvent.PlayerHiddenChangeRequested(false);

        UnlockPlayerMovement();
        RestoreRendererSorting();

        if (applyExitOffset && playerBody != null)
            MovePlayerTo((Vector2)playerBody.position + exitOffset);
    }

    private void MaintainHiddenState()
    {
        MovePlayerTo(GetHidePosition());
        ApplyHiddenSorting();

        if (player != null)
            player.SetMovementLocked(true);
        else if (playerRig != null)
        {
            playerRig.linearVelocity = Vector2.zero;
            playerRig.angularVelocity = 0f;
            playerRig.gravityScale = 0f;
        }
    }

    private Vector2 GetHidePosition()
    {
        // Hide spots can pull the player horizontally into cover, but should
        // not change the player's height and leave them hanging in the air.
        return new Vector2(transform.position.x + hideOffset.x, hiddenPlayerY);
    }

    private void MovePlayerTo(Vector2 position)
    {
        if (player != null)
        {
            player.Teleport(position);
            return;
        }

        if (playerRig != null)
        {
            playerRig.linearVelocity = Vector2.zero;
            playerRig.angularVelocity = 0f;
            playerRig.position = position;
            return;
        }

        if (playerBody != null)
            playerBody.position = position;
    }

    private void LockPlayerMovement()
    {
        if (player != null)
        {
            player.SetMovementLocked(true);
            return;
        }

        if (playerRig == null || fallbackRigidBodyLocked)
            return;

        fallbackRigidBodyLocked = true;
        fallbackGravityScale = playerRig.gravityScale;
        fallbackConstraints = playerRig.constraints;

        playerRig.linearVelocity = Vector2.zero;
        playerRig.angularVelocity = 0f;
        playerRig.gravityScale = 0f;
        playerRig.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private void UnlockPlayerMovement()
    {
        if (player != null)
        {
            player.SetMovementLocked(false);
            return;
        }

        if (playerRig == null || !fallbackRigidBodyLocked)
            return;

        playerRig.constraints = fallbackConstraints;
        playerRig.gravityScale = fallbackGravityScale;
        fallbackRigidBodyLocked = false;
    }

    private void CacheRendererStates()
    {
        if (playerBody == null)
        {
            rendererStates = null;
            return;
        }

        Renderer[] renderers = playerBody.GetComponentsInChildren<Renderer>(true);
        rendererStates = new RendererState[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = new RendererState
            {
                renderer = renderers[i],
                sortingLayerID = renderers[i].sortingLayerID,
                sortingOrder = renderers[i].sortingOrder,
            };
        }
    }

    private void ApplyHiddenSorting()
    {
        if (rendererStates == null || rendererStates.Length == 0)
            return;

        Renderer spotRenderer = CacheHideSpotRenderer();
        if (spotRenderer == null)
            return;

        int minOrder = int.MaxValue;
        int maxOrder = int.MinValue;

        for (int i = 0; i < rendererStates.Length; i++)
        {
            if (rendererStates[i].renderer == null)
                continue;

            minOrder = Mathf.Min(minOrder, rendererStates[i].sortingOrder);
            maxOrder = Mathf.Max(maxOrder, rendererStates[i].sortingOrder);
        }

        if (minOrder == int.MaxValue || maxOrder == int.MinValue)
            return;

        bool shouldHideInFront = hideRenderDepth == HideRenderDepth.Front;
        int spotOrder = spotRenderer.sortingOrder;

        for (int i = 0; i < rendererStates.Length; i++)
        {
            Renderer targetRenderer = rendererStates[i].renderer;
            if (targetRenderer == null)
                continue;

            targetRenderer.sortingLayerID = spotRenderer.sortingLayerID;
            targetRenderer.sortingOrder = shouldHideInFront
                ? spotOrder + frontSortingGap + (rendererStates[i].sortingOrder - minOrder)
                : spotOrder - backSortingGap - (maxOrder - rendererStates[i].sortingOrder);
        }
    }

    private void RestoreRendererSorting()
    {
        if (rendererStates == null)
            return;

        for (int i = 0; i < rendererStates.Length; i++)
        {
            Renderer targetRenderer = rendererStates[i].renderer;
            if (targetRenderer == null)
                continue;

            targetRenderer.sortingLayerID = rendererStates[i].sortingLayerID;
            targetRenderer.sortingOrder = rendererStates[i].sortingOrder;
        }
    }

    private Renderer CacheHideSpotRenderer()
    {
        if (hideSpotRenderer != null)
            return hideSpotRenderer;

        hideSpotRenderer = GetComponent<Renderer>();
        if (hideSpotRenderer == null)
            hideSpotRenderer = GetComponentInChildren<Renderer>();

        return hideSpotRenderer;
    }

    private void EnsureHideAction()
    {
        if (hideAction != null)
            return;

        if (!S_InputBindingManager.TryGetExisting(out S_InputBindingManager inputManager))
            return;

        hideAction = inputManager.Actions.Player.Hide;
    }

    private bool HidePressedThisFrame()
    {
        if (hideAction != null && hideAction.WasPerformedThisFrame())
            return true;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;

        return Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
    }

    private void OnValidate()
    {
        frontSortingGap = Mathf.Max(1, frontSortingGap);
        backSortingGap = Mathf.Max(1, backSortingGap);
    }

    private void OnDisable()
    {
        S_GameEvent.OnRespawnRequested -= HandleRespawnRequested;

        if (isHiding)
            ExitHide();
    }

    private void HandleRespawnRequested()
    {
        if (isHiding)
            ExitHide(false);

        playerInRange = false;
        ClearPlayerCache();
    }
}
