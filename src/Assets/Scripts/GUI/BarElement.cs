using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Represents a type of bar, which can be filled to a certain degree. Used for example for health bars
 */

public class BarElement : MonoBehaviour {
	public bool horizontal = false; //else vertical is choosen

	public Color color; //Which color the filled part should be dyed in
	public float part; // How much of the bar should be filled (0.0-1.0)

	public Transform glow;
	public Transform fill;
	public Transform container;

	private Image glowImg;
	private Image fillImg;
	private Image containerImg;

	void Start () {
		glowImg = glow.GetComponent<Image> ();
		fillImg = fill.GetComponent<Image> ();
		containerImg = container.GetComponent<Image> ();

		if (horizontal) {
			containerImg.fillMethod = Image.FillMethod.Horizontal;
		} else {
			containerImg.fillMethod = Image.FillMethod.Vertical;
		}
	}
	
	// Update is called once per frame
	void Update () {
		glowImg.color = color;

		// Make background partly transparent

		Color oldColor = color;

		color.a /= 3;

		fillImg.color = color;

		color = oldColor;

		if (Mathf.Approximately(part, 1)) {
			// Fill images completely

			containerImg.fillAmount = 1.0f;
		} else {
			// Fill images to correct amount

			float pixelsFilled; // How many pixels of the glow image are visible

			if (horizontal)
				pixelsFilled = 6 + (glow.GetComponent<RectTransform>().rect.width-12) * part;
			else
				pixelsFilled = 6 + (glow.GetComponent<RectTransform>().rect.height-12) * part;

			if (horizontal)
				containerImg.fillAmount = pixelsFilled / glow.GetComponent<RectTransform>().rect.width;
			else
				containerImg.fillAmount = pixelsFilled / glow.GetComponent<RectTransform>().rect.height;
		}
	}
}
