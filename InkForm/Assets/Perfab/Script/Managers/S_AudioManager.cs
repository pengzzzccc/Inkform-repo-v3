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
    private enum PlatformAlarmFadeState
    {
        None,
        FadingIn,
        FadingOut
    }

    public static S_AudioManager Instance { get; private set; }
    private const string BgmVolumePrefsKey = "InkForm.Audio.BgmVolume";
    private const string SfxVolumePrefsKey = "InkForm.Audio.SfxVolume";

    [Header("BGM")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.5f;

    [Header("SFX")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Platform Alarm")]
    [SerializeField] private AudioClip platformAlarmClip;
    [SerializeField][Min(0f)] private float platformAlarmVolumeMultiplier = 1f;
    [SerializeField][Min(0f)] private float platformAlarmFadeInTime = 0.25f;
    [SerializeField][Min(0f)] private float platformAlarmFadeOutTime = 0.35f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource platformAlarmSource;
    private PlatformAlarmFadeState platformAlarmFadeState = PlatformAlarmFadeState.None;
    private float platformAlarmFadeTimer;
    private float platformAlarmFadeDuration;
    private float platformAlarmFadeStartVolume;
    private float platformAlarmFadeTargetVolume;

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.ApplySceneAudioSettings(this);
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }

        Instance = this;

        bgmVolume = PlayerPrefs.GetFloat(BgmVolumePrefsKey, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefsKey, sfxVolume);

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

    void Update()
    {
        UpdatePlatformAlarmFade();
    }

    void OnEnable()
    {
        S_GameEvent.OnPlaySFX += PlaySFX;
        S_GameEvent.OnPlaySFXPitched += PlaySFX;
        S_GameEvent.OnBGMChange += PlayBGM;
        S_GameEvent.OnBgmVolumeChangeRequested += SetBgmVolume;
        S_GameEvent.OnSfxVolumeChangeRequested += SetSfxVolume;
        S_GameEvent.OnSectionDescentStarted += StartPlatformAlarm;
        S_GameEvent.OnSectionDescentCompleted += StopPlatformAlarm;
    }

    void OnDisable()
    {
        S_GameEvent.OnPlaySFX -= PlaySFX;
        S_GameEvent.OnPlaySFXPitched -= PlaySFX;
        S_GameEvent.OnBGMChange -= PlayBGM;
        S_GameEvent.OnBgmVolumeChangeRequested -= SetBgmVolume;
        S_GameEvent.OnSfxVolumeChangeRequested -= SetSfxVolume;
        S_GameEvent.OnSectionDescentStarted -= StartPlatformAlarm;
        S_GameEvent.OnSectionDescentCompleted -= StopPlatformAlarm;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;

        PlayerPrefs.SetFloat(BgmVolumePrefsKey, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (platformAlarmSource != null)
            platformAlarmSource.volume = GetPlatformAlarmTargetVolume();

        PlayerPrefs.SetFloat(SfxVolumePrefsKey, sfxVolume);
        PlayerPrefs.Save();
    }

    private void ApplySceneAudioSettings(S_AudioManager sceneAudio)
    {
        if (sceneAudio == null)
            return;

        if (sceneAudio.bgmClip != null && bgmClip != sceneAudio.bgmClip)
        {
            bgmClip = sceneAudio.bgmClip;
            PlayBGM(bgmClip);
        }

        if (sceneAudio.platformAlarmClip != null)
            platformAlarmClip = sceneAudio.platformAlarmClip;

        platformAlarmVolumeMultiplier = sceneAudio.platformAlarmVolumeMultiplier;
        platformAlarmFadeInTime = sceneAudio.platformAlarmFadeInTime;
        platformAlarmFadeOutTime = sceneAudio.platformAlarmFadeOutTime;
        if (platformAlarmSource != null)
            platformAlarmSource.volume = GetPlatformAlarmTargetVolume();
    }

    private void StartPlatformAlarm(int sectionIndex)
    {
        if (platformAlarmClip == null || platformAlarmSource == null)
            return;

        platformAlarmSource.clip = platformAlarmClip;
        platformAlarmSource.loop = true;

        if (!platformAlarmSource.isPlaying)
        {
            platformAlarmSource.volume = 0f;
            platformAlarmSource.Play();
        }

        StartPlatformAlarmFade(PlatformAlarmFadeState.FadingIn, GetPlatformAlarmTargetVolume(), platformAlarmFadeInTime);
    }

    private void StopPlatformAlarm(int sectionIndex)
    {
        if (platformAlarmSource == null)
            return;

        if (!platformAlarmSource.isPlaying)
        {
            platformAlarmSource.clip = null;
            platformAlarmFadeState = PlatformAlarmFadeState.None;
            return;
        }

        StartPlatformAlarmFade(PlatformAlarmFadeState.FadingOut, 0f, platformAlarmFadeOutTime);
    }

    private void StartPlatformAlarmFade(PlatformAlarmFadeState fadeState, float targetVolume, float duration)
    {
        platformAlarmFadeState = fadeState;
        platformAlarmFadeTimer = 0f;
        platformAlarmFadeDuration = Mathf.Max(0f, duration);
        platformAlarmFadeStartVolume = platformAlarmSource != null ? platformAlarmSource.volume : 0f;
        platformAlarmFadeTargetVolume = Mathf.Max(0f, targetVolume);

        if (platformAlarmFadeDuration > 0f)
            return;

        CompletePlatformAlarmFade();
    }

    private void UpdatePlatformAlarmFade()
    {
        if (platformAlarmFadeState == PlatformAlarmFadeState.None || platformAlarmSource == null)
            return;

        platformAlarmFadeTimer += Time.unscaledDeltaTime;
        float t = platformAlarmFadeDuration > 0f
            ? Mathf.Clamp01(platformAlarmFadeTimer / platformAlarmFadeDuration)
            : 1f;

        platformAlarmSource.volume = Mathf.Lerp(platformAlarmFadeStartVolume, platformAlarmFadeTargetVolume, t);

        if (t >= 1f)
            CompletePlatformAlarmFade();
    }

    private void CompletePlatformAlarmFade()
    {
        if (platformAlarmSource == null)
        {
            platformAlarmFadeState = PlatformAlarmFadeState.None;
            return;
        }

        platformAlarmSource.volume = platformAlarmFadeTargetVolume;

        if (platformAlarmFadeState == PlatformAlarmFadeState.FadingOut)
        {
            platformAlarmSource.Stop();
            platformAlarmSource.clip = null;
        }

        platformAlarmFadeState = PlatformAlarmFadeState.None;
    }

    private float GetPlatformAlarmTargetVolume()
    {
        return sfxVolume * platformAlarmVolumeMultiplier;
    }

}
