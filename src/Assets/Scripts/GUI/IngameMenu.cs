using UnityEngine;
using System.Collections;

public class IngameMenu : MonoBehaviour {
	public GameObject pauseMenu;
	public GameObject optionMenu;
	public GameObject menuDialog;
	public GameObject exitDialog;
	public GameObject weaponDialog;

	public GameObject strategyPanel;

	public PlayerAiming aim;
	public PlayerController con;

	public Transform audioManager;

	private AudioManager audioMan;

	public static IngameMenu instance;

	void Start()
	{
		instance = this;

		weaponDialog.SetActive(true);

		audioMan = audioManager.GetComponent<AudioManager>();
		
		GameMaster.PauseGame();
		PauseGame();
	}

	void Update () {
		// Check if player pauses / resume via ESC-button

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			// Togle pausation

			if (pauseMenu.activeSelf || optionMenu.activeSelf)
			{
				ResumeGame();
			} else
			{
				OpenPauseMenu();
			}
		}
	}

	public void OpenPauseMenu()
	{
		// Cancel if in weapon selection

		if (weaponDialog.activeSelf)
			return;

		// Pause

		aim.SetActive(false);
		con.SetActive(false);

		optionMenu.SetActive(false);
		menuDialog.SetActive(false);
		exitDialog.SetActive(false);

		GameMaster.PauseGame();

		PauseGame();

		pauseMenu.SetActive(true);
	}

	public void OpenOptionMenu()
	{
		aim.SetActive(false);
		con.SetActive(false);

		pauseMenu.SetActive(false);
		menuDialog.SetActive(false);
		exitDialog.SetActive(false);

		GameMaster.PauseGame();

		PauseGame();

		optionMenu.SetActive(true);
	}

	public void OpenExitDialog()
	{
		aim.SetActive(false);
		con.SetActive(false);

		pauseMenu.SetActive(false);
		menuDialog.SetActive(false);
		optionMenu.SetActive(false);

		GameMaster.PauseGame();

		PauseGame();

		exitDialog.SetActive(true);
	}

	public void OpenMenuDialog()
	{
		aim.SetActive(false);
		con.SetActive(false);

		pauseMenu.SetActive(false);
		exitDialog.SetActive(false);
		optionMenu.SetActive(false);

		GameMaster.PauseGame();

		PauseGame();

		menuDialog.SetActive(true);
	}

	public void ResumeGame()
	{
		// Resume

		aim.SetActive(true);
		con.SetActive(true);

		GameMaster.ResumeGame();

		pauseMenu.SetActive(false);
		menuDialog.SetActive(false);
		exitDialog.SetActive(false);
		optionMenu.SetActive(false);
		weaponDialog.SetActive(false);

		audioMan.Unmute();
	}

	private void PauseGame()
	{
		audioMan.Mute();
	}

	public void OnWin() {
		strategyPanel.SetActive(false);
	}
}
