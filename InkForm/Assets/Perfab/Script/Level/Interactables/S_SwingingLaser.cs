using UnityEngine;

/// <summary>
/// Swinging laser hazard. Emits a beam from the nozzle that kills the player on contact.
/// The beam sweeps one way between angleA and angleB, waits a loop interval (beam off),
/// then restarts. Beam is clipped by blocking layers (terrain/player) and renders to the hit point.
/// A looping 3D hum is audible only when the player is near.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(AudioSource))]
public class S_SwingingLaser : MonoBehaviour
{
    public enum SwingMode
    {
        OneWayAtoB, // sweep A -> B, wait, repeat
        OneWayBtoA, // sweep B -> A, wait, repeat
        FullCycle   // sweep A -> B -> A, wait, repeat
    }

    [Header("Swing")]
    [SerializeField] private SwingMode swingMode = SwingMode.OneWayAtoB;
    [SerializeField] private float angleA = -45f;
    [SerializeField] private float angleB = 45f;
    [SerializeField, Min(0f)] private float swingSpeed = 60f; // degrees per second
    [SerializeField, Min(0f)] private float loopInterval = 1f; // wait between sweeps (beam off)
    [SerializeField] private bool autoStart = true;

    [Header("Beam")]
    [SerializeField, Min(0.1f)] private float maxLength = 30f;
    [SerializeField] private LayerMask blockingLayers; // terrain + player
    [SerializeField, Min(0.001f)] private float beamWidth = 0.15f;
    [SerializeField] private Color beamColor = new Color(1f, 0.2f, 0.2f, 0.85f);
    [SerializeField] private Material beamMaterial;
    [SerializeField] private string sortingLayerName = DefaultBeamSortingLayerName;
    [SerializeField] private int sortingOrder = 5;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioClip humLoop; // running hum, 3D distance-attenuated
    [SerializeField, Range(0f, 1f)] private float humVolume = 0.6f;
    [SerializeField, Min(0f)] private float humHearMinDistance = 3f;  // full volume within this
    [SerializeField, Min(0f)] private float humHearMaxDistance = 12f; // silent beyond this

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    private const int GizmoArcSegments = 16;
    private const string DefaultBeamSortingLayerName = "Mid";

    private static Material fallbackBeamMaterial;
    private LineRenderer beam;
    private Transform beamTransform;
    private AudioSource humSource;
    private float sweepTimer;   // progress through current sweep
    private float waitTimer;    // progress through loop interval
    private bool isWaiting;
    private bool isActive;
    private bool hasKilledThisFrame;

    void Awake()
    {
        EnsureBeamRenderer();
        SetupHumSource();
        isActive = autoStart;
        ResetSweep();
    }

    void OnEnable()
    {
        EnsureBeamRenderer();
        UpdateBeam();
    }

    void OnValidate()
    {
        maxLength = Mathf.Max(0.1f, maxLength);
        beamWidth = Mathf.Max(0.001f, beamWidth);
        swingSpeed = Mathf.Max(0f, swingSpeed);
        loopInterval = Mathf.Max(0f, loopInterval);
        humHearMaxDistance = Mathf.Max(humHearMinDistance, humHearMaxDistance);
        if (string.IsNullOrWhiteSpace(sortingLayerName))
            sortingLayerName = DefaultBeamSortingLayerName;
    }

    void Update()
    {
        hasKilledThisFrame = false;

        if (isActive)
            TickSwing();

        UpdateBeam();
        UpdateHum();
    }

    /// <summary>Enable or disable the laser.</summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
            ResetSweep();
    }

    private void ResetSweep()
    {
        sweepTimer = 0f;
        waitTimer = 0f;
        isWaiting = false;
        ApplyAngle(GetStartAngle());
    }

    private void TickSwing()
    {
        float range = Mathf.Abs(angleB - angleA);

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= loopInterval)
            {
                isWaiting = false;
                sweepTimer = 0f;
                // Snap to start angle now so the beam doesn't flash at the end this frame.
                ApplyAngle(GetStartAngle());
            }
            return;
        }

        // FullCycle travels A->B->A, so its angular distance is twice the range.
        float travel = swingMode == SwingMode.FullCycle ? range * 2f : range;
        float sweepDuration = swingSpeed > 0f ? travel / swingSpeed : 0f;
        sweepTimer += Time.deltaTime;

        float t = sweepDuration > 0f ? Mathf.Clamp01(sweepTimer / sweepDuration) : 1f;
        ApplyAngle(GetSweepAngle(t));

        if (t >= 1f)
        {
            isWaiting = true;
            waitTimer = 0f;
        }
    }

    private float GetStartAngle()
    {
        // FullCycle and AtoB start at A; BtoA starts at B.
        return swingMode == SwingMode.OneWayBtoA ? angleB : angleA;
    }

    // Angle at sweep progress t (0..1) for the current mode.
    private float GetSweepAngle(float t)
    {
        switch (swingMode)
        {
            case SwingMode.OneWayBtoA:
                return Mathf.Lerp(angleB, angleA, t);
            case SwingMode.FullCycle:
                // A -> B -> A
                return Mathf.Lerp(angleA, angleB, Mathf.PingPong(t * 2f, 1f));
            case SwingMode.OneWayAtoB:
            default:
                return Mathf.Lerp(angleA, angleB, t);
        }
    }

    private void ApplyAngle(float angle)
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool IsBeamLive()
    {
        return isActive && !isWaiting;
    }

    private void UpdateBeam()
    {
        if (beam == null)
            return;

        bool live = IsBeamLive();
        beam.enabled = live;
        if (!live)
            return;

        Vector2 origin = transform.position;
        Vector2 dir = transform.right;

        Vector2 endPoint = origin + dir * maxLength;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxLength, blockingLayers);
        if (hit.collider != null)
        {
            endPoint = hit.point;
            if (!hasKilledThisFrame && S_PlayerLookup.IsPlayer(hit.collider))
                KillPlayer();
        }

        beam.SetPosition(0, origin);
        beam.SetPosition(1, endPoint);
    }

    private void KillPlayer()
    {
        hasKilledThisFrame = true;
        if (hitSfx != null)
            S_GameEvent.PlaySFX(hitSfx);
        S_GameEvent.PlayerDied();
    }

    private void UpdateHum()
    {
        if (humSource == null)
            return;

        bool shouldPlay = isActive && humLoop != null;
        if (shouldPlay)
        {
            if (humSource.clip != humLoop)
                humSource.clip = humLoop;
            if (!humSource.isPlaying)
                humSource.Play();
        }
        else if (humSource.isPlaying)
        {
            humSource.Stop();
        }
    }

    private void SetupHumSource()
    {
        humSource = GetComponent<AudioSource>();
        if (humSource == null)
            humSource = gameObject.AddComponent<AudioSource>();

        humSource.playOnAwake = false;
        humSource.loop = true;
        humSource.volume = humVolume;
        humSource.spatialBlend = 1f; // 3D: attenuate by distance
        humSource.rolloffMode = AudioRolloffMode.Linear;
        humSource.minDistance = humHearMinDistance;
        humSource.maxDistance = humHearMaxDistance;
    }

    private void EnsureBeamRenderer()
    {
        if (beamTransform == null)
            beamTransform = transform.Find("LaserBeam");

        if (beamTransform == null)
        {
            GameObject go = new GameObject("LaserBeam");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            beamTransform = go.transform;
        }

        if (beam == null)
            beam = beamTransform.GetComponent<LineRenderer>();
        if (beam == null)
            beam = beamTransform.gameObject.AddComponent<LineRenderer>();

        ConfigureBeam(beam);
    }

    private void ConfigureBeam(LineRenderer lr)
    {
        if (lr == null)
            return;

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = beamWidth;
        lr.endWidth = beamWidth;
        lr.startColor = beamColor;
        lr.endColor = beamColor;
        lr.numCapVertices = 4;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.sortingOrder = sortingOrder;

        lr.sortingLayerName = GetBeamSortingLayerName();

        lr.sharedMaterial = beamMaterial != null ? beamMaterial : GetFallbackMaterial();
    }

    private string GetBeamSortingLayerName()
    {
        return string.IsNullOrWhiteSpace(sortingLayerName)
            ? DefaultBeamSortingLayerName
            : sortingLayerName.Trim();
    }

    private static Material GetFallbackMaterial()
    {
        if (fallbackBeamMaterial != null)
            return fallbackBeamMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        fallbackBeamMaterial = new Material(shader);
        fallbackBeamMaterial.name = "Generated Laser Beam Material";
        return fallbackBeamMaterial;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Vector3 origin = transform.position;
        Quaternion baseRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;

        // Swing bounds.
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Vector3 edgeA = origin + (baseRot * AngleToDir(angleA)) * maxLength;
        Vector3 edgeB = origin + (baseRot * AngleToDir(angleB)) * maxLength;
        Gizmos.DrawLine(origin, edgeA);
        Gizmos.DrawLine(origin, edgeB);
        Gizmos.DrawWireSphere(origin, 0.1f);

        // Swing arc.
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.35f);
        Vector3 prev = edgeA;
        for (int i = 1; i <= GizmoArcSegments; i++)
        {
            float t = (float)i / GizmoArcSegments;
            float a = Mathf.Lerp(angleA, angleB, t);
            Vector3 point = origin + (baseRot * AngleToDir(a)) * maxLength;
            Gizmos.DrawLine(prev, point);
            prev = point;
        }

        // Hum hearing range.
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(origin, humHearMaxDistance);
    }

    private static Vector3 AngleToDir(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }
}
