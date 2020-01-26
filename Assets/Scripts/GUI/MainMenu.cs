using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class MainMenu : MonoBehaviour {
	void Start()
	{
		if (GameData.wasMenuLoaded)
		{
			// Disable (speed up very fast) animation

			GetComponent<Animator>().SetFloat("speed", 22f);
		} else
		{
			if (GameData.starts == 1)
			{
				// Game started first time

				// Play animation slower

				GetComponent<Animator>().SetFloat("speed", 0.5f);
			} else
			{
				// Game already started before

				GetComponent<Animator>().SetFloat("speed", 1.5f);
			}
		}
	}
}
