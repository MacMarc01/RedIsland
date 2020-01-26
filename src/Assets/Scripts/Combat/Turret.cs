using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
	// defines the properties of a turret

	public float maxDistance; // The maximum distance the player can shoot and aim at
	public float minDistance; // The minimum distance the player can shoot and aim at, turn to 1.0 or less to disable
	public float turretMovement;  // The speed at which the turret can turn
	public float turretDistanceMultiplier; // It also takes time to alter the aim distance. The speed of this is calculated by turretMovement times turretDistanceMultiplier
	public bool canTurn360;
	public float maxTurn = 360; // Only effective when canTurn360 is false
	public float posRelY = 0;
}
