using UnityEngine;

/// <summary>
/// Singleton audio manager for BGM and SFX playback.
/// 
/// HOW TO USE:
///   1. Attach this script to a GameObject in the scene (one per scene).
///   2. Assign audio clips in the Inspector (BGM clip, SFX clips on Player).
///   3. Any script calls S_GameEvent.PlaySFX(clip) to play a one-shot SFX.
///   4. Any script calls S_GameEvent.BGMChange(clip) to switch BGM.
///   5. Adjust volume sliders in Inspector (0-1 range), no UI needed.
/// 
/// Player-specific SFX (jump, form switch) are triggered from S_Player
/// via S_GameEvent.PlaySFX(). All other systems can do the same.
/// </summary>
public class S_AudioManager : MonoBehaviour
{
    public static S_AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.5f;

    [Header("SFX")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Platform Alarm")]
    [SerializeField] private AudioClip platformAlarmClip;
    [SerializeField][Min(0f)] private float platformAlarmVolumeMultiplier = 1f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource platformAlarmSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Add two AudioSources programmatically — no need to manually attach in Inspector
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        platformAlarmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        platformAlarmSource.playOnAwake = false;
        platformAlarmSource.loop = true;
        platformAlarmSource.volume = sfxVolume * platformAlarmVolumeMultiplier;
    }

    void Start()
    {
        // Auto-play BGM if a clip is assigned
        if (bgmClip != null)
            PlayBGM(bgmClip);
    }

    void OnEnable()
    {
        S_GameEvent.OnPlaySFX += PlaySFX;
        S_GameEvent.OnPlaySFXPitched += PlaySFX;
        S_GameEvent.OnBGMChange += PlayBGM;
        S_GameEvent.OnSectionDescentStarted += StartPlatformAlarm;
        S_GameEvent.OnSectionDescentCompleted += StopPlatformAlarm;
    }

    void OnDisable()
    {
        S_GameEvent.OnPlaySFX -= PlaySFX;
        S_GameEvent.OnPlaySFXPitched -= PlaySFX;
        S_GameEvent.OnBGMChange -= PlayBGM;
        S_GameEvent.OnSectionDescentStarted -= StartPlatformAlarm;
        S_GameEvent.OnSectionDescentCompleted -= StopPlatformAlarm;
    }

    // Called via S_GameEvent.PlaySFX(clip)
    private void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, 1f, 1f);
    }

    private void PlaySFX(AudioClip clip, float pitch, float volumeMultiplier)
    {
        if (clip == null || sfxSource == null) return;

        float previousPitch = sfxSource.pitch;
        sfxSource.volume = sfxVolume;
        sfxSource.pitch = Mathf.Max(0.01f, pitch);
        sfxSource.PlayOneShot(clip, Mathf.Max(0f, volumeMultiplier));
        sfxSource.pitch = previousPitch;
    }

    // Called via S_GameEvent.BGMChange(clip)
    private void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        bgmSource.volume = bgmVolume;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /// <summary>
    /// Stops the current BGM. Can be called directly:
    /// S_AudioManager.Instance.StopBGM();
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    private void StartPlatformAlarm(int sectionIndex)
    {
        if (platformAlarmClip == null || platformAlarmSource == null)
            return;

        if (platformAlarmSource.isPlaying)
            return;

        platformAlarmSource.clip = platformAlarmClip;
        platformAlarmSource.loop = true;
        platformAlarmSource.volume = sfxVolume * platformAlarmVolumeMultiplier;
        platformAlarmSource.Play();
    }

    private void StopPlatformAlarm(int sectionIndex)
    {
        if (platformAlarmSource == null)
            return;

        platformAlarmSource.Stop();
        platformAlarmSource.clip = null;
    }
}
