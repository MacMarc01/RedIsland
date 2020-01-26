using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour {

    // Collects all volume adjustments

    private float OverallVolume = 1.0f;
    private float EffectVolume = 1.0f;
    private float SpeechVolume = 1.0f;
    private float EnvironmentVolume = 0.6f;
    private float MusicVolume = 0.8f;

	private bool muteEffects; // Used for pause menu
	private bool muteEnvironment; // Used for pause menu
	private bool dampenMusic; // Used for pause menu

    private List<AdjustableSound> sounds = new List<AdjustableSound>(); // Collection of all AdjustableSounds

    public AudioManager ()
    {
        // Set default values
        OverallVolume = 1.0f;
        EffectVolume = 1.0f;
        SpeechVolume = 1.0f;
        EnvironmentVolume = 0.6f;
        MusicVolume = 0.8f;
    }

	public enum SoundType
    {
        EFFECTS,
        SPEECH,
        MUSIC,
        ENVIRONMENT,
        OTHER
    }

    public void SetVolume(SoundType type, float volume)
    {
        if (type == SoundType.EFFECTS)
            EffectVolume = volume * OverallVolume;
        else if (type == SoundType.SPEECH)
            SpeechVolume = volume * OverallVolume;
        else if (type == SoundType.ENVIRONMENT)
            EnvironmentVolume = volume * OverallVolume;
        else if (type == SoundType.MUSIC)
            MusicVolume = volume * OverallVolume;

        UpdateSounds();
    }

    public void SetOverallVolume(float volume)
    {
        // Set volume

        OverallVolume = volume;

        UpdateSounds();
    }

    // The volume for the typed multiplied by the overall volume
    public float GetRealVolume(SoundType type)
    {
		if (type == SoundType.EFFECTS)
			if (muteEffects)
				return 0;
			else
				return EffectVolume * OverallVolume;
        else if (type == SoundType.SPEECH)
            return SpeechVolume * OverallVolume;
        else if (type == SoundType.ENVIRONMENT)
			if (muteEffects)
				return 0;
			else
				return EnvironmentVolume * OverallVolume;
		else if (type == SoundType.MUSIC)
			if (dampenMusic)
				return MusicVolume * OverallVolume / 2;
			else
				return MusicVolume * OverallVolume;
		else // 'Other' (default) volume
            return OverallVolume;
    }

    public void AddAudioSource(AdjustableSound sound)
    {
        sounds.Add(sound);
    }

	public void Mute()
	{
		muteEffects = true;
		muteEnvironment = true;
		dampenMusic = true;

        UpdateSounds();
    }

	public void Unmute()
	{
		muteEffects = false;
		muteEnvironment = false;
		dampenMusic = false;

        UpdateSounds();
    }

    private void UpdateSounds()
    {
        for (int i = 0; i < sounds.Count; i++)
        {
            AdjustableSound sound = sounds[i];
            if (sound == null)
            {
                sounds.RemoveAt(i);
                i--;
                continue;
            }
            sound.UpdateSound();
        }
    }
}
