using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMap : MonoBehaviour
{
	public float traction = 1.0f; // Speed at which vehicles can move over this terrain
	public bool water = false;

	private void OnTriggerEnter2D(Collider2D collision)
	{
		// Check if it's a vehicle

		if (collision.gameObject.GetComponent<Vehicle>() != null)
			UpdateFriction(collision);
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		// Check if it's a vehicle

		if (collision.gameObject.GetComponent<Vehicle>() != null)
			UpdateFriction(collision);
	}

	private void OnTriggerStay2D(Collider2D collision)
	{
		// Check if it's a vehicle and in motion

		if (collision.gameObject.GetComponent<Vehicle>() != null && collision.gameObject.GetComponent<Vehicle>().speed != 0)
			UpdateFriction(collision);
	}

	private void UpdateFriction(Collider2D collision)
	{
		// Check how many contact points are in terrain

		GameObject colliding = collision.gameObject;
		Vehicle vehicle = colliding.GetComponent<Vehicle>();
		Transform[] contactPoints = vehicle.contactPoints;

		if (contactPoints.Length == 0)
			return;

		Collider2D collider = gameObject.GetComponent<Collider2D>();

		int inside = 0;

		foreach (Transform t in contactPoints)
		{
			if (collider.OverlapPoint(t.position))
				inside++;
		}

		float percentage = 1f * inside / contactPoints.Length;

		// Drown

		if (inside == contactPoints.Length)
		{
			vehicle.Drown();
		}

		// Apply to vehicle

		float newTraction = (1f - percentage) + traction * percentage;
		if (vehicle.traction != newTraction)
		{
			// Adjust speed

			colliding.GetComponent<Rigidbody2D>().velocity *= 0.4f + (newTraction / vehicle.traction * 0.6f);

			// Adjust power

			vehicle.traction = newTraction;

			// Adjust drag

			vehicle.setDrag(vehicle.drag * newTraction);
			vehicle.setAngularDrag(vehicle.angularDrag * newTraction);
		}
	}
}
