using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveForward : MonoBehaviour {

	public Vector3 movementPerFrame;

	void FixedUpdate () {
		if (GameMaster.IsPaused()) return;

		transform.localPosition = transform.localPosition + movementPerFrame;
	}
}
