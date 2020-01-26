using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

    public Transform player;
    
    private Rigidbody2D rb2d;
    private Vehicle tank;

	private bool active = true;

    // Initialization
    void Start () {
        rb2d = GetComponent<Rigidbody2D>();

        tank = GetComponent<Vehicle>();
    }
	
    // Update in fixed Intervals
    void FixedUpdate()
    {
		if (active)
			tank.Drive(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
		else
			tank.Drive(0, 0);
    }

	public void SetActive(bool active)
	{
		this.active = active;
	}
}
