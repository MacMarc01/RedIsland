using UnityEngine;
using System.Collections;

public class Shooting : MonoBehaviour {

    public Transform joint; // The transform, on which is the gun. Usually the turret
    public Transform gun;

    public float maxTurnSpeed; // The maximum speed for joint rotation
    public aimMode mode;
    public float freeRotationDelay = 5f; // After this amount of seconds, the joint changes its rotation when in idle movementMode

    private Transform target; // Only effective, when current rot movementMode is AimAtTransform
    private Transform[] targets; // Only effective, when current rot movementMode is AimAtGroup
    private float dir; // Only effective, when current rot movementMode is AimAtDirection
    private rotatingMode rotMode;

    public enum aimMode
    {
        AimDirectly,
        AimAtGroupOfOpponents
    }

    private enum rotatingMode
    {
        AimAtTransform,
        AimAtGroup,
        AimAtDirection,
        AimFreely
    }

	// Use this for initialization
	void Start () {
        dir = 0;
        rotMode = rotatingMode.AimAtDirection;
	}
	
	// Update is called once per frame
	void Update () {
		if (GameMaster.IsPaused()) return;

		if (rotMode == rotatingMode.AimAtTransform)
        {
            if (target == null)
            {
                dir = 0;
                rotMode = rotatingMode.AimAtDirection;
                return;
            }

          //  float currentRotation = 
        }
        else if (rotMode == rotatingMode.AimAtGroup)
        {
            if (targets.Length == 0)
            {
                dir = 0;
                rotMode = rotatingMode.AimAtDirection;
                return;
            }
        }
        else if (rotMode == rotatingMode.AimAtDirection)
        {

        }
        else if (rotMode == rotatingMode.AimFreely)
        {

        }
    }

    public void aimAt(Transform target)
    {
        if (mode == aimMode.AimAtGroupOfOpponents)
        {
            Debug.LogError("parameter: single target; movementMode: multi target");
            return;
        }
    }

    public void aimAtGroup(Transform[] targets)
    {
        if (mode == aimMode.AimDirectly)
        {
            Debug.LogError("parameter: multi target; movementMode: single target");
            return;
        }
    }

    public void aimAtDirection(float degrees)
    {

    }

    public void aimForward()
    {

    }

    public void rotateFreely()
    {

    }
}
