using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineSelector : MonoBehaviour
{
	public AudioSource engineSound;

	public void StartUp() {
		engineSound.enabled = true;
	}
}
