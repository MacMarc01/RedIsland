using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Class to update the text of a speech bubble, which indicates the current percentage of a slider above the pressed slider.
// Also adjusts the alpha value when handle is pressed / released

[RequireComponent(typeof(CanvasGroup))]
public class SliderValueDisplay : MonoBehaviour {
	private Text textfield;
	private CanvasGroup group;

	private bool isProcessActive;
	private bool stopFadeInProcess;
	private bool stopFadeOutProcess;

	private float alpha;

	void OnEnable()
	{
		textfield = transform.GetChild(0).GetComponent<Text>();

		group = GetComponent<CanvasGroup>();
	}

	public void SetValue(float value)
	{
		// Change text of speech bubble

		if (gameObject.activeSelf)
			textfield.text = Mathf.RoundToInt(value * 100f) + " %";
	}

	public void fadeIn()
	{
		// End current fading process, if there is one

		if (isProcessActive)
			stopFadeOutProcess = true;

		gameObject.SetActive(true);

		StartCoroutine(startFadeIn());
	}

	public void fadeOut()
	{
		// End current fading process, if there is one

		if (isProcessActive)
			stopFadeInProcess = true;

		StartCoroutine(startFadeOut());
	}

	private IEnumerator startFadeIn()
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		isProcessActive = true;

		// Adjust alpha

		alpha += 0.03f;

		if (alpha > 1.0f)
			alpha = 1.0f;

		group.alpha = alpha;

		// Check if done

		if (Mathf.Approximately(alpha, 1.0f) || stopFadeInProcess)
		{
			// End process

			stopFadeInProcess = false;

			isProcessActive = false;
		} else
		{
			// Restart progress

			yield return new WaitForEndOfFrame();

			StartCoroutine(startFadeIn());
		}
	}

	private IEnumerator startFadeOut()
	{
		isProcessActive = true;

		// Adjust alpha

		alpha -= 0.03f;

		if (alpha < 0.0f)
			alpha = 0.0f;

		group.alpha = alpha;

		// Check if done

		if (Mathf.Approximately(alpha, 0.0f) || stopFadeOutProcess)
		{
			// End process

			stopFadeOutProcess = false;

			isProcessActive = false;

			gameObject.SetActive(false);
		}
		else
		{
			// Restart progress

			yield return new WaitForEndOfFrame();

			StartCoroutine(startFadeOut());
		}
	}
}
