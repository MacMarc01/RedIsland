using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]

public class Vehicle : MonoBehaviour {
	public int terrainUpdatesPerSecond = 3; // How often the vehicle checks the ground it's on and changing behavior accordingly
    public float speed = 4;
    public float backwardMultiplier = 0.5f;
    public float turningSpeed = 1;
    public float drag = 1; // How much inertion the vehicle has in movement
	public float angularDrag = 2; // How much inertion the vehicle has in rotation
    public float drift = 1; // Turn to 0 to steer the vehicle directly in the direction it's facing
	public WateringBehavior wateringBehavior; // What happens with the vehicle when immerged in water
    public Transform[] leftTracks; // Used for speed animation
    public Transform[] rightTracks; // s.a.
	public float animationMultiplier = 1f;
	public float rotationMultiplier = 1f; // how fast the animations are sped up when vehicle is turning
	public Transform[] contactPoints; // Used to determine the current terrain
	public Transform engineSound;
	public float pitcher = 1f;
	public Transform mapObject;

    public bool showDebugInfo = false;

	[HideInInspector]
	public float traction = 1.0f; // Terrain scripts can change this to emulate different ground traction

    private Transform veh;
    private Rigidbody2D rb;
    private Animator[] leftTrackAnims;
    private Animator[] rightTrackAnims;
    private AudioSource src;
    private AdjustableSound set;
	private Map map;

    private double engineStress = 0.3; // How hard the engine works, used for sound

	private bool inWater = false;

	private float oldSpeed;
	private float oldTurningSpeed;

	[System.Serializable]
	public class WateringBehavior
	{
		public WateringMode mode;

		public Object[] removeOnWater;

		public enum WateringMode
		{
			DROWN,
			IGNORE,
			SWIM
		}
	}

    // Use this for initialization
    void Start () {
        veh = transform;

        rb = GetComponent<Rigidbody2D>();

		map = mapObject.GetComponent<Map>();

        setDrag(drag);
		setAngularDrag(angularDrag);

		oldSpeed = speed;
		oldTurningSpeed = turningSpeed;

		// Init tracks

		SetAnims();

        if (engineSound != null)
        {
            src = engineSound.GetComponent<AudioSource>();

            set = engineSound.GetComponent<AdjustableSound>();
        }
    }

    void FixedUpdate()
    {
	    if (GameMaster.IsPaused()) return;
	    KillOrthogonalVelocity();
    }
	
    public void Drive (float vert, float rotation)
    {
        if (vert > 1)
            vert = 1;
        else if (vert < -1)
            vert = -1;

		if (rotation > 1)
			rotation = 1;
		else if (rotation < -1)
			rotation = -1;

        // Calculate engineStress (smoothed out engine usage)
		if (engineSound != null)
            engineStress =
                (engineStress * 0.97)
              + (Mathf.Clamp((Mathf.Abs(vert) + Mathf.Abs(rotation * 0.6666f)), 0.3f, 1.0f) * 0.03);

        // Calculate rotation
        
        float rot = rotation * turningSpeed * -1 * angularDrag * rb.mass;

		// Apply terrain traction

		rot *= traction * traction;
		vert *= traction * traction;

		// Apply velocity

		rb.AddForce(rb.transform.up * vert 
			* speed 
			* rb.mass 
			* drag
			);

		// Apply rotation
		rb.AddTorque(rot);

        // Set animated track speed

        foreach (Animator a in leftTrackAnims)
        {
            a.SetFloat(
				"Speed", (transform.InverseTransformDirection(rb.velocity).y + (rot / 15f) * rotationMultiplier) * animationMultiplier);
        }
        foreach (Animator a in rightTrackAnims)
        {
            a.SetFloat("Speed", (transform.InverseTransformDirection(rb.velocity).y - (rot / 15) * rotationMultiplier) * animationMultiplier);
        }

        // Set sound level

        if (engineSound != null)
        {
            set.SetVolume((float) engineStress);

            src.pitch = 0.25f + (float) (engineStress / 1.5) * pitcher;
        }
    }

	public void Drown()
	{
		if (wateringBehavior.mode == WateringBehavior.WateringMode.DROWN)
		{
			gameObject.GetComponent<Entity>().letDie();
		}
	}

    public void setDrag(float amount)
    {
        rb.drag = amount;
    }

	public void SetVehicleDrag(float amount) {
		float percentage = rb.drag / drag;
		drag = amount;
		setDrag(percentage * amount);
	}

	public void setAngularDrag(float amount)
	{
		rb.angularDrag = amount;
	}

	public void SetVehicleAngularDrag(float amount) {
		float percentage = rb.angularDrag / angularDrag;
		angularDrag = amount;
		setAngularDrag(percentage * amount);
	}

	public void SetSpeed(float value) {
		speed = value;
		oldSpeed = value;
	}

	public void SetTurnSpeed(float value) {
		turningSpeed = value;
		oldTurningSpeed = value;
	}

	public void SetPitcher(float value) {
		pitcher = value;
	}

    public void KillOrthogonalVelocity()
    {
        Vector2 forwardVelocity = veh.up * Vector2.Dot(rb.velocity, veh.up);
        Vector2 rightVelocity = veh.right * Vector2.Dot(rb.velocity, veh.right);
        rb.velocity = forwardVelocity + rightVelocity * drift * traction;
    }

	public void setAnimationMultiplier(float value) {
		animationMultiplier = value;
	}

	public void SetAnims() {
		leftTrackAnims = new Animator[rightTracks.Length];
		rightTrackAnims = new Animator[leftTracks.Length];

		for (int i = 0; i < leftTracks.Length; i++) {
			rightTrackAnims[i] = leftTracks[i].GetComponent<Animator>();
		}
		for (int i = 0; i < rightTracks.Length; i++) {
			leftTrackAnims[i] = rightTracks[i].GetComponent<Animator>();
		}
	}
}
