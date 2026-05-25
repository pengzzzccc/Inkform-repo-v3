using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[CreateAssetMenu(fileName = "CountdownDigitVoiceLibrary", menuName = "InkForm/Tutorial/Countdown Digit Voice Library")]
public class S_CountdownDigitVoiceLibrary : ScriptableObject
{
    [Header("Phrase Clips")]
    [SerializeField] private AudioClip prefixClip;
    [SerializeField] private AudioClip secondsClip;

    [Header("Digit Clips")]
    [Tooltip("Index 0-9 should contain the matching spoken digit clip.")]
    [SerializeField] private AudioClip[] digitClips = new AudioClip[10];

    [Header("Playback")]
    [SerializeField, Min(0f)] private float clipGap = 0.04f;

    public float ClipGap => clipGap;

    public bool TryBuildCountdownClips(int seconds, List<AudioClip> output)
    {
        if (output == null)
            return false;

        output.Clear();

        if (seconds < 0 || digitClips == null || digitClips.Length < 10)
            return false;

        string digits = seconds.ToString(CultureInfo.InvariantCulture);
        for (int i = 0; i < digits.Length; i++)
        {
            int digit = digits[i] - '0';
            if (digit < 0 || digit > 9 || digitClips[digit] == null)
            {
                output.Clear();
                return false;
            }
        }

        if (prefixClip != null)
            output.Add(prefixClip);

        for (int i = 0; i < digits.Length; i++)
            output.Add(digitClips[digits[i] - '0']);

        if (secondsClip != null)
            output.Add(secondsClip);

        return output.Count > 0;
    }
}
