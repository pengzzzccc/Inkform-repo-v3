using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class S_VoiceLinePlayer : MonoBehaviour
{
    [Header("Subtitle UI")]
    [SerializeField] private GameObject subtitleRoot;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Settings")]
    [SerializeField] private float fallbackDisplayDuration = 3f;
    [SerializeField] private float typeSpeed = 0.04f;
    [SerializeField] private bool allowSkip = true;

    private AudioSource audioSource;
    private bool skipRequested;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        ClearSubtitleText();
        SetSubtitleVisible(false);
    }

    /// <summary>
    /// Play a voice line and show subtitle text. Yields until playback/display finishes.
    /// </summary>
    public IEnumerator PlayVoiceLine(AudioClip clip, string subtitle)
    {
        if (clip != null)
            yield return PlayVoiceSequence(new[] { clip }, subtitle, 0f);
        else
            yield return PlayVoiceSequence(null, subtitle, 0f);
    }

    public IEnumerator PlayVoiceSequence(IReadOnlyList<AudioClip> clips, string subtitle, float clipGap = 0f)
    {
        StopPlayback();
        skipRequested = false;

        bool hasAudio = HasAnyClip(clips);
        bool audioFinished = !hasAudio;
        Coroutine audioRoutine = null;

        if (hasAudio)
            audioRoutine = StartCoroutine(PlayAudioSequence(clips, Mathf.Max(0f, clipGap), () => audioFinished = true));

        bool hasSubtitle = subtitleText != null && !string.IsNullOrEmpty(subtitle);
        if (hasSubtitle)
        {
            SetSubtitleVisible(true);
            yield return TypeSubtitle(subtitle);
        }
        else
        {
            ClearSubtitleText();
            SetSubtitleVisible(false);
        }

        if (hasAudio)
        {
            while (!audioFinished && !skipRequested)
            {
                if (WasSkipPressed())
                    skipRequested = true;

                yield return null;
            }
        }
        else
        {
            float endTime = Time.unscaledTime + fallbackDisplayDuration;
            while (Time.unscaledTime < endTime && !skipRequested)
            {
                if (WasSkipPressed())
                    skipRequested = true;

                yield return null;
            }
        }

        if (skipRequested)
        {
            if (audioRoutine != null)
                StopCoroutine(audioRoutine);

            StopPlayback();
        }

        ClearSubtitleText();
        SetSubtitleVisible(false);

        S_GameEvent.VoiceLineFinished();
    }

    private IEnumerator TypeSubtitle(string text)
    {
        if (subtitleText == null || string.IsNullOrEmpty(text))
            yield break;

        subtitleText.text = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            if (WasSkipPressed())
            {
                skipRequested = true;
                subtitleText.text = text;
                yield break;
            }

            subtitleText.text += text[i];
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
    }

    public void ClearSubtitle()
    {
        StopPlayback();

        ClearSubtitleText();
        SetSubtitleVisible(false);
    }

    private void ClearSubtitleText()
    {
        if (subtitleText != null)
            subtitleText.text = string.Empty;
    }

    private void SetSubtitleVisible(bool visible)
    {
        GameObject root = GetSubtitleRoot();
        if (root != null)
        {
            root.SetActive(visible);
            return;
        }

        if (subtitleText != null)
            subtitleText.gameObject.SetActive(visible);
    }

    private GameObject GetSubtitleRoot()
    {
        if (subtitleRoot != null)
            return subtitleRoot;

        if (subtitleText == null)
            return null;

        return subtitleText.transform.parent != null
            ? subtitleText.transform.parent.gameObject
            : subtitleText.gameObject;
    }

    private IEnumerator PlayAudioSequence(IReadOnlyList<AudioClip> clips, float clipGap, System.Action onFinished)
    {
        for (int i = 0; clips != null && i < clips.Count; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null)
                continue;

            audioSource.clip = clip;
            audioSource.Play();

            while (audioSource.isPlaying && !skipRequested)
                yield return null;

            if (skipRequested)
                break;

            if (clipGap > 0f && i < clips.Count - 1)
            {
                float endTime = Time.unscaledTime + clipGap;
                while (Time.unscaledTime < endTime && !skipRequested)
                    yield return null;
            }
        }

        onFinished?.Invoke();
    }

    private bool HasAnyClip(IReadOnlyList<AudioClip> clips)
    {
        if (clips == null)
            return false;

        for (int i = 0; i < clips.Count; i++)
        {
            if (clips[i] != null)
                return true;
        }

        return false;
    }

    private bool WasSkipPressed()
    {
        if (!allowSkip)
            return false;

        return S_TutorialSkipInput.WasSkipPressedThisFrame();
    }

    private void StopPlayback()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    private void OnDisable()
    {
        StopPlayback();
        ClearSubtitleText();
        SetSubtitleVisible(false);
    }
}
