using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Script to contain all data saved after session and globally relevant session data

public class GameData : MonoBehaviour
{
	public static GameData instance;

	// Save-data (options)

	public static bool fullscreen;
	public static int vertRes;
	public static int horRes;

	public static int renderQuality; // 0 = low; 1 = medium; 2 = high
	
	public static float overallVol;
	public static float sfxVol;
	public static float voiceVol;
	public static float musicVol;
	public static float environmentVol;

	public static int starts;

	public static bool loaded = false; // Whether any activation of GameData script occured before

	// Save-data (campaign progress)

	public static bool startedCampaign;
	public static int levelsDone;
	public static int coins;
	public static int weaponsBuyed; // How many weapon upgrades the player has
	public static int weaponSelected; // Which level of the weapon is selected
	public static int armorLevel; // How many armor upgrades the player has
	public static int trackLevel; // How many track upgrades the player has
	public static int scoutEquipmentLevel; // How many scout equipment upgrades the player has
	public static int teamSkills;

	// Session data

	public static bool wasMenuLoaded; // Whether the main menu was opened in this session. (Used to deactivate start animation a second time)
	
	public static AudioManager audioManage;

	public Transform audioManager;

	public Transform renderModeSelector;
	public Transform fullscreenActivator;
	public Transform overallVolSlider;
	public Transform sfxVolSlider;
	public Transform voiceVolSlider;
	public Transform musicVolSlider;
	public Transform environmentVolumeSlider;

	public void OnEnable()
	{
		if (audioManager != null)
			GameData.audioManage = audioManager.GetComponent<AudioManager>();
		
		if (!loaded)
		{
			// Load every value or default it, if it does not exist
			
			fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
			vertRes = PlayerPrefs.GetInt("vertRes", 1000);
			horRes = PlayerPrefs.GetInt("horRes", 600);
			renderQuality = PlayerPrefs.GetInt("quality", 2);
			overallVol = PlayerPrefs.GetFloat("Overall_Volume", 1.0f);
			sfxVol = PlayerPrefs.GetFloat("Effects_Volume", 1.0f);
			voiceVol = PlayerPrefs.GetFloat("Voice_Volume", 1.0f);
			musicVol = PlayerPrefs.GetFloat("Music_Volume", 0.7f);
			environmentVol = PlayerPrefs.GetFloat("Environment_Volume", 0.4f);
			starts = PlayerPrefs.GetInt("Starts", 0) + 1;
			startedCampaign = PlayerPrefs.GetInt("Campaign_Started", 0) == 1;
			levelsDone = PlayerPrefs.GetInt("Levels_Done", 0);
			coins = PlayerPrefs.GetInt("Coins", 0);
			weaponsBuyed = PlayerPrefs.GetInt("Weapons_Buyed", 0);
			weaponSelected = PlayerPrefs.GetInt("Weapons_Selected", 0);
			armorLevel = PlayerPrefs.GetInt("Armor_Buyed", 0);
			trackLevel = PlayerPrefs.GetInt("Tracks_Buyed", 0);
			scoutEquipmentLevel = PlayerPrefs.GetInt("Scout_Equipment_Buyed", 0);

			wasMenuLoaded = false;

			loaded = true;

			Debug.Log("Starting Red Island on " + System.DateTime.Today + "; Starts: " + starts);
		}

		instance = this;
	}

	void Start()
	{
		if (renderModeSelector != null)
			renderModeSelector.GetComponent<Dropdown>().value = GameData.renderQuality;

		if (fullscreenActivator != null)
			fullscreenActivator.GetComponent<Toggle>().isOn = GameData.fullscreen;

		if (overallVolSlider != null)
			overallVolSlider.GetComponent<Slider>().value = GameData.overallVol;

		if (sfxVolSlider != null)
			sfxVolSlider.GetComponent<Slider>().value = GameData.sfxVol;

		if (voiceVolSlider != null)
			voiceVolSlider.GetComponent<Slider>().value = GameData.voiceVol;

		if (musicVolSlider != null)
			musicVolSlider.GetComponent<Slider>().value = GameData.musicVol;

		if (environmentVolumeSlider != null)
			environmentVolumeSlider.GetComponent<Slider>().value = GameData.environmentVol;


        // Set values to loaded values

        if (audioManage != null)
		{
			SetOverallVolume(overallVol);
			SetEffectsVolume(sfxVol);
			SetVoiceVolume(voiceVol);
			SetEnvironmentVolume(environmentVol);
            SetMusicVolume(musicVol);
		}
		SetRenderQuality(renderQuality);
		SetFullscreen(fullscreen);
	}

	void Update()
	{
		// Test for universal key codes

		if (Input.GetKeyDown(KeyCode.F11))
		{
			// Change fullscreen movementMode

			SetFullscreen(!fullscreen);
		}

		if ((Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.F4))
		{
			// End programm

			// Save data

			Save();

			// Close

			ExitGame.Exit();
		}
	}

	public void Save()
	{
		// Write all data

		if (fullscreen)
			PlayerPrefs.SetInt("Fullscreen", 1);
		else
			PlayerPrefs.SetInt("Fullscreen", 0);
		PlayerPrefs.SetInt("vertRes", vertRes);
		PlayerPrefs.SetInt("horRes", horRes);
		PlayerPrefs.SetInt("quality", renderQuality);
		PlayerPrefs.SetFloat("Overall_Volume", overallVol);
		PlayerPrefs.SetFloat("Effects_Volume", sfxVol);
		PlayerPrefs.SetFloat("Voice_Volume", voiceVol);
		PlayerPrefs.SetFloat("Music_Volume", musicVol);
		PlayerPrefs.SetFloat("Environment_Volume", environmentVol);
		PlayerPrefs.SetInt("Starts", starts);
		if (startedCampaign)
			PlayerPrefs.SetInt("Campaign_Started", 1);
		else
			PlayerPrefs.SetInt("Campaign_Started", 0);
		PlayerPrefs.SetInt("Levels_Done", levelsDone);
		PlayerPrefs.SetInt("Coins", coins);
		PlayerPrefs.SetInt("Weapons_Buyed", weaponsBuyed);
		PlayerPrefs.SetInt("Weapons_Selected", weaponSelected);
		PlayerPrefs.SetInt("Armor_Buyed", armorLevel);
		PlayerPrefs.SetInt("Tracks_Buyed", trackLevel);
		PlayerPrefs.SetInt("Scout_Equipment_Buyed", scoutEquipmentLevel);

		// Save file

		PlayerPrefs.Save();
	}

	public static void SetOverallVolume(float volume)
	{
		GameData.audioManage.SetOverallVolume(volume);
		
		GameData.overallVol = volume;
	}

	public static void SetEffectsVolume(float volume)
	{
		audioManage.SetVolume(AudioManager.SoundType.EFFECTS, volume);
		
		GameData.sfxVol = volume;
	}

	public static void SetVoiceVolume(float volume)
	{
		audioManage.SetVolume(AudioManager.SoundType.SPEECH, volume);
		
		GameData.voiceVol = volume;
	}

	public static void SetMusicVolume(float volume)
	{
		audioManage.SetVolume(AudioManager.SoundType.MUSIC, volume);
		
		GameData.musicVol = volume;
	}

	public static void SetEnvironmentVolume(float volume)
	{
		audioManage.SetVolume(AudioManager.SoundType.ENVIRONMENT, volume);
		
		GameData.environmentVol = volume;
	}

	public static void SetRenderQuality(int quality)
	{
		GameData.ApplyRenderQuality(quality);

		GameData.renderQuality = quality;
	}

	public static void SetFullscreen(bool fullscreen)
	{
		Screen.fullScreen = fullscreen;

		GameData.fullscreen = fullscreen;

		if (instance != null && instance.fullscreenActivator != null)
			instance.fullscreenActivator.GetComponent<Toggle>().isOn = fullscreen;

		// Maximize windows for editor

		#if UNITY_EDITOR
		if (Application.isEditor && UnityEditor.EditorWindow.focusedWindow != null)
			UnityEditor.EditorWindow.focusedWindow.maximized = fullscreen;
		#endif
	}

	// Non static methods are needed, because it's easer to let the values be edited directly by the EventSystem, which cannot handle static methods.

	public void SetOverallVolumeInst(float volume)
	{
		GameData.SetOverallVolume(volume);
	}

	public void SetEffectsVolumeInst(float volume)
	{
		GameData.SetEffectsVolume(volume);
	}

	public void SetVoiceVolumeInst(float volume)
	{
		GameData.SetVoiceVolume(volume);
	}

	public void SetMusicVolumeInst(float volume)
	{
		GameData.SetMusicVolume(volume);
	}

	public void SetEnvironmentVolumeInst(float volume)
	{
		GameData.SetEnvironmentVolume(volume);
	}

	public void SetRenderQualityInst(int quality)
	{
		GameData.ApplyRenderQuality(quality);

		GameData.renderQuality = quality;
	}

	public void SetFullscreenInst(bool fullscreen)
	{
		Screen.fullScreen = fullscreen;

		GameData.fullscreen = fullscreen;

		if (instance != null && instance.fullscreenActivator != null)
			instance.fullscreenActivator.GetComponent<Toggle>().isOn = fullscreen;

		// Maximize windows for editor

		#if UNITY_EDITOR
		if (Application.isEditor && UnityEditor.EditorWindow.focusedWindow != null)
			UnityEditor.EditorWindow.focusedWindow.maximized = fullscreen;
		#endif
	}

	public static float GetOverallVolume()
	{
		return GameData.overallVol;
	}

	public static float GetEffectVolume()
	{
		return GameData.sfxVol;
	}

	public static float GetVoiceVolume()
	{
		return GameData.voiceVol;
	}

	public static float GetMusicVolume()
	{
		return GameData.musicVol;
	}

	public static float GetEnvironmentVolume()
	{
		return GameData.environmentVol;
	}

	public static int GetRenderQuality()
	{
		return GameData.renderQuality;
	}

	public static void ApplyRenderQuality(int quality)
	{
		if (quality == 0)
		{
			QualitySettings.SetQualityLevel(0);
		}
		else if (quality == 1)
		{
			QualitySettings.SetQualityLevel(3);
		}
		else if (quality == 2)
		{
			QualitySettings.SetQualityLevel(5);
		}
		else
		{
			Debug.LogError("Impossible render quality applied");
		}
	}
}