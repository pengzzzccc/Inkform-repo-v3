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

    private bool isCameraControlActive;
    private S_CameraControlSkill cameraControlSkill;
    private float timeScaleBeforeCameraControl = 1f;
    private float fixedDeltaTimeBeforeCameraControl = 0.02f;

    public bool IsSprintCharging => isSprintCharging;
    public bool IsCameraControlActive => isCameraControlActive;

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
        bool dynamicColliderEnabled)
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
        if (cameraController != null && moveAction != null)
            cameraController.ManualControlTick(moveAction.ReadValue<Vector2>());
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

        cameraControlSkill = skill;
        isCameraControlActive = true;
        player.ClearGripState();

        if (cameraController == null)
            cameraController = FindAnyObjectByType<S_CameraMove>();

        timeScaleBeforeCameraControl = Time.timeScale;
        fixedDeltaTimeBeforeCameraControl = Time.fixedDeltaTime;

        float bulletScale = cameraControlSkill.BulletTimeScale;
        Time.timeScale = timeScaleBeforeCameraControl * bulletScale;
        Time.fixedDeltaTime = fixedDeltaTimeBeforeCameraControl * bulletScale;

        if (cameraController != null)
            cameraController.BeginManualControl();
    }

    public void EndCameraControl()
    {
        if (!isCameraControlActive)
            return;

        isCameraControlActive = false;

        Time.timeScale = timeScaleBeforeCameraControl;
        Time.fixedDeltaTime = fixedDeltaTimeBeforeCameraControl;

        if (cameraController != null)
            cameraController.EndManualControl();

        cameraControlSkill = null;
    }

    public void BeginSprintCharge()
    {
        if (isSprintCharging || player == null)
            return;

        if (player.IsParalyzed)
            return;

        if (sprintCooldownRemaining > 0f)
            return;

        sprintSkill = S_SkillTree.Instance != null ? S_SkillTree.Instance.GetSprintSkill() : null;
        if (sprintSkill == null)
            return;

        if (!sprintSkill.availableSolid && !player.IsFluidForm)
            return;

        if (!sprintSkill.availableFluid && player.IsFluidForm)
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
            sprintCooldownRemaining = sprintSkill.GetCooldown(0);
            StopSprintChargeSfx();
            sprintSkill.ActivateCharge(player, sprintSkill.MinSprintSpeed, releaseDirection);
            PlaySprintReleaseSfx();
            isSprintCharging = false;
            sprintChargeTimer = 0f;
            return;
        }

        float charge01 = Mathf.Clamp01(sprintChargeTimer / sprintSkill.MaxChargeTime);
        float finalSpeed = Mathf.Lerp(sprintSkill.MinSprintSpeed, sprintSkill.MaxSprintSpeed, charge01);

        sprintCooldownRemaining = sprintSkill.GetCooldown(sprintChargeStage);
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
}
