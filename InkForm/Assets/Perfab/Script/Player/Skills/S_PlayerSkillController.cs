using UnityEngine;
using UnityEngine.InputSystem;

public class S_PlayerSkillController : MonoBehaviour
{
    private S_Player player;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction cameraControlAction;
    private S_CameraMove cameraController;
    private S_PlayerProceduralRenderer proceduralRenderer;
    private S_PlayerDynamicCollider dynamicCollider;
    private GameObject body;
    private Rigidbody2D bodyRigidbody;
    private float solidGravityScale;
    private bool useDynamicCollider;

    private bool isSprintCharging;
    private float sprintChargeTimer;
    private int sprintChargeStage;
    private float chargeScaleMultiplier = 1f;
    private float chargeShakeTimer;
    private int previousChargeStage;
    private bool chargeRotationUnlocked;
    private PhysicsMaterial2D originalPhysicsMaterial;
    private S_Soild_sprint sprintSkill;
    private float sprintCooldownRemaining;
    private bool chargeVisualsActive;
    private AudioSource sprintChargeSource;

    private const string CameraLookInputLockId = "CameraLook";

    private bool isCameraControlActive;
    private S_CameraControlSkill cameraControlSkill;

    private InputAction jumpAction;
    private S_HookTentacleRenderer tentacleRenderer;
    private bool isHookActive;
    private S_HookSkill hookSkill;
    private S_Hook currentHook;
    private S_Hook promptedHook;
    private float ropeLength;
    private float hookGravityScaleBackup;

    public bool IsSprintCharging => isSprintCharging;
    public bool IsCameraControlActive => isCameraControlActive;
    public bool IsHookActive => isHookActive;

    public void Initialize(
        S_Player owner,
        InputAction move,
        InputAction sprint,
        InputAction cameraControl,
        S_CameraMove camera,
        S_PlayerProceduralRenderer renderer,
        S_PlayerDynamicCollider colliderController,
        GameObject bodyObject,
        Rigidbody2D rigidbody,
        float gravityScale,
        bool dynamicColliderEnabled,
        InputAction jump = null,
        S_HookTentacleRenderer tentacle = null)
    {
        player = owner;
        moveAction = move;
        sprintAction = sprint;
        cameraControlAction = cameraControl;
        cameraController = camera;
        proceduralRenderer = renderer;
        dynamicCollider = colliderController;
        body = bodyObject;
        bodyRigidbody = rigidbody;
        solidGravityScale = gravityScale;
        useDynamicCollider = dynamicColliderEnabled;
        jumpAction = jump;
        tentacleRenderer = tentacle;

        SetupSprintChargeAudioSource();
    }

    public void HandleCameraControlInput()
    {
        if (cameraControlAction == null || player == null)
            return;

        if (!isCameraControlActive && cameraControlAction.WasPerformedThisFrame())
        {
            S_CameraControlSkill skill = S_SkillTree.Instance != null
                ? S_SkillTree.Instance.GetCameraControlSkill()
                : null;

            if (skill != null)
                skill.Activate(player);
        }

        if (isCameraControlActive && cameraControlAction.WasReleasedThisFrame())
        {
            EndCameraControl();
        }
    }

    public void CameraControlTick()
    {
        if (cameraController == null)
            return;

        InputSystem_Actions.CameraActions camera = S_Input.Actions.Camera;
        Vector2 pan = camera.CameraPan.ReadValue<Vector2>();
        bool zoomIn = camera.ZoomIn.IsPressed();
        bool zoomOut = camera.ZoomOut.IsPressed();
        cameraController.LookTick(pan, zoomIn, zoomOut);
    }

    public void TickCooldown()
    {
        if (sprintCooldownRemaining > 0f)
            sprintCooldownRemaining -= Time.deltaTime;
    }

    public void BeginCameraControl(S_CameraControlSkill skill)
    {
        if (isCameraControlActive || skill == null || player == null)
            return;

        if (player.IsParalyzed || player.IsMovementLocked || isSprintCharging || Mathf.Approximately(Time.timeScale, 0f))
            return;

        if (player.Energy != null && !player.Energy.CanStartSkill(skill))
            return;

        cameraControlSkill = skill;
        isCameraControlActive = true;
        player.ClearGripState();

        if (cameraController == null)
            cameraController = FindAnyObjectByType<S_CameraMove>();

        // Lock the player's gameplay input so the look-mode keys only drive the camera.
        S_GameEvent.PushGameplayInputLock(CameraLookInputLockId);

        if (cameraController != null)
            cameraController.BeginManualControl();
    }

    public void EndCameraControl()
    {
        if (!isCameraControlActive)
            return;

        isCameraControlActive = false;

        S_GameEvent.PopGameplayInputLock(CameraLookInputLockId);

        if (cameraController != null)
            cameraController.EndManualControl();

        if (player != null && player.Energy != null)
            player.Energy.NotifySkillUseStopped();

        cameraControlSkill = null;
    }

    public void BeginSprintCharge()
    {
        if (isSprintCharging || player == null)
            return;

        if (player.IsParalyzed)
            return;

        sprintSkill = S_SkillTree.Instance != null ? S_SkillTree.Instance.GetSprintSkill() : null;
        if (sprintSkill == null)
            return;

        if (!sprintSkill.availableSolid && !player.IsFluidForm)
            return;

        if (!sprintSkill.availableFluid && player.IsFluidForm)
            return;

        if (player.Energy != null && !player.Energy.CanStartSkill(sprintSkill))
            return;

        isSprintCharging = true;
        sprintChargeTimer = 0f;
        sprintChargeStage = 0;
        previousChargeStage = 0;
        chargeShakeTimer = 0f;
        chargeScaleMultiplier = 1f;
        chargeVisualsActive = false;
    }

    public void FixedTickSprintCharge()
    {
        if (!isSprintCharging || sprintSkill == null || bodyRigidbody == null)
            return;

        sprintChargeTimer += Time.fixedDeltaTime;

        if (player != null
            && player.Energy != null
            && !player.Energy.TryConsumeSkillEnergy(sprintSkill, Time.fixedDeltaTime))
        {
            CancelSprintCharge();
            return;
        }

        if (!chargeVisualsActive && sprintChargeTimer >= sprintSkill.BufferTime)
        {
            chargeVisualsActive = true;
            originalPhysicsMaterial = bodyRigidbody.sharedMaterial;
            if (sprintSkill.ChargeBallMaterial != null)
                bodyRigidbody.sharedMaterial = sprintSkill.ChargeBallMaterial;

            bodyRigidbody.freezeRotation = false;
            chargeRotationUnlocked = true;

            if (proceduralRenderer != null)
                proceduralRenderer.SetChargeOverride(true);

            S_GameEvent.PlaySFX(sprintSkill.ChargeStartClip);
            PlaySprintChargeStageSfx(0);
        }

        if (!chargeVisualsActive)
            return;

        sprintChargeStage = sprintSkill.GetStage(sprintChargeTimer);

        if (sprintChargeStage != previousChargeStage)
        {
            chargeShakeTimer = 0f;
            previousChargeStage = sprintChargeStage;
            PlaySprintChargeStageSfx(sprintChargeStage);
        }

        chargeShakeTimer += Time.fixedDeltaTime;
        float baseScale = sprintSkill.GetStageScale(sprintChargeTimer);
        float shakeOffset = sprintSkill.GetShakeOffset(chargeShakeTimer);
        chargeScaleMultiplier = baseScale + shakeOffset;

        if (useDynamicCollider && dynamicCollider != null)
            dynamicCollider.SetChargeOverride(true, chargeScaleMultiplier);

        bodyRigidbody.gravityScale = solidGravityScale;
    }

    public void ReleaseSprintCharge()
    {
        if (!isSprintCharging || sprintSkill == null || player == null)
            return;

        float releaseDirection = player.FacingRight ? 1f : -1f;

        if (!chargeVisualsActive)
        {
            if (player.Energy != null && !player.Energy.TrySpendAmount(sprintSkill.QuickTapEnergyCost))
            {
                CancelSprintCharge();
                return;
            }

            sprintCooldownRemaining = 0f;
            StopSprintChargeSfx();
            sprintSkill.ActivateCharge(player, sprintSkill.MinSprintSpeed, releaseDirection);
            PlaySprintReleaseSfx();
            isSprintCharging = false;
            sprintChargeTimer = 0f;
            player.Energy?.NotifySkillUseStopped();
            return;
        }

        float charge01 = Mathf.Clamp01(sprintChargeTimer / sprintSkill.MaxChargeTime);
        float finalSpeed = Mathf.Lerp(sprintSkill.MinSprintSpeed, sprintSkill.MaxSprintSpeed, charge01);

        sprintCooldownRemaining = 0f;
        StopSprintChargeSfx();
        sprintSkill.ActivateCharge(player, finalSpeed, releaseDirection);
        PlaySprintReleaseSfx();

        isSprintCharging = false;
        chargeVisualsActive = false;
        chargeScaleMultiplier = 1f;
        sprintChargeTimer = 0f;
        sprintChargeStage = 0;
        chargeShakeTimer = 0f;

        RestoreChargeVisuals();
        player.Energy?.NotifySkillUseStopped();
    }

    public void CancelSprintCharge()
    {
        if (!isSprintCharging && !chargeVisualsActive)
            return;

        bool restoreChargeVisuals = chargeVisualsActive;

        StopSprintChargeSfx();
        isSprintCharging = false;
        chargeVisualsActive = false;
        chargeScaleMultiplier = 1f;
        sprintChargeTimer = 0f;
        sprintChargeStage = 0;
        previousChargeStage = 0;
        chargeShakeTimer = 0f;

        if (restoreChargeVisuals)
            RestoreChargeVisuals();

        if (player != null && player.Energy != null)
            player.Energy.NotifySkillUseStopped();
    }

    public bool SprintReleasedThisFrame()
    {
        return sprintAction != null && sprintAction.WasReleasedThisFrame();
    }

    private void RestoreChargeVisuals()
    {
        if (bodyRigidbody != null && chargeRotationUnlocked)
        {
            bodyRigidbody.freezeRotation = true;
            bodyRigidbody.rotation = 0f;
            chargeRotationUnlocked = false;
        }

        if (bodyRigidbody != null)
            bodyRigidbody.sharedMaterial = originalPhysicsMaterial;

        if (proceduralRenderer != null)
            proceduralRenderer.SetChargeOverride(false);

        if (useDynamicCollider && dynamicCollider != null)
            dynamicCollider.SetChargeOverride(false, 1f);
    }

    private void PlaySprintChargeStageSfx(int stage)
    {
        if (sprintSkill == null || sprintChargeSource == null)
            return;

        AudioClip clip = sprintSkill.GetChargeStageClip(stage);
        if (clip == null)
            return;

        float pitch = sprintSkill.GetChargeStagePitch(stage);
        StopSprintChargeSfx();

        sprintChargeSource.clip = clip;
        sprintChargeSource.pitch = Mathf.Max(0.01f, pitch);
        sprintChargeSource.loop = true;
        sprintChargeSource.Play();
    }

    private void PlaySprintReleaseSfx()
    {
        if (sprintSkill == null)
            return;

        S_GameEvent.PlaySFX(sprintSkill.GetChargeReleaseClip());
    }

    private void StopSprintChargeSfx()
    {
        if (sprintChargeSource == null)
            return;

        if (sprintChargeSource.isPlaying)
            sprintChargeSource.Stop();

        sprintChargeSource.clip = null;
        sprintChargeSource.pitch = 1f;
    }

    private void SetupSprintChargeAudioSource()
    {
        if (sprintChargeSource != null)
            return;

        GameObject sourceHost = body != null ? body : gameObject;
        sprintChargeSource = sourceHost.AddComponent<AudioSource>();
        sprintChargeSource.playOnAwake = false;
        sprintChargeSource.loop = true;
        sprintChargeSource.spatialBlend = 0f;
        sprintChargeSource.pitch = 1f;
    }

    // ──────────────────────────────────────────────
    //  Hook (grappling tentacle) skill
    // ──────────────────────────────────────────────

    /// <summary>Update() hook: scan/select anchors and show the prompt, or detach on jump.</summary>
    public void HandleHookInput()
    {
        if (player == null)
            return;

        if (isHookActive)
        {
            // Jump releases the hook with the current swing momentum.
            if (jumpAction != null && jumpAction.WasPerformedThisFrame())
                DetachHook(true);
            return;
        }

        // Only selectable in fluid form, when nothing else owns the player.
        if (!CanStartHook())
        {
            SetPromptedHook(null);
            return;
        }

        if (hookSkill == null && S_SkillTree.Instance != null)
            hookSkill = S_SkillTree.Instance.GetHookSkill();

        if (hookSkill == null)
        {
            SetPromptedHook(null);
            return;
        }

        S_Hook best = FindBestHook(hookSkill.DetectionRadius);
        SetPromptedHook(best);

        if (best == null)
            return;

        if (S_PlayerInteractInput.WasPressedThisFrame()
            && (player.Energy == null || player.Energy.CanStartSkill(hookSkill)))
        {
            BeginHook(best);
        }
    }

    private bool CanStartHook()
    {
        if (!player.IsFluidForm || player.IsParalyzed || player.IsMovementLocked)
            return false;

        if (isCameraControlActive || isSprintCharging)
            return false;

        return S_SkillTree.Instance == null || S_SkillTree.Instance.IsUnlocked("Hook");
    }

    private S_Hook FindBestHook(float radius)
    {
        Vector2 origin = bodyRigidbody != null ? bodyRigidbody.position : (Vector2)player.BodyTransform.position;
        float facing = player.FacingRight ? 1f : -1f;
        float radiusSqr = radius * radius;

        S_Hook best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < S_Hook.All.Count; i++)
        {
            S_Hook hook = S_Hook.All[i];
            if (hook == null)
                continue;

            Vector2 toHook = hook.AnchorPosition - origin;
            float distSqr = toHook.sqrMagnitude;
            if (distSqr > radiusSqr || distSqr < 0.0001f)
                continue;

            float dist = Mathf.Sqrt(distSqr);
            Vector2 dir = toHook / dist;
            // Prefer hooks aligned with the facing direction, then closer ones.
            float alignment = dir.x * facing;            // -1..1
            float score = alignment - dist / radius;     // facing dominates, distance breaks ties
            if (score > bestScore)
            {
                bestScore = score;
                best = hook;
            }
        }

        return best;
    }

    private void SetPromptedHook(S_Hook hook)
    {
        if (promptedHook == hook)
            return;

        if (promptedHook != null)
            promptedHook.SetPromptVisible(false);

        promptedHook = hook;

        if (promptedHook != null)
            promptedHook.SetPromptVisible(true);
    }

    private void BeginHook(S_Hook hook)
    {
        if (hook == null || bodyRigidbody == null)
            return;

        currentHook = hook;
        isHookActive = true;

        Vector2 pos = bodyRigidbody.position;
        float dist = Vector2.Distance(pos, hook.AnchorPosition);
        ropeLength = Mathf.Clamp(dist, hookSkill.MinRopeLength, hookSkill.MaxRopeLength);

        player.ClearGripState();
        SetPromptedHook(null);

        hookGravityScaleBackup = bodyRigidbody.gravityScale;

        if (tentacleRenderer != null)
            tentacleRenderer.SetActive(true);
    }

    /// <summary>FixedUpdate hook: pendulum + rope shortening + energy drain.</summary>
    public void FixedTickHook()
    {
        if (!isHookActive || bodyRigidbody == null || hookSkill == null || currentHook == null)
            return;

        float dt = Time.fixedDeltaTime;

        if (player.Energy != null && !player.Energy.TryConsumeSkillEnergy(hookSkill, dt))
        {
            DetachHook(false);
            return;
        }

        // Rope steadily shortens so the player rises toward the hook.
        ropeLength = Mathf.MoveTowards(ropeLength, hookSkill.MinRopeLength, hookSkill.RiseSpeed * dt);

        Vector2 anchor = currentHook.AnchorPosition;
        Vector2 pos = bodyRigidbody.position;
        Vector2 toPlayer = pos - anchor;
        float dist = toPlayer.magnitude;
        if (dist < 0.0001f)
        {
            DetachHook(false);
            return;
        }

        Vector2 radial = toPlayer / dist;
        Vector2 tangent = new Vector2(-radial.y, radial.x);

        Vector2 v = bodyRigidbody.linearVelocity;
        // Constrain to the circle: remove the radial velocity component (gravity stays tangential).
        v -= radial * Vector2.Dot(v, radial);
        // Move keys push the swing along the tangent.
        float moveX = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
        v += tangent * (moveX * hookSkill.SwingAccel * dt);
        v = Vector2.ClampMagnitude(v, hookSkill.MaxSwingSpeed);
        bodyRigidbody.linearVelocity = v;

        // Hard length constraint (pulls the player inward as the rope shortens).
        bodyRigidbody.position = anchor + radial * ropeLength;

        if (ropeLength <= hookSkill.MinRopeLength + 0.001f)
            DetachHook(false);
    }

    private void DetachHook(bool launched)
    {
        if (!isHookActive)
            return;

        isHookActive = false;

        if (bodyRigidbody != null)
        {
            bodyRigidbody.gravityScale = hookGravityScaleBackup > 0f ? hookGravityScaleBackup : solidGravityScale;
            if (launched && hookSkill != null)
                bodyRigidbody.linearVelocity += Vector2.up * hookSkill.JumpOffBoost;
        }

        if (tentacleRenderer != null)
            tentacleRenderer.SetActive(false);

        if (player != null && player.Energy != null)
            player.Energy.NotifySkillUseStopped();

        // Preserve the swing momentum so the player flies out instead of stopping mid-air.
        if (player != null)
            player.BeginHookLaunchMomentum();

        currentHook = null;
    }

    /// <summary>LateUpdate hook: draw the tentacle while attached.</summary>
    public void HookRenderTick()
    {
        if (!isHookActive || tentacleRenderer == null || currentHook == null || bodyRigidbody == null)
            return;

        float swingSpeed = bodyRigidbody.linearVelocity.magnitude;
        tentacleRenderer.RenderTick(bodyRigidbody.position, currentHook.AnchorPosition, swingSpeed);
    }

    /// <summary>Called when the player cancels all active skills (form switch, scene change, etc.).</summary>
    public void CancelHook()
    {
        SetPromptedHook(null);
        if (isHookActive)
            DetachHook(false);
    }
}
