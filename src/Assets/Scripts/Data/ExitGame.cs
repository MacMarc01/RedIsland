using UnityEngine;
using System.Collections;

public class ExitGame : MonoBehaviour {
    public void ExitInst()
    {
        Debug.Log("Application was quit by player.");

        Application.Quit();

		// Stop application when in editor

		#if UNITY_EDITOR
		if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
		#endif
	}

	public static void Exit()
	{
		Debug.Log("Application was quit by player.");

		Application.Quit();

		// Stop application when in editor

		#if UNITY_EDITOR
		if (Application.isEditor)
		{
			UnityEditor.EditorApplication.isPlaying = false;
		}
		#endif
	}
}
