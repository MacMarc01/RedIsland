using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class StartScreen : MonoBehaviour
{
	public Transform changeScene;

	void Start()
	{
		ChangeScene cS = changeScene.GetComponent<ChangeScene>();

		if (GameData.starts == 1)
		{
			// Game started first time

			// Play animation slower

			GetComponent<Animator>().speed = 0.5f;

			// Change scene later

			cS.delay /= 0.5f;
		}
		else
		{
			// Game already started before

			// Play animation quicker
			
			GetComponent<Animator>().speed = 1.5f;

			// Change scene earlier

			cS.delay /= 1.5f;
		}
	}
}
