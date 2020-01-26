using System;
using UnityEngine;
using System.Collections;
using Pathfinding;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Vehicle))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Entity))]
public class Moving : MonoBehaviour {
	private int[] debugVar;
	[Header("References")]
	public Transform aStar;
	[HideInInspector] public Transform[] squadMembers; // All other members in the squad
	[Space(4)]

	[Header("Performance Settings")]
	public float updateRate = 2f; // How often per second the pathfinder updates
	public float brainUpdateRate = 5f; // How often per second the AI adjust its driving
	[Space(4)]

	[Header("Behavior Settings")]
	public bool moveWhenFighting = false; // If opponent is closer or equal than shootDistance, the entity will not move forward
	public bool turnToTarget = false; // When true the entity always looks at its target when within shootDistance
	public float circleProbability = 0.0f; // The probability at which the entity drives in a circle around the target
	public bool doMoveAroundWhenIdle = true; // Let the entity move a bit around when it's idle
	public float moveAroundWhenIdle = 60; // If the entity is idle, it moves to a random near location after approximately this time in seconds.
	[HideInInspector] public float followOpponent; // If the target moves away, the entity follows this far. Turn to 0 to not follow opponents
	[HideInInspector] public float shootDistance; // The ideal distance from which the entity fires; should be below its weapons range
	[HideInInspector] public float minDistance;
	[Space(4)]

	[Header("Drive Settings")]
	public float minAngle = 60; // The entity will only accelerate from / towards it's target if the angle is smaller than this
	public float maxAngle = 30; // Full acceleration if angle is less than this
	public float mostExactAngle = 2; // The entity will turn till this angle at its target
	public bool driveBackwards = true;
	[Space(4)]

	[Header("Local Avoidance Settings")]
	public float localAvoidanceDist = 1f; // How far away objects are considered
	public float localAvoidanceOffset = 1f; // How much the local avoidance is offset (to prioritize objects in front of the vehicle)
	[Space(4)]

	[Header("Path Finding Settings")]
	public float nextWaypointDistance = 0.5f;
	[Space(4)]

	// debug settings

	public bool ShowDebugInfo = false;
	public bool ShowClusters;

	// path finding status

	[HideInInspector] public Transform target;
	/*[HideInInspector]*/ public int mode = 0; // 0 = doing nothing, 1 = driving in idle, 2 = driving to goal, 3 = driving to squad, 4 = attacking
	[HideInInspector] public Path path;
	[HideInInspector] public Vector3 intendedPosition; // where the vehicle currently is or plans to be in a moment
	[HideInInspector] public Vector3 destDirection;

	// private references

	private Seeker seeker;
	private Rigidbody2D rb;
	private Vehicle veh;
	private float updateDelay;
	private int currentWaypoint = 0;
	private Vector2 targetPlace;
	private Squad squad;
	private AstarPath aStarPath;
	private List<Transform> nearEntities; // Used to not check all entities constantly
	public List<Transform> clusteredEntities; // All entities which are near enough to be the same cluster
	private Dictionary<EntityCluster, int> clusterSides;
	/*
     * Used to save whether to go around each cluster on the left or the right
     * (Otherwise the entity would constantly redecide)
     */

	// internal driving information

	private float rot = 0; // The current rotation (steering)
	private float acc = 0; // The current acceleration

	// internal debug information

	GUIStyle gizmosStyle;
	[HideInInspector] public bool hasStarted = false;

	void Start() {
		// Call dependencies

		seeker = GetComponent<Seeker>();
		rb = GetComponent<Rigidbody2D>();
		veh = GetComponent<Vehicle>();
		squad = transform.parent.gameObject.GetComponent<Squad>();
		aStarPath = aStar.GetComponent<AstarPath>();

		// Inits

		gizmosStyle = new GUIStyle();
		gizmosStyle.fontSize = 8;
		gizmosStyle.normal.textColor = new Color(.7f, 0f, 0f);

		nearEntities = new List<Transform>();
		clusteredEntities = new List<Transform>();
		clusterSides = new Dictionary<EntityCluster, int>();

		minDistance = GetComponent<Entity>().obstacleSize + GameMaster.minPassDist;

		// Convert to updates per second to delay in seconds

		updateDelay = 1f / updateRate;

		targetPlace = transform.position;

		StartCoroutine(UpdatePath());
		StartCoroutine(OnBrainUpdate());

		hasStarted = true;
	}

	public bool IsMoving() {
		return mode != 0;
	}

	public void SetMode(int newMode) {
		if (mode == 0 || newMode == 0) {
			mode = newMode;
			//try {
				GameMaster.ReconsiderEntityCluster(transform);
			//} catch (Exception e) {
			//	if (e != null && e.Message != null)
			//		Debug.LogError("hi " + e.Message);
			//}
		} else {
			mode = newMode;
		}

		destDirection = new Vector3();
	}

	private void FixedUpdate()
	{
		if (GameMaster.IsPaused()) return;
		if (path != null) {
			// Calculate if goal is reached

			if (currentWaypoint >= path.vectorPath.Count && mode != 0) {
				SetMode(0);

				path = null;
				//seeker.StartPath(transform.position, transform.position, DoNothing);
			} else if (currentWaypoint < path.vectorPath.Count) {
				// Distance between entity and next waypoint

				float dis = Vector3.Distance(transform.position, path.vectorPath[currentWaypoint]);
				if (dis < nextWaypointDistance) {
					currentWaypoint++;
				}
			}
		}

		// Apply driving instructions
		veh.Drive(acc, rot);
	}

	IEnumerator UpdatePath() {
		while (GameMaster.entities == null || GameMaster.IsPaused())
			yield return new WaitForSeconds(0.3f);

		// update near entities

		UpdateNearEntities();

		// Check if path must be changed

		// Check if it is too far away from squad

		if (!CheckSquad()) {

			// Check if the squad has a destination

			if (!CheckDestination()) {

				// Check if it is in idle and wants to move around

				CheckIdle();
			}
		}

		// Build new path

		if (target != null) {
			seeker.StartPath(transform.position, target.position, OnPathComplete);
		} else if (mode != 0) {
			seeker.StartPath(transform.position, targetPlace, OnPathComplete);
		}

		// debug

		ShowDebugInfo = false;

		// recurse

		yield return new WaitForSeconds(updateDelay);
		StartCoroutine(UpdatePath());
	}

	public void UpdateNearEntities() {
		float minPassDist = GameMaster.minPassDist;
		float minClusterDist = minPassDist + GetComponent<Entity>().obstacleSize;
		EntityCluster thisCluster = GetComponent<Entity>().cluster;

		foreach (GameObject g in GameMaster.entities) {
			if (g == null)
			{
				GameMaster.entities.Remove(g);
				break;
			}

			if (g.GetComponent<Entity>() == null)
			{
				GameMaster.entities.Remove(g);
				break;
			}

			Transform t = g.transform;

			if (t == transform)
				continue;

			float dist = Vector2.Distance(t.transform.position, transform.position);
			if (nearEntities.Contains(t.transform)) {
				if (dist > 7) {
					nearEntities.Remove(t.transform);

					// Check if their cluster data should also be deleted
					// Check if it was the last entity from it's cluster

					EntityCluster cluster = t.GetComponent<Entity>().cluster;

					if (cluster == null)
					{
						Debug.LogError("cluster fragment");

						GameMaster.ReconsiderEntityCluster(transform);
						cluster = t.GetComponent<Entity>().cluster;
					}
					bool last = true;
					foreach (Transform nearEntity in nearEntities) {
						if (cluster.Contains(nearEntity)) {
							last = false;
							break;
						}
					}

					if (last) {
						clusterSides.Remove(cluster);
					}

					// Remove from cluster (this code should never be executed under normal circumstances)

					if (clusteredEntities.Contains(t)) {
						// Remove clustered entities from former cluster

						foreach (Transform clean in clusteredEntities) {
							try {
								thisCluster.RemoveEntity(clean);
							} catch (Exception ex) {
								Debug.LogError(ex);
							}
						}
						thisCluster.RemoveEntity(transform);

						// Reconsider cluster for nearby entities

						foreach (Transform companion in clusteredEntities) {
							GameMaster.ReconsiderEntityCluster(companion);
						}

						// Remove from clustered entities
						clusteredEntities.Remove(t);
						if (t.GetComponent<Moving>() != null)
							t.GetComponent<Moving>().clusteredEntities.Remove(transform);

						// Reconsider own cluster

						GameMaster.ReconsiderEntityCluster(transform);
					}
				} // check if should be clustered
				else if (!clusteredEntities.Contains(t) && dist < (minClusterDist + t.GetComponent<Entity>().obstacleSize - 0.15f)) {
					clusteredEntities.Add(t);
					if (t.GetComponent<Moving>() != null)
						t.GetComponent<Moving>().clusteredEntities.Add(transform);
					
					GameMaster.ReconsiderEntityCluster(transform);
					GameMaster.ReconsiderEntityCluster(t);
				}
			} else if (dist < 7) {
				nearEntities.Add(t.transform);

				// Check if should be moved to same cluster

				if (dist < (minClusterDist + t.GetComponent<Entity>().obstacleSize - 0.15f)) {
					GameMaster.ReconsiderEntityCluster(transform);
				}
			}
		}

		// Sort list

		nearEntities.Sort(new PositionComparer(transform));

		// Check if clustered entities are still in cluster range

		List<Transform> toBeRemoved = new List<Transform>();
		for (var i = 0; i < clusteredEntities.Count; i++) {
			Transform entity = clusteredEntities[i];
			if (entity.GetComponent<Entity>() == null) // means it is dead
			{
				clusteredEntities.Remove(entity);
				continue;
			}

			float dist = (entity.position - transform.position).magnitude;

			if (dist > (minClusterDist + entity.GetComponent<Entity>().obstacleSize + 0.15f)) {
				// should be removed

				toBeRemoved.Add(entity);
			}
		}

		if (toBeRemoved.Count > 0) {
			// Remove clustered from former cluster
			Transform[] companions = thisCluster.Where(transform => nearEntities.Contains(transform)).ToArray();

			foreach (Transform clean in companions) {
				try
				{
					thisCluster.RemoveEntity(clean);
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
			thisCluster.RemoveEntity(transform);
			
			// Reconsider cluster for nearby entities

			foreach (Transform companion in companions)
			{
				GameMaster.ReconsiderEntityCluster(companion);
			}

			// Remove too distant entities

			foreach (var t in toBeRemoved) {
				clusteredEntities.Remove(t);
				float distance = (t.position - transform.position).magnitude;
				if (t.GetComponent<Moving>() != null)
					t.GetComponent<Moving>().clusteredEntities.Remove(transform);
			}

			// Reconsider own cluster

			GameMaster.ReconsiderEntityCluster(transform);
		}

		if (toBeRemoved.Count != 0)
			GameMaster.ReconsiderEntityCluster(transform);

		// Debug

		if (ShowDebugInfo) {
			// Mode
			Vector3 pos = transform.position;
			pos += new Vector3(.5f, -.5f, 0);

			// Destination
			if (mode == 1)
				DrawSquare(targetPlace, Color.green, (1 / updateRate));
			else if (mode == 2)
				DrawSquare(targetPlace, new Color(1, 0, 1), (1 / updateRate));
		}
	}

	private void OnPathComplete(Path p) {
		// When Path calculation was completed

		if (p.error == true) {
			Debug.LogError(p.errorLog + "\nmode:" + mode + "pos t: " + transform.position + "\ntarget " + targetPlace);
		}

		path = p;

		// Select sensible first node

		if (p.vectorPath.Count > 1)
			currentWaypoint = 1;
		for (int i = 1; i < p.vectorPath.Count; i++) {
			if ((transform.position - p.vectorPath[i]).magnitude < nextWaypointDistance)
				currentWaypoint = i;
			else
				break;
		}
	}

	private IEnumerator OnBrainUpdate() {
		while (GameMaster.entities == null || GameMaster.IsPaused())
			yield return new WaitForSeconds(0.5f);

		try {
			// Check if attacking

			if (squad.combatMode == Squad.CombatMode.Attacking || squad.combatMode == Squad.CombatMode.Pursuing)
			{
				// Stay still when it can fire and is near enough to its target

				EntityAiming aiming = GetComponent<EntityAiming>();
				if (aiming != null && aiming.target != null)
				{
					float targetDist = (transform.position - aiming.target.position).magnitude;
					
					if (mode == 0)
					{
						if (!aiming.currentlyFiring || targetDist > aiming.maxDistance - 2.5)
						{
							float distance = ((Vector2) transform.position - targetPlace).magnitude;
							if (distance > 1)
							{
								SetMode(2);
							}
						}
					}
					else
					{
						if (aiming.currentlyFiring && targetDist < aiming.maxDistance - 2.5)
						{
							SetMode(0);
						}
					}
				}
			}

			// Check movementMode

			if (mode == 0) // not moving
			{
				intendedPosition = transform.position;

				// Brake, if fast

				float forwardVelocity = transform.InverseTransformDirection(rb.velocity).y;

				if (Mathf.Abs(forwardVelocity) > 0.2) {
					// Brake

					acc = -1.5f * forwardVelocity;
					acc = acc > -1 ? acc : -1;
					acc = acc < +1 ? acc : +1;
				} else {
					// Do not accelerate

					acc = 0;
				}

				// Steer forward

				rot = 0;
			} else // moving
			{
				if (path != null && path.vectorPath.Count > currentWaypoint) {
					// Calculate desired direction

					Vector3 pathDirection = (path.vectorPath[currentWaypoint] - transform.position).normalized;
					if (destDirection.magnitude > 0.01)
						destDirection = CalcLocalAvoidance(pathDirection, (destDirection + pathDirection) / 2);
					else
						destDirection = CalcLocalAvoidance(pathDirection, pathDirection);

					if (ShowDebugInfo) {
						Debug.DrawLine(transform.position, transform.position + pathDirection, Color.cyan, (1 / brainUpdateRate));
						Debug.DrawLine(transform.position, transform.position + destDirection, new Color(.8f, 0f, .8f), (1 / brainUpdateRate));
					}

					// Turn

					float angularDiff = Vector2.Angle(destDirection, transform.up);
					Vector3 cross = Vector3.Cross(destDirection, transform.up);

					if (cross.z > 0)
						angularDiff = 360 - angularDiff;

					if (angularDiff > 180)
						angularDiff -= 360;

					angularDiff *= -1;

					if (angularDiff >= mostExactAngle || angularDiff <= -mostExactAngle) {
						// Steer

						if (angularDiff < 0) {
							rot = -1;
						} else {
							rot = 1;
						}
					} else {
						rot = angularDiff / mostExactAngle / 2;
					}

					// Accelerate

					if (-minAngle < angularDiff && angularDiff < minAngle) {
						// Move forward

						if (angularDiff == 0) {
							acc = 1;
						} else {
							if (angularDiff < 0)
								angularDiff *= -1;

							if (minAngle - maxAngle != 0)
								acc = -angularDiff / (minAngle - maxAngle) + minAngle / (minAngle - maxAngle);
							else
								acc = 0;

							acc = acc > 0 ? acc : 0;
							acc = acc < 1 ? acc : 1;
						}

					} else if (driveBackwards == true && (angularDiff > (180 - minAngle) || angularDiff < (-180 + minAngle))) {
						// Move backwards

						if (angularDiff < 0)
							angularDiff *= -1;

						if (minAngle - maxAngle != 0)
							acc = (180 - angularDiff) / (minAngle - maxAngle) - minAngle / (minAngle - maxAngle);
						else
							acc = 0;

						acc = acc < 0 ? acc : 0;
						acc = acc > -1 ? acc : -1;
					} else {
						// Brake, if fast

						float forwardVelocity = transform.InverseTransformDirection(rb.velocity).y;

						if (Mathf.Abs(forwardVelocity) > 0.2) {
							// Brake (hard)

							acc = -3.5f * forwardVelocity;

							acc = acc > -1 ? acc : -1;
						} else {
							// Do not accelerate

							acc = 0;
						}
					}
				}
			}
		} catch (System.ArgumentOutOfRangeException e) {
			Debug.LogError(e.StackTrace);
			Debug.LogError("movementMode " + mode + "; currentWaypoint " + currentWaypoint + "; number of waypoints " + path.vectorPath.Count);
		}

		yield return new WaitForSeconds(1f / brainUpdateRate);
		StartCoroutine(OnBrainUpdate());
	}

	private bool CheckSquad() {
		// Check if it must move to squad

		if (((new Vector2(transform.position.x, transform.position.y) - squad.GetPosition()).magnitude > squad.movementRadius + 0.5) && mode < 3)
		// It won't try to move to the squad, when it's already moving to the squad, or attacking opponents
		{ // move to squad
		  // Is too far away from squad

			bool searching = true;
			int x = 25;

			do {
				// Create random positions around the squad, until one fits

				Vector2 dir = Random.insideUnitCircle * squad.movementRadius * 0.7f;

				Vector2 newPos = squad.GetPosition() + dir;

				// Find new target position

				float probability = Mathf.Clamp((GetClosestObjectDistance(newPos, GameMaster.entities.ToArray(), true) - 2) * 0.05f, 0.005f, 0.4f); // Places not near at other entities are more probable

				probability *= Mathf.Clamp(1 - ((new Vector2(transform.position.x, transform.position.y) - newPos).magnitude / 12), 0.2f, 1); // Nearer places are more probable

				if (aStarPath.GetNearest(newPos).node.Walkable && (probability > (float)Random.Range(0f, 1f))) {
					// Found position

					SetMode(3);
					searching = false;

					targetPlace = newPos;
				}

				x--;
			} while (searching && x > 0);

			return true;
		}

		// Check if it has moved to squad

		else if (mode == 3 && (new Vector2(transform.position.x, transform.position.y) - squad.GetPosition()).magnitude < (squad.movementRadius - 1)) {
			// It returned to squad

			SetMode(0);
		}

		if (mode == 3)
			return true;
		else
			return false;
	}

	private bool CheckDestination() {
		// Check if squad has a destination

		if (mode == 2) {
			if (squad.movementMode == Squad.MovementMode.Moving)
			{
				// Check if target position is still accordant with squad target

				float targetDist = (targetPlace - squad.targetPosition).magnitude;
				if (targetDist < squad.movementRadius) {
					// accordant

					return true;
				}
			}
			else {
				// Squad finished moving

				SetMode(1);

				return true;
			}
		} else if (squad.movementMode != Squad.MovementMode.Moving)
			return false;

		// Check if already moved

		Vector2 pos = new Vector2(transform.position.x, transform.position.y);

		float dist = (squad.targetPosition - pos).magnitude;

		if (squad.combatMode == Squad.CombatMode.Pursuing && squad.attackedSquad != null) {
			if (dist < squad.attackedSquad.movementRadius)
				return true;
		}
		else {
			if (dist < squad.movementRadius)
				return true;
		}


		// Try to maintain order

		bool foundPos = false;
		Vector2 newPos = pos + (squad.targetPosition - squad.GetPosition());

		if (aStarPath.GetNearest(newPos).node != null &&
			aStarPath.GetNearest(newPos).node.Walkable) {
			// Test if near

			if (GetClosestObjectDistance(newPos, GameMaster.entities.ToArray(), true) < 2) {
				foundPos = true;
			}
		}

		// Look for random place

		int x = 25;
		while ((!foundPos) && x > 0) {
			// Create random positions around the squad, until one fits

			Vector2 dir = new Vector2();

			if (squad.combatMode == Squad.CombatMode.Pursuing && squad.attackedSquad != null)
				dir = Random.insideUnitCircle * squad.attackedSquad.movementRadius;
			else
				dir = Random.insideUnitCircle * squad.movementRadius;

			newPos = squad.targetPosition + dir;

			// Find new target position

			float probability = Mathf.Clamp((GetClosestObjectDistance(newPos, GameMaster.entities.ToArray(), true) - 2) * 0.15f, 0.005f, 1f); // Places not near at other entities are more probable

			if (aStarPath.GetNearest(newPos).node != null &&
				aStarPath.GetNearest(newPos).node.Walkable && (probability > Random.Range(0f, 1f))) {
				// Found position
				foundPos = true;
			}

			x--;
		}

		if (foundPos) {
			targetPlace = newPos;

			SetMode(2);

			return true;
		} else {
			return false;
		}
	}

	private void CheckIdle() {
		if (mode != 0)
			return;

		if (!doMoveAroundWhenIdle)
			return;

		targetPlace = transform.position;

		// Check if it will move

		float ran = Random.Range(0f, 1f);

		if (moveAroundWhenIdle == 0 || updateRate == 0)
			return;

		float prob = (1f / updateRate) / moveAroundWhenIdle;

		if (ran >= prob)
			return;

		// Move randomly around

		// Find new target location

		bool searching = true;
		int x = 25;

		while (searching && x > 0) {
			// Create random positions around the squad, until one fits

			Vector2 dir = Random.insideUnitCircle * squad.movementRadius;

			Vector2 newPos = squad.GetPosition() + dir;

			// Find new target position

			float probability = Mathf.Clamp((GetClosestObjectDistance(newPos, GameMaster.entities.ToArray(), true) - 2) * 0.15f, 0.005f, 1f); // Places not near at other entities are more probable

			if (aStarPath.GetNearest(newPos).node.Walkable && (probability > Random.Range(0f, 1f))) {
				// Found position

				SetMode(1);
				searching = false;

				targetPlace = newPos;
			}

			x--;
		}
	}

	// Takes in a vector to where the entity wants to moves, gives back a vector which also factors in local avoidance
	private Vector3 CalcLocalAvoidance(Vector3 regularDirection, Vector3 previousDirection) {
		if (nearEntities.Count == 0)
			return regularDirection;

		float combinedObstruction = 0f;
		List<EntityCluster> obstacles = new List<EntityCluster>();
		List<float> obstructionValues = new List<float>();

		try {
			foreach (Transform t in nearEntities) {
				if (t.GetComponent<Entity>() == null || t.GetComponent<Entity>().cluster == null) // means it is dead
				{
					nearEntities.Remove(t);
					if (clusteredEntities.Contains(t))
						clusteredEntities.Remove(t);
					continue;
				}

				Vector2 diff = t.position - transform.position;

				diff = Quaternion.Euler(0, 0, Vector2.SignedAngle(previousDirection + regularDirection, Vector2.up)) * diff;

				/* Distort Vector, to 
				 *  - prioritize objects in front of the tank, rather than the back
				 *  - prioritize objects directly in the way, instead of left or right to the way
				*/
				diff.y -= localAvoidanceOffset;
				diff.y /= 2;

				float obstruction = 1f - (diff.magnitude / (localAvoidanceDist * t.GetComponent<Entity>().cluster.GetObstacleSizeWithout(transform)));
				if (obstruction < 0)
					obstruction = 0;

				if (obstruction > 0.03f) {
					// t is an obstacle

					// Check if it is very near of other obstacles (so the vehicle can't pass between them)

					EntityCluster cluster = t.GetComponent<Entity>().cluster;
					if (cluster.Contains(transform)) {
						// It's in the same cluster, so the cluster must be split up
						// Check if already indexed

						bool indexed = false;
						EntityCluster container = null;

						foreach (EntityCluster obstacle in obstacles) {
							if (obstacle.Contains(t) && !obstacle.Contains(transform)) {
								indexed = true;
								container = obstacle;
							}
						}

						if (indexed) {
							obstructionValues[obstacles.IndexOf(container)] += obstruction;
						} else {
							// create new (temporary) cluster

							EntityCluster tempCluster = new EntityCluster(new List<Transform>() { t }, true);
							for (int i = 0; i < tempCluster.Count; i++) {
								Transform companion = tempCluster[i];

								if (companion.GetComponent<Moving>() == null)
									continue;

								foreach (Transform clustered in companion.GetComponent<Moving>().clusteredEntities) {
									if (!tempCluster.Contains(clustered) && clustered != transform) {
										tempCluster.AddTemporary(clustered);
										//if (ShowDebugInfo)
										//	Debug.DrawLine(tempCluster[i].position, clustered.position, new Color(1f, .6f, .9f), 1.1f / brainUpdateRate);
									}
								}

								//if (tempCluster.Count == 1 && ShowDebugInfo)
								//	DrawSquare(companion.position - new Vector3(.1f, .1f, 0), new Color(1f, .6f, .9f), 1.1f / brainUpdateRate);
							}
							obstacles.Add(tempCluster);
							obstructionValues.Add(obstruction);
						}
					} else
					if (obstacles.Contains(cluster)) {
						obstructionValues[obstacles.IndexOf(cluster)] += obstruction;
					} else {
						obstacles.Add(cluster);
						obstructionValues.Add(obstruction);
					}

					combinedObstruction += obstruction;
				}
			}
		}
		catch (InvalidOperationException ex) {}
		catch (NullReferenceException ex) {
			Debug.LogError(ex.Message);
		}

		// factor in cluster size

		if (combinedObstruction < 0.05f)
			return regularDirection;
		if (combinedObstruction > 1f)
			combinedObstruction = 1f;

		// Combine vectors

		float directionCorrection = 0; // In degrees;
		for (var i = 0; i < obstacles.Count; i++) {
			// Check whether the obstacle is on the left or right

			int side = 0; // -1 = left, +1 = right

			Vector2 diff = obstacles[i].GetAvgPosWithout(transform) - (Vector2)transform.position;
			diff = Quaternion.Euler(0, 0, Vector2.SignedAngle(previousDirection, Vector2.up)) * diff;

			if (clusterSides.ContainsKey(obstacles[i])) {
				// It has encountered the cluster recently and the side was saved
				// The previously chosen side is more probable to be picked again to reduce direction switching

				int oldSide = clusterSides[obstacles[i]];
				int newSide = diff.x < 0 ? -1 : 1;

				if (newSide == oldSide) {
					// new and old side agree

					side = clusterSides[obstacles[i]];
				} else {
					// If the overall obstruction is high, the vehicle will likely keep it's side
					// If the target direction is very different from the former side, it will likely change sides

					float angle = Vector2.SignedAngle(transform.up, diff);
					angle = angle > 0 ? angle : -angle; // absolute value

					if (angle > combinedObstruction * 45) {
						// Change side

						side = newSide;
						clusterSides[obstacles[i]] = newSide;
					} else {
						// keep side

						side = oldSide;
					}

					side = clusterSides[obstacles[i]];
				}
			} else {
				// First time, check is needed

				if (diff.x < 0) {
					// left

					side = -1;
					clusterSides.Add(obstacles[i], side);
				} else {
					// right

					side = 1;
					clusterSides.Add(obstacles[i], side);
				}
			}

			//// Debug
			//if (ShowDebugInfo)
			//{
			//	// show if the cluster is avoided by going left (red) or right (blue) by line color
			//	// number of lines shows the obstruction value (1 - 10)

			//	int number = Mathf.RoundToInt(obstructionValues[i] * obstructionValues[i] * 9f);
			//	Color color = (side == -1) ? Color.blue : Color.red;
			//	Vector3 offset = new Vector3(0, .1f, 0);
			//	foreach (Transform entity in obstacles[i]) {
			//		Vector3 origDirection = (entity.position - transform.position).normalized * 0.01f;
			//		Vector3 perpDirection = Quaternion.Euler(0, 0, 90) * origDirection;

			//		for (int n = 0; n <= number; n++)
			//		{
			//			Vector3 offset2 = perpDirection * (n - number / 2f);
			//			Debug.DrawLine(transform.position + offset2, entity.position + offset2, color, (1 / brainUpdateRate));
			//		}
			//	}
			//}

			directionCorrection += obstructionValues[i] * 105 * side;
		}
		// Clamp
		directionCorrection = Mathf.Clamp(directionCorrection, -120, 120);

		// Adjust vector

		regularDirection = Quaternion.Euler(0, 0, directionCorrection) * regularDirection;
		//CheckObjectAvoidance(previousDirection);
		return regularDirection;
	}

	/*
     * Used to not run into map objects while avoiding other entities
     */
	private void CheckObjectAvoidance(Vector2 direction) {
		// When avoiding entities, the Moving object must choose whether to surpass them on the left or the right.
		// It must be checked though, if there is a map object on either side, so the vehicle doesn't collide.
		// This is done by casting out multiple rays to each side and check if the end points are accessible.

		Vector2 position = transform.position;
		int resolution = 6; // How many rays are on each side

		// Prepare ray casting
		float step = -360f / (resolution * 2); // How many degrees one ray differs from the next
		direction = Quaternion.Euler(0, 0, step * -0.5f) * direction; // Rotate half a step to get a symmetric distribution

		int i = 0;

		// left side

		for (; i < resolution; i++) {
			// Rotate

			direction = Quaternion.Euler(0, 0, step) * direction;
			Vector2 checkmark = position + direction;
		}

		// right side

		for (; i < resolution * 2; i++) {
			// Rotate

			direction = Quaternion.Euler(0, 0, step) * direction;
			Vector2 checkmark = position + direction;

			// Debug

			if (ShowDebugInfo && Application.isPlaying)
				DrawSizedSquare(checkmark, Color.black, 0.33f, 0.05f);
		}
	}

	void OnDrawGizmos() {
#if UNITY_EDITOR
		if (!Application.isPlaying)
			return;

		// Draw cluster connections

		EntityCluster cluster = GetComponent<Entity>().cluster;

		if (ShowClusters) {
			foreach (Transform e in cluster) {
				Debug.DrawLine(transform.position, e.transform.position, new Color(1, 0.5f, 0.5f));
			}
		}

		// Show cluster size

		if (ShowDebugInfo && cluster != null) {
			Handles.color = Color.gray;
			Vector3 position = transform.position + new Vector3(.5f, -.5f, 0);
			Handles.Label(position, "movementMode " + ModeToString());
		}

		if (!ShowDebugInfo)
			return;
#endif
	}

	void OnDrawGizmosSelected()
	{
		ShowDebugInfo = true;
	}

	/* Returns the description in word form of the current movementMode */
	private string ModeToString() {
		switch (mode) {
			// 0 = doing nothing, 1 = driving in idle, 2 = driving to goal, 3 = driving to squad, 4 = attacking
			case 0:
				return "nothing";
			case 1:
				return "idle";
			case 2:
				return "destination";
			case 3:
				return "squad reformation";
			case 4:
				return "attacking";
			default:
				return "unknown movementMode";
		}
	}

	/*
     * Compares objects based on the distance to a mother object
     */
	private class PositionComparer : IComparer<Transform> {
		private Transform mother;

		public PositionComparer(Transform mother) {
			this.mother = mother;
		}

		public int Compare(Transform x, Transform y) {
			float distX = (mother.position - x.position).magnitude;
			float distY = (mother.position - y.position).magnitude;

			return distX < distY ? -1 : 1;
		}
	}

	// Get a point at a circle
	public static Vector2 GetOnCircle(float angle, float distance) {
		return new Vector2(GetXOnCircle(angle, distance), GetYOnCircle(angle, distance));
	}

	// Get a point (the X-coordinate) on a circle
	public static float GetXOnCircle(float angle, float distance) {
		angle *= Mathf.PI / 180;

		return (distance * Mathf.Sin(angle));
	}

	// Get a point (the Y-coordinate) on a circle
	public static float GetYOnCircle(float angle, float distance) {
		angle *= Mathf.PI / 180;

		return (distance * Mathf.Cos(angle));
	}

	private static Transform GetClosestObject(Vector2 position, GameObject[] objects) {
		Transform tMin = null;
		float minDist = Mathf.Infinity;
		foreach (GameObject g in objects) {
			Transform t = g.transform;

			float dist = Vector2.Distance(t.position, position);
			if (dist < minDist) {
				tMin = t;
				minDist = dist;
			}
		}
		return tMin;
	}

	public static float GetClosestObjectDistance(Vector2 position, GameObject[] objects, bool considerTarget) {
		float minDist = Mathf.Infinity;
		foreach (GameObject g in objects) {
			if (g == null)
				continue;
			Transform t = g.transform;

			if (t != null) {
				if (considerTarget && t.GetComponent<Moving>() != null && t.GetComponent<Moving>().mode != 0) {
					// Use objects target position, not its actual one

					float dist = Vector2.Distance(t.GetComponent<Moving>().targetPlace, position);
					if (dist < minDist) {
						minDist = dist;
					}
				} else {
					// Use objects actual position

					float dist = Vector2.Distance(t.position, position);
					if (dist < minDist) {
						minDist = dist;
					}
				}
			}
		}
		return minDist;
	}

	private static void DoNothing(Path p) { }

	private static void DrawSquare(Vector2 pos) {
		// Draw a yellow Debug-Line square with one frame length

		// Draw a Debug-Line square

		// Calculate corners

		float size = 0.5f;

		Vector2 topLeft, topRight, lowerLeft, lowerRight;

		topLeft = new Vector2(pos.x - size, pos.y + size);
		topRight = new Vector2(pos.x + size, pos.y + size);
		lowerLeft = new Vector2(pos.x - size, pos.y - size);
		lowerRight = new Vector2(pos.x + size, pos.y - size);

		// Draw Lines

		Debug.DrawLine(topLeft, topRight, Color.black);
		Debug.DrawLine(lowerLeft, lowerRight, Color.black);
		Debug.DrawLine(topLeft, lowerLeft, Color.black);
		Debug.DrawLine(topRight, lowerRight, Color.black);
	}

	private static void DrawSquare(Vector2 pos, Color color) {
		// Draw a  Debug-Line square with fixed length

		DrawSquare(pos, color, 0.5f);
	}

	private static void DrawSquare(Vector2 pos, Color color, float duration) {
		DrawSizedSquare(pos, color, duration, 0.5f);
	}

	private static void DrawSizedSquare(Vector2 pos, Color color, float duration, float size) {

		// Draw a Debug-Line square

		// Calculate corners

		duration /= 2;

		Vector2 topLeft, topRight, lowerLeft, lowerRight;

		topLeft = new Vector2(pos.x - size, pos.y + size);
		topRight = new Vector2(pos.x + size, pos.y + size);
		lowerLeft = new Vector2(pos.x - size, pos.y - size);
		lowerRight = new Vector2(pos.x + size, pos.y - size);

		// Draw Lines

		Debug.DrawLine(topLeft, topRight, color, duration);
		Debug.DrawLine(lowerLeft, lowerRight, color, duration);
		Debug.DrawLine(topLeft, lowerLeft, color, duration);
		Debug.DrawLine(topRight, lowerRight, color, duration);
	}
}
