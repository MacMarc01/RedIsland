using UnityEngine;
using System.Collections;

public class PlayerAiming : MonoBehaviour {
	public GameObject StrategyPanel;

    public Transform crosshair;

    public Transform tank;
    public Transform turret;
    private Camera cam;
    private double turretDistance;
    private bool singleShot;
    private Weapon weapon;
	private Vector2 mousePos;
	private PlayerController pc;

	[HideInInspector]
	public float maxDistance; // The maximum distance the player can shoot and aim at
	[HideInInspector]
	public float minDistance; // The minimum distance the player can shoot and aim at, turn to 1.0 or less to disable
	[HideInInspector]
	public float turretMovement;  // The speed at which the turret can turn
	[HideInInspector]
	public float turretDistanceMultiplier; // It also takes time to alter the aim distance. The speed of this is calculated by turretMovement times turretDistanceMultiplier
	[HideInInspector]
	public bool canTurn360;
	[HideInInspector]
	public float maxTurn = 360; // Only effective when canTurn360 is false

	public double angle; // The direction the player's tank's turret is facing at
    public double distance; // At which distance the weapon is aiming at

	[HideInInspector]
	public bool active = true; // Whether the focus is on the world view

	void Start()
	{
		OnStart();
	}

	public void OnStart () {
        turret = tank.Find("Textures").Find("Turret");
        weapon = turret.Find("Gun").GetComponent<Weapon>();
        cam = Camera.main;

		// Load turret values

		Turret turretValues = turret.GetComponent<Turret>();

		maxDistance = turretValues.maxDistance;
		minDistance = turretValues.minDistance;
		turretMovement = turretValues.turretMovement;
		turretDistanceMultiplier = turretValues.turretDistanceMultiplier;
		canTurn360 = turretValues.canTurn360;
		maxTurn = turretValues.maxTurn;

		turretDistance = turretMovement * turretDistanceMultiplier;
	}
	
	void FixedUpdate () {
		if (GameMaster.IsPaused()) return;

		// Return, if game is paused

		if (Mathf.Approximately(Time.timeScale, 0.0f))
			return;

        // Calculate distance

        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);

        mousePos = new Vector2(mouse.x, mouse.y);
		
        Vector2 turretPos = new Vector2(turret.position.x, turret.position.y);
        Vector2 diffVec = mousePos - turretPos;
        float aimedDistance = diffVec.magnitude;
        double diff = (float) (aimedDistance - distance);

        if (diff < -turretDistance)
            diff = -turretDistance;
        else if (diff > turretDistance)
            diff = turretDistance;

        // Apply distance

        distance += diff;

        if (distance < minDistance)
            distance = minDistance;
        else if (distance > maxDistance)
            distance = maxDistance;

        // Calculate mouse rotation

        diffVec.Normalize();
        
        float rotM = Mathf.Atan2(diffVec.y, diffVec.x) * Mathf.Rad2Deg * -1;

        rotM += 90;

        if (rotM > 180)
            rotM -= 360;

        // Calculate turret rotation

        float rotT = (turret.rotation.eulerAngles.z * -1) + 180;

        // Calc turning amount

        float rot;

        if (canTurn360)
        {
            // Calculate difference

            rot = rotM - rotT;
            if (rot < -180)
                rot = rot + 360;
            else if (rot > 180)
                rot = rot - 360;

            // Calculate turning amount

            if (rot > turretMovement)
            {
                rot = (float)turretMovement;
            }

            else if (rot < -turretMovement)
            {
                rot = (float)-turretMovement;
            }

            rot = -rot + turret.rotation.eulerAngles.z;

            if (rot < -180)
                rot = rot + 360;
            else if (rot > 180)
                rot = rot - 360;

            // Apply turning amount
            
            turret.rotation = Quaternion.Euler(0, 0, rot);
        } else
        {
            float currentRotation = -1 * (turret.localRotation.eulerAngles.z - 180);

            float wantedRotation = - GetAngle(tank.up, cam.ScreenToWorldPoint(Input.mousePosition) - turret.position);

            float offset = wantedRotation - currentRotation;

            float rotationAmount = Mathf.Clamp(offset, -turretMovement, +turretMovement);

            float newRotation = currentRotation + rotationAmount;
            
            if (newRotation < -maxTurn)
                newRotation = -maxTurn;
            else if (newRotation > +maxTurn)
                newRotation = +maxTurn;

            turret.localRotation = Quaternion.Euler(0, 0, 180-newRotation);
        }

        // Set crosshair position

        float rotation = turret.rotation.eulerAngles.z;
        crosshair.position = new Vector3(turret.position.x + GetX(rotation + 270, (float) distance), turret.position.y + GetY(rotation + 270, (float) distance), 0);

		// Check for activation or deactivation

		if (Input.GetMouseButtonDown(0) || Input.GetKeyDown("space"))
		{
			if (IsMouseInRT(StrategyPanel))
			{
				active = false;
			}
			else
			{
				active = true;
			}
		}

		// Check if player presses shoot key

		if (((weapon.singleBurst == true && (Input.GetKeyDown("space") || Input.GetMouseButtonDown(0)))
			|| (weapon.singleBurst == false && (Input.GetKey("space") || Input.GetMouseButton(0))))
			&& active == true) {
			Debug.Log("check for shot at " + Time.deltaTime);
			weapon.CheckForShot(crosshair.position);
		}
    }

	public void SetActive(bool active)
	{
		this.active = active;
	}

	public void setTurret(Transform newTurret)
	{
		this.turret = newTurret;
	}

    // Get a point (the X-coordinate) at at circle
    private float GetX(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (distance * Mathf.Cos(angle));
    }

    // Get a point (the Y-coordinate) at at circle
    private float GetY(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (distance * Mathf.Sin(angle));
    }
    
    private static float GetAngle(Vector2 v1, Vector2 v2)
    {
        var sign = Mathf.Sign(v1.x * v2.y - v1.y * v2.x);
        return Vector2.Angle(v1, v2) * sign;
    }

	private bool IsMouseInRT(GameObject go)
	{
		if (Input.GetMouseButton(0) && go.activeSelf &&
			 RectTransformUtility.RectangleContainsScreenPoint(
				 go.GetComponent<RectTransform>(),
				 Input.mousePosition))
			return true;
		else
			return false;
	}
}