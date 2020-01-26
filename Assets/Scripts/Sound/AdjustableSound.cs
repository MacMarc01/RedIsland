using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AdjustableSound : MonoBehaviour {
    
    public AudioManager.SoundType soundType = AudioManager.SoundType.OTHER;
    public float originalVolume = 1.0f; // The sound specific volume
    private float volume = 1.0f; // The actual volume (with volume options and other factors considered)
	
    private AudioManager soundManager;

    public bool autoUpdate = false;

    void Start () {
        soundManager = GameObject.Find("SoundManager").GetComponent<AudioManager>();
        soundManager.AddAudioSource(this);

        volume = originalVolume;
		UpdateSound();
	}

    void Update()
    {
	    if (!autoUpdate)
		    return;

	    volume = originalVolume;

	    UpdateSound();
    }

    public void UpdateSound()
    {
        GetComponent<AudioSource>().volume = soundManager.GetRealVolume(soundType) * volume;
    }

    public void SetVolume(float vol)
    {
        volume = vol;
        UpdateSound();
    }

    public float GetVolume() {
        return volume;
    }
}
