using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(Entity))]
public class EntityAiming : MonoBehaviour
{
    public bool aimDirectly; // If the distance of the aim is activated
    public double maxDistance; // The maximum distance the entity can shoot and aim at
    public double minDistance; // The minimum distance the entity can shoot and aim at, turn to 1.0 or less to disable

    public double turretMovement;  // How fast the turret can rotate
    public double turretDistanceMultiplier; // It also takes time to alter the aim distance. The speed of this is calculated by turretMovement times turretDistanceMultiplier
    public bool canTurn360 = true;
    public double angleInDegrees; // By how much degree the turret can turn

    public float aimDistanceMultplier = 2; // From which distance the AI aims at its target (multiplied with Weapon's range)
    public float afterShot = 5; // How much time the AI aims at the last sight of its target (in seconds)
    public bool aimForwardWhenDriving = false;
    public bool aimRandomWhenIdle = true;
    public float aimRandomDelay; // After which amount of time the turret rotates to a new random position (approximately, in seconds)

    public float updatesPerSecond = 2;

    public Transform turret; // The turret object
    public Transform tank;
    private Transform firePoint; // The muzzle
    private Weapon gun; // The gun object

    public Transform target;
    private float aimDir;
    private bool afterAim = false;
    /*[HideInInspector]*/ public bool currentlyFiring = false;

    private bool randomMoveIncoming = false;
    private double randomMoveAt = 0;

    private bool explosive;
    private float aimDistance;
    private int team;

    private float distance = 5; // the distance which the weapon is currently aimed at
    private float turretDistance;

    void Start()
    {
        gun = turret.Find("Gun").GetComponent<Weapon>();
        firePoint = gun.transform.Find("FirePoint");

        explosive = gun.BulletHitDetection == Weapon.DetectionMode.ExplosionWithCollider;

        aimDistance = aimDistanceMultplier * gun.range;
        team = GetComponent<Entity>().team;
        turretDistance = (float) (turretMovement * turretDistanceMultiplier);

		// Randomize range

		maxDistance = Random.Range(0.99f, 1.01f) * maxDistance;
        
        StartCoroutine(AI_Update());
    }

    void FixedUpdate() {
		if (GameMaster.IsPaused()) return;

		currentlyFiring = false;

		// Aim

		if (explosive)
        {
            //ToDo: Add explosion behavior
        }
        else
        {
            if (target != null)
            {
                bool aimed = true; // If the weapon is aimed at target

                Vector2 aimPos = target.position;
                Vector2 turretPos = new Vector2(turret.position.x, turret.position.y);
                Vector2 difference = aimPos - turretPos;

                if (aimDirectly)
                {
                    // Calculate distance

                    float dis = difference.magnitude;
                    double diff = (float)(dis - distance);

                    if (diff < -turretDistance)
                    {
                        diff = -turretDistance;
                        aimed = false;
                    }
                    else if (diff > turretDistance)
                    {
                        diff = turretDistance;
                        aimed = false;
                    }

                    // Apply distance

                    distance += (float)diff;

                    if (distance < minDistance)
                        distance = (float)minDistance;
                    else if (distance > maxDistance)
                        distance = (float)maxDistance;
                }

                // Calculate target's position angle

                difference.Normalize();

                float rotTarget = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg * -1;

                rotTarget += 90;

                if (rotTarget > 180)
                    rotTarget -= 360;

                // Calculate turret rotation

                float rotTurret = (turret.rotation.eulerAngles.z * -1) + 180;

                // Calculate difference

                float rot = rotTarget - rotTurret;
                if (rot < -180)
                    rot = rot + 360;
                else if (rot > 180)
                    rot = rot - 360;

                aimDir = rot;

                // Calculate turning amount

                if (rot > turretMovement)
                {
                    rot = (float)turretMovement;

                    aimed = false;
                }

                else if (rot < -turretMovement)
                {
                    rot = (float)-turretMovement;

                    aimed = false;
                }

                rot = -rot + turret.rotation.eulerAngles.z;

                if (rot < -180)
                    rot = rot + 360;
                else if (rot > 180)
                    rot = rot - 360;

                // Apply turning amount

                turret.rotation = Quaternion.Euler(0, 0, rot);

                // Test for shooting

                if (aimed && (target.position - firePoint.position).magnitude <= gun.range)
                {
                    Vector2 start = firePoint.position;
                    Vector2 end;
                    if (aimDirectly)
                    {
                        end = new Vector2(firePoint.position.x, firePoint.position.y) + GetOnCircle(rot, distance);

                        Ray2D ray = new Ray2D(start, end - start);
                        RaycastHit2D hit = Physics2D.Raycast(start, end - start, distance, gun.toHit);

                        if (hit.distance == 0)
                            Debug.LogError("Error: Weapon is aimed, but no target in raycast hit");

						// Check if there's a direct line of side to the enemy

                        if (hit.collider.GetComponent<Entity>() != null && hit.collider.GetComponent<Entity>().team != GetComponent<Entity>().team)
                        {
                            // Fire weapon

                            Vector2 pos = GetVector2(turret.position) + GetOnCircle(rot, distance);

                            currentlyFiring = true;
							int damageDealt = gun.CheckForShot(pos);
							if (damageDealt > 0)
								GetComponent<Entity>().squad.damageCaused += damageDealt;
                        }
						else if (target != null && gun.rateWhenHot < 1.0f && (float)gun.speed < (gun.timeTillShoot / 2))
						{
							// Turn minigun at half speed while aiming

							currentlyFiring = true;
							int damageDealt = gun.CheckForShot(GetVector2(turret.position) + GetOnCircle(rot, distance));
							if (damageDealt > 0)
								GetComponent<Entity>().squad.damageCaused += damageDealt;
						}
					}
                    else
                    {
                        end = new Vector2(firePoint.position.x, firePoint.position.y) + GetOnCircle(rot, gun.range);

                        Ray2D ray = new Ray2D(start, end - start);
                        RaycastHit2D hit = Physics2D.Raycast(start, end - start, gun.range, gun.toHit);
                        
                        if (hit.distance != 0 && hit.collider.GetComponent<Entity>() != null && hit.collider.GetComponent<Entity>().team != GetComponent<Entity>().team)
                        {
                            // Fire weapon

                            Vector2 pos = GetVector2(turret.position) + GetOnCircle(rot, distance);

							currentlyFiring = true;
							int damageDealt = gun.CheckForShot(pos);
                            if (damageDealt > 0)
	                            GetComponent<Entity>().squad.damageCaused += damageDealt;
						}
						else if (target != null && gun.rateWhenHot < 1.0f && (float)gun.speed < (gun.timeTillShoot / 2))
						{
							// Turn minigun at half speed while aiming

							int damageDealt = gun.CheckForShot(GetVector2(turret.position) + GetOnCircle(rot, distance));
							if (damageDealt > 0)
								GetComponent<Entity>().squad.damageCaused += damageDealt;
						}
					}
				}
				else if (target != null && gun.rateWhenHot < 1.0f && (float)gun.speed < (gun.timeTillShoot / 2))
				{
					// Turn minigun at half speed while aiming

					gun.CheckForShot(GetVector2(turret.position) + GetOnCircle(rot, distance));
				}
			} else
            {
                // Rotate in aim direction

                float currentRotation = turret.localRotation.eulerAngles.z - 180;


                // Calculate goal direction

                float goalDir = aimDir - tank.rotation.eulerAngles.z;
                
                // Calculate rotation

                float rot = goalDir - currentRotation;
                if (rot < -180)
                    rot = rot + 360;
                else if (rot > 180)
                    rot = rot - 360;

                // Apply maximum rotation speed
                if (rot > turretMovement)
                    rot = (float)turretMovement;
                else if (rot < -turretMovement)
                    rot = (float)-turretMovement;

                float newRot = currentRotation + rot + 180;
                if (newRot < 0)
                    newRot = newRot + 360;
                else if (newRot > 360)
                    newRot = newRot - 360;

                turret.localRotation = Quaternion.Euler(0, 0, newRot);
            }
        }
    }

    IEnumerator AI_Update()
    {
		int n = 0;
        while (GameMaster.friends == null || n <= 5)
        {
            yield return new WaitForSeconds(0.25f);
			n++;
        }

        if (explosive)
        {
            // Explosive movementMode (means it aims at a group of entities)
        }
        else
        {
			// Ballistic movementMode (means it aims at single entity)

	        // Check if there's any target aimed at

			if (target == null) {
				// No target

				// Check if target is available

				Entity targetEntity = GetBestTarget(turret.position);
				if (targetEntity != null) {
					// found new target

					target = targetEntity.transform;

					randomMoveIncoming = false;
				}
				// No target - check if it wants to turn randomly around yet
				else if (randomMoveIncoming && Time.time >= randomMoveAt) {
					// move turret randomly

					if (canTurn360) {
						do {
							aimDir = Random.Range(-180, +180);
						} while (Random.Range(0, 360) < Mathf.Abs(aimDir)); // It's more probable that the turret looks forward, because it looks strange, if it always looks back 
					} else {
						do {
							aimDir = Random.Range((float)-angleInDegrees, (float)+angleInDegrees);
						} while (Random.Range(0, (float)angleInDegrees * 2) < Mathf.Abs(aimDir)); // It's more probable that the turret looks forward, because it looks strange, if it always looks back 
					}

					randomMoveIncoming = false;
				} else if (randomMoveIncoming == false) {
					// No objective - move turret randomly around after delay

					randomMoveIncoming = true;

					randomMoveAt = Time.time + (Random.Range(0.5f, 1.5f) * aimRandomDelay);
				}
			}
			else
            {
	            if (target.GetComponent<Entity>() == null) // target was destroyed
					target = null;

				// already has a target

				// Check if target is aimable at
				// If yes - do nothing
				// If no - seek best target

				if (!hasLineOfSight(target))
                {
                    // no line of sight - seek best target

                    Entity bestTarget = GetBestTarget(turret.position);

                    if (bestTarget == null)
                    {
						// move turret randomly around

                        randomMoveIncoming = true;
                        randomMoveAt = Time.time + (Random.Range(0.5f, 1.5f) * aimRandomDelay);
                    }
                    else
                    {
	                    target = bestTarget.transform;
                    }
                }

	            afterAim = false;
            }

            yield return new WaitForSeconds(1f / updatesPerSecond);
        }
        StartCoroutine(AI_Update());
    }

    Transform GetClosestObject(Vector2 position, Transform[] objects)
    {
        Transform tMin = null;
        float minDist = Mathf.Infinity;
        foreach (Transform t in objects)
        {
            float dist = Vector2.Distance(t.position, position);
            if (dist < minDist)
            {
                tMin = t;
                minDist = dist;
            }
        }
        return tMin;
    }

	// Gets nearest target in line of sight or if none available just nearest target
    Entity GetBestTarget(Vector2 position)
    {
		// Get list of possible targets

        List<GameObject> possibleTargets;
        
        if (team == 0)
        {
            // player team

            possibleTargets = GameMaster.enemies;
        }
        else
        {
            // enemy team

            possibleTargets = GameMaster.friends;
        }

        Entity nearestTarget = null;
        Entity nearestAimableTarget = null; // The nearest target within line of sight

        float minDist = Mathf.Infinity;
        float minAimableDist = Mathf.Infinity;

		// Loop through
        foreach (GameObject g in possibleTargets) {
			if (g != null) {
				// Check target distance

				Transform t = g.transform;
                float dist = Vector2.Distance(t.position, position);
                if (dist > aimDistance)
	                continue;

				// Check if it could be nearest aimable target

				if (dist < minAimableDist)
				{
					// Check if in line of sight and within reach

					Vector2 start = firePoint.position;
					Vector2 end = t.position;

					Ray2D ray = new Ray2D(start, end - start);
					RaycastHit2D hit = Physics2D.Raycast(start, end - start, 1000f, gun.toHit);

					if (hit.distance == 0)
					{
						Debug.LogError("Error: No raycast hit. t = " + t.name);
					}
					else if (hit.transform == t && hit.distance <= maxDistance && hit.distance < minAimableDist) {
						// t is nearest aimable target yet

						nearestAimableTarget = t.GetComponent<Entity>();
						minAimableDist = hit.distance;
					}
				}

				// Check if it's nearest target otherwise

	            if (dist < minDist)
                {
					// nearest target yet

                    nearestTarget = t.GetComponent<Entity>();
                    minDist = dist;
                }
            }
        }

        if (nearestAimableTarget != null)
	        return nearestAimableTarget;
		else
	        return nearestTarget;
    }

	// Check whether there's a clear line of sight and the target is withing reach
    bool hasLineOfSight(Transform t)
    {
	    if (t == null)
		    return false;

	    Vector2 start = turret.position;
	    Vector2 dir = ((Vector2) t.position - start).normalized;

	    RaycastHit2D hit = Physics2D.Raycast(start + 1f * dir, dir, 1000f, gun.toHit);

	    if (hit.distance == 0)
		    Debug.Log("Error: No raycast hit. " + transform.name + " to " + t.name);

	    if (hit.transform == t && hit.distance <= maxDistance)
		    return true;
	    else
		    return false;
    }

    // Get a point at a circle
    private Vector2 GetOnCircle(float angle, float distance)
    {
        return new Vector2(GetXOnCircle(angle, distance), GetYOnCircle(angle, distance));
    }

    // Get a point (the X-coordinate) on a circle
    private float GetXOnCircle(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (distance * Mathf.Sin(angle));
    }

    // Get a point (the Y-coordinate) on a circle
    private float GetYOnCircle(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (-1f * distance * Mathf.Cos(angle));
    }

    // Converts Vector3 to Vector2 (x and y)
    private Vector2 GetVector2(Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }
}
