using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Script to set 4 different images for toggle depending on whether it's on and interactable

[RequireComponent(typeof(Toggle))]
public class ToggleGraphics : MonoBehaviour {

	public Transform onImage;
	public Transform offImage;

	public Sprite onAndActivated;
	public Sprite offAndActivated;
	public Sprite onAndDeactivated;
	public Sprite offAndDeactivated;

	private Toggle toggle;
	private bool active;

	public void SetOn(bool isOff)
	{
		offImage.gameObject.SetActive(!isOff);
		onImage.gameObject.SetActive(isOff);
	}

	void Start()
	{
		toggle = GetComponent<Toggle>();

		active = toggle.IsInteractable();

		SetOn(toggle.isOn);
	}

	void Update()
	{
		if (toggle.IsInteractable() != active)
		{
			if (active)
			{
				// Toggle is not active anymore

				onImage.GetComponent<Image>().sprite = onAndDeactivated;
				offImage.GetComponent<Image>().sprite = offAndDeactivated;
			} else
			{
				// Toggle is now active

				onImage.GetComponent<Image>().sprite = onAndActivated;
				offImage.GetComponent<Image>().sprite = offAndActivated;
			}

			active = toggle.IsInteractable();
		}
	}
}
