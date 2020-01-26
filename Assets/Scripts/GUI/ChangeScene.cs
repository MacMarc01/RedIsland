using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour {

    public string startScene; // Load scene when script enabled
    public float delay; // Load scene after this amount of seconds when script enabled


	bool alreadyStarted=false;
    // Use this for initialization
    void Start()
    {
		// Load scene, if there's one declared for instant activation

		if (!startScene.Equals(""))
	        StartCoroutine(ActivateScene(delay, startScene));
    }

    private IEnumerator ActivateScene(float delay, string path)
    {
		// Load Scene

		AsyncOperation ao = SceneManager.LoadSceneAsync(path);
        ao.allowSceneActivation = false;

        yield return new WaitForSeconds(delay);

        // Activate Scene

        ao.allowSceneActivation = true;
    }

	// Load and activate scene
	public void LoadScene (string scene)
	{
		if (!startScene.Equals(""))
		{
			Debug.LogError("Can't load more than 1 scene");
			return;
		}

		StartCoroutine(ActivateScene(0, scene));
	}
}