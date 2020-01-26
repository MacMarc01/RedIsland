using UnityEngine;
using System.Collections;

public class MinRangeAnimation : MonoBehaviour
{
    public float interval;
    public float animationSpeed = 1f;
    public Transform parent;
    public Transform dot;
    public Transform weapon;
    public PlayerAiming aim;

	private bool started = false;

	private int number;
    private double rotation;
    private Weapon gun;
    private float minRange;

    public void StartUp()
	{
		started = true;

		gun = weapon.GetComponent<Weapon>();

		bool show = gun.aimDir;

		if (show)
		{
			minRange = gun.minRange;

			// Calculate how many dots

			number = (int)Mathf.Round(minRange * 2f * Mathf.PI / interval);
			if (number < 1)
				number = 1;

			// Calculate angle from one dot to next one

			double angle = 360d * 1d / number;

			// Set dot's position

			dot.localPosition = new Vector3(GetX((float)(angle * (float)0), minRange), GetY((float)(angle * (float)0), 0));
			dot.rotation = Quaternion.Euler(0, 0, 270);
			// Clone dot

			for (int i = 1; i < number; i++)
			{
				// Create clone

				Transform nextDot = Instantiate(dot);

				// Apply it to Container

				nextDot.parent = parent;

				// Rotate and position it

				float rot = (float)(angle * (float)i) - 90;
				if (rot < 0)
					rot += 360;

				nextDot.rotation = Quaternion.Euler(0, 0, rot);
				nextDot.localPosition = new Vector3(GetX((float)(angle * (float)i), minRange), GetY((float)(angle * (float)i), minRange), 0); // Position on an imaginary circle around the parent
			}

			animationSpeed = animationSpeed / (minRange * 2f * Mathf.PI);
		}
		else
		{
			// Remove particle

			Destroy(dot.gameObject);

			// Remove script

			Destroy(gameObject.AddComponent<MaxRangeAnimation>());
		}
	}

	public void SetWeapon(Transform newWeapon)
	{
		weapon = newWeapon;
	}

	void FixedUpdate() {
		if (GameMaster.IsPaused()) return;

		if (!started)
			return;

		// Return, if game is paused

		if (Mathf.Approximately(Time.timeScale, 0.0f))
			return;

		// Rotate parent (creates animation)

		parent.rotation = Quaternion.Euler(0, 0, parent.rotation.eulerAngles.z - animationSpeed);
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
}
