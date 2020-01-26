using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using System;

public class Squad : MonoBehaviour {
	/*[HideInInspector]*/
	public List<Transform> members;

	[Header("Activation Settings")]
	public bool isPlayer = false; // used to make the player be regarded as a squad in combat assessment, but still have own control
	[Space(4)]

	[Header("References")]
	public Transform aStar;
	[Space(4)]

	[Header("Performance Setting")]
	public float updatesPerSecond = 1f;
	[Space(4)]

	[Header("Movement Settings")]
	public int movementRadiusMultiplier = 5; // The maximum radius at which the movers can move around the targetPosition / around the squads center
	public bool allowFreeMovement = true; // Whether the squad can flee or attack. When false it will strictly perform the task it is bound to.
	public int randomMoveEverySeconds = 20;
	public int randomMovementDistance;
	[Space(4)]

	[Header("Combat Settings")]
	public int team; // 0 = friend (player); 1 = enemy (computer)
	public float bloodlustMultiplier;
	public float bloodlustMin;
	public float bloodlustRaisePerMinute = 0.15f;
	[Space(4)]

	[Header("Debug Settings")]
	public bool showDebugInfo = false;
	[Space(4)]

	// Status variables
	[Header("Status variables")]

	// Movement variables
	private AstarPath aStarPath;
	public MovementMode movementMode = MovementMode.Stationary;
	[HideInInspector] public Vector2 position;
	[HideInInspector] public Vector2 targetPosition;
	[HideInInspector] private bool hasTarget = false;
	private Transform targetObject;
	[HideInInspector] public float movementRadius; // How far entities are allowed to move from the center
	public bool mobile = true; // Immobile squads are e.g. those containing gun turrets

	// Squad health
	/*[HideInInspector]*/
	public float combatPower; // The estimated strength of the squad
	private float maxHealth;

	// Combat variables
	public bool inCombat = false;
	public CombatMode combatMode;
	public float pursuingForSeconds; // How long the current opponent has been pursued
	public bool fleeingFromCluster; // Whether fleeing from a cluster fight DEBUG ONLY public
	public int fleeingCounter = 0; // How many updates the squad has been fleeing for
	public double bloodlust; // Determines how offensive the squad acts, changes during the game
							 /*[HideInInspector]*/
	public List<Squad> squadsInCombatWith;
	/*[HideInInspector]*/
	public Squad attackedSquad;
	[HideInInspector] public Squad fleeingFrom;
	[HideInInspector] public int damageReceived = 0; // How much damage was dealt to the team since last update (for bloodlust calculation)
	[HideInInspector] public int damageCaused = 0;   // How much damage was dealt by the team since last update (for bloodlust calculation)
	public SquadCluster cluster;
	public bool hasCluster; // DEBUG ONLY
	public List<Transform> clusteredSquads;

	// Lifetime variables
	[HideInInspector] public bool spawned = false;

	public int debugger = 0;

	public enum MovementMode {
		Stationary,
		Moving,
		Normal,
		Following
	}

	public enum CombatMode {
		None,
		Attacking,
		Pursuing,
		Fleeing,
		ComingToHelp
	}

	// Use this for initialization
	public void Start() {
		if (isPlayer) {
			// init standard variables

			movementRadius = 4f;
			position = transform.position;
			members = new List<Transform>() { transform };

			StartCoroutine(UpdateSquad());

			return;
		}

		// Add members

		members = new List<Transform>();

		for (int i = 0; i < transform.childCount; i++)
			members.Add(transform.GetChild(i));

		// Set team

		foreach (Transform t in members) {
			Entity entity = t.GetComponent<Entity>();

			entity.team = team;

			if (t.GetComponent<Moving>() == null)
				mobile = false;
		}

		// Calculate position
		{
			// Squad position is the average of its members position

			position = new Vector2(0, 0);

			foreach (Transform t in members) {
				position.x += t.position.x;
				position.y += t.position.y;
			}

			position /= members.Count;
		}

		// Calculate values

		movementRadius = movementRadiusMultiplier * Mathf.Sqrt(members.Count);
		CalcCombatPower();

		// Start update sequence except when in edit movementMode

		bool avoid = false;

#if UNITY_EDITOR
		avoid = !EditorApplication.isPlaying;
#endif

		if (!avoid) {
			StartCoroutine(UpdateSquad());
		}

		// Calculate max health

		maxHealth = 0;
		foreach (Transform member in members) {
			maxHealth += member.GetComponent<Entity>().maxHealth;
		}

		// Inits

		bloodlust = bloodlustMultiplier;
		aStarPath = aStar.GetComponent<AstarPath>();
		squadsInCombatWith = new List<Squad>();
	}

	// DEBUG ONLY
	private void Update() {
		hasCluster = cluster != null;
		if (hasCluster) {
			clusteredSquads = new List<Transform>();
			foreach (Transform t in cluster) {
				clusteredSquads.Add(t);
			}
		}
	}

	IEnumerator UpdateSquad() {
		yield return new WaitForSeconds(1 / updatesPerSecond);

		if (GameMaster.IsPaused()) {
			yield return new WaitForSeconds(0.3f / updatesPerSecond);
			StartCoroutine(UpdateSquad());
		}

		if (isPlayer) {
			try {
				position = transform.position;

				// Assess combat power

				combatPower = 3f;
				CheckCombat();
				CheckHelp();
			} catch (Exception e) {
				Debug.LogError(e.Message);
			}

			yield return new WaitForSeconds(1 / updatesPerSecond);
			StartCoroutine(UpdateSquad());
		} else {
			try {

				// Calculate position

				if (movementMode != MovementMode.Stationary) // If defending, the squad position stays the same
			{
					// Squad position is the average of its members position

					position = new Vector2(0, 0);

					foreach (Transform t in members) {
						position.x += t.position.x;
						position.y += t.position.y;
					}

					position.x /= members.Count;
					position.y /= members.Count;
				}

				// Update stats

				CalcCombatPower();

				if (mobile) {

					bloodlust = GetBloodlust();
					CheckCombat();
					CheckHelp();

					// Check if Following

					if (movementMode == MovementMode.Following) {
						targetPosition = targetObject.position;
						hasTarget = true;
					}

					// Manage moving

					if (combatMode == CombatMode.Attacking || combatMode == CombatMode.Pursuing || combatMode == CombatMode.ComingToHelp) {
						if (attackedSquad == null) {
							combatMode = CombatMode.None;
							targetPosition = GetPosition();
							movementMode = MovementMode.Stationary;
						} else {

							// Check if cluster is immobile

							if (combatMode != CombatMode.Pursuing && cluster != null && cluster.Where(squad => !squad.GetComponent<Squad>().mobile).Any()) {
								Squad immoble = cluster.Where(squad => !squad.GetComponent<Squad>().mobile).ToArray()[0].GetComponent<Squad>();

								float dist = (immoble.GetPosition() - GetPosition()).magnitude - movementRadius;
								if (dist > 0) {
									movementMode = MovementMode.Moving;
									targetPosition = immoble.GetPosition();
									hasTarget = true;
								} else {
									targetPosition = immoble.GetPosition();
									movementMode = MovementMode.Stationary;
									hasTarget = false;
								}
							} else {
								movementMode = MovementMode.Moving;
								targetPosition = attackedSquad.GetPosition();
								hasTarget = true;
							}
						}
					} else if (combatMode == CombatMode.Fleeing) {
						movementMode = MovementMode.Moving;

						Flee();
					} else if (movementMode == MovementMode.Moving) {
						// Check if target is reached

						float dist = (position - targetPosition).magnitude;

						if (dist < movementRadius * 0.5f + 2) {
							// target is reached

							position = targetPosition;
							movementMode = MovementMode.Stationary;
							hasTarget = false;
						} else {
							bool reached = true;

							foreach (Transform member in members) {
								float targetDist = ((Vector2)member.position - targetPosition).magnitude;
								if (targetDist > movementRadius) {
									reached = false;
									break;
								}
							}

							if (reached) {
								position = targetPosition;
								movementMode = MovementMode.Stationary;
								hasTarget = false;
							}
						}
					}

					// Check for random wandering

					if (allowFreeMovement && mobile && (movementMode == MovementMode.Stationary || movementMode == MovementMode.Normal)) {
						// Could move around
						// Move at random given by the random Movement multiplier

						float threshold = 1f / (1f * updatesPerSecond * randomMoveEverySeconds);
						float value = UnityEngine.Random.value;

						if (value < threshold) {
							// When aggressive, move to opponent

							threshold = (float)bloodlust - 1;
							value = UnityEngine.Random.value;

							if (value < threshold) {
								// Find opponent

								try {
									FindRandomOpponent();
								} catch (Exception e) {
									Debug.LogError(e.Message);
								}
							} else {
								// Find suitable place

								FindRandomPlace();
							}
						}
					}
				}
			} catch (Exception e) {
				Debug.Log(e.Message);
			}

			StartCoroutine(UpdateSquad());
		}
	}

	/*
	 * Calculate the estimated combat strength
	 */
	public float CalcCombatPower() {
		if (isPlayer)
			return 3f * (1 + 1f * GetComponent<Entity>().health / GetComponent<Entity>().maxHealth);

		// Depending on the units health it adds 50% (zero health) to 100% (at full health) of its original power to the squad power

		combatPower = 0;

		foreach (Transform t in members) {
			Entity entity = t.GetComponent<Entity>();

			// Combat power shrinks linearly to half, when health drops

			combatPower += (float)(entity.combatStrength * ((1f * entity.GetHealth() / entity.maxHealth) * 0.5 + 0.5));
		}

		return combatPower;
	}

	public double GetBloodlust() {
		double value = bloodlust;

		// Regular increase

		value += bloodlustRaisePerMinute / (updatesPerSecond * 60);

		// Subtract damage recieved

		value -= damageReceived * 1d / maxHealth * 1.25;
		damageReceived = 0;

		// Add damage dealt

		value += damageCaused * .5d / maxHealth * 1.25;
		damageCaused = 0;

		// Clamp

		if (value < bloodlustMin)
			value = bloodlustMin;

		return value;
	}

	public void FindRandomPlace() {
		// 10 tries

		for (int i = 0; i < 10; i++) {
			// Create vector with random x and y

			Vector2 random = new Vector2(UnityEngine.Random.Range(-randomMovementDistance, +randomMovementDistance), UnityEngine.Random.Range(-randomMovementDistance, +randomMovementDistance));
			Vector2 newPos = position + random;

			// Check if in map

			if (!Map.IsWithinBounds(newPos)) {
				continue;
			}

			// Check if still withing movement distance

			float diff = random.magnitude;

			if (diff > randomMovementDistance)
				continue;

			// Check if accessible

			if (!aStarPath.GetNearest(newPos).node.Walkable) {
				continue;
			}

			// Check if inside other squad

			bool trespassing = false;
			foreach (Squad squad in LevelMaster.squads) {
				if (squad.team != team)
					continue;

				float dist = (squad.position - newPos).magnitude;
				if (dist < squad.movementRadius || dist < movementRadius) {
					trespassing = true;
					break;
				}
			}

			if (trespassing) {
				continue;
			}

			// Search successful, change target

			targetPosition = newPos;
			movementMode = MovementMode.Moving;
			hasTarget = true;

			return;
		}
	}

	/*
	 * Find random oppoonent squad on the map to move to when aggressive
	 */
	private void FindRandomOpponent() {
		// Get all opponent squads

		List<Squad> opponents = (LevelMaster.squads.Where(squad => squad.team != team)).ToList();

		if (opponents.Count == 0)
			return;

		// Assign a probability to each opponent, which equals the inverse distance

		Dictionary<Squad, float> opponentProbabilities = new Dictionary<Squad, float>();
		float probabilitySum = 0;

		// Loop through every opponent squad

		foreach (Squad opponentSquad in opponents) {
			float dist = (GetPosition() - opponentSquad.GetPosition()).magnitude;
			float prob = 100 / (dist * dist);
			opponentProbabilities.Add(opponentSquad, prob);
			probabilitySum += prob;
		}

		// Select according to probability

		float randomValue = UnityEngine.Random.value * probabilitySum;
		float x = 0;

		foreach (Squad opponentSquad in opponents) {
			x += opponentProbabilities[opponentSquad];
			if (x >= randomValue) {
				// Found opponent squad
				// Move towards it

				Vector2 opponentPos = opponentSquad.GetPosition();
				float distance = (opponentPos - GetPosition()).magnitude;
				Vector2 newPos = new Vector2();

				if (distance < randomMovementDistance) {
					// Move directly to opponent

					newPos = opponentPos;
				} else {
					// Move in the direction of the opponent

					// Find suitable place

					Vector2 dir = (opponentPos - GetPosition()).normalized;
					bool foundPos = false;

					for (float d = randomMovementDistance; d < distance; d += 5) {
						newPos = GetPosition() + dir * d;

						if (aStarPath.GetNearest(newPos).node.Walkable) {
							foundPos = true;
							break;
						}
					}

					if (!foundPos) {
						// Move directly to opponent
						newPos = opponentPos;
					}
				}

				targetPosition = newPos;

				movementMode = MovementMode.Moving;
				hasTarget = true;
			}
		}
	}

	private void Flee() {
		fleeingCounter++;
		if (fleeingCounter % 5 != 1) // Only seek new position every fifth update (to reduce constant rearrangement)
			return;
		if (fleeingFrom == null) {
			targetPosition = GetPosition();
			movementMode = MovementMode.Stationary;
			combatMode = CombatMode.None;
		}

		// Check if it can run away

		Vector2 position = GetPosition();
		Vector2 flightDirection = (position - fleeingFrom.GetPosition()).normalized;
		Vector2 newPosition = new Vector2();
		bool foundPos = false;

		// keep trying to find new location with increasing distance
		for (int i = 4; i < 100 && !foundPos; i++) {
			newPosition = position + flightDirection * 10f * i;

			// Check if colliding with wall
			int collision = Map.IsNearOfWall(newPosition, movementRadius * 3);

			if (collision > 0) {
				// colliding with at least 1 border
				// Seek position along the border (with 3 movement radii distance)

				bool horizontalCollision = collision == 2 || collision == 4 || collision > 4;
				bool verticalCollision = collision == 1 || collision == 3 || collision > 4;

				float contactPoint = 0; // The distance from the border to the map origin (0, 0)

				if (horizontalCollision)
					contactPoint = Map.staticMapWidth / 2f * Map.staticTileSize - 3 * movementRadius;
				if (collision == 4) // flip if on the left side
					contactPoint *= -1;

				if (verticalCollision)
					contactPoint = Map.staticMapHeight / 2f * Map.staticTileSize - 3 * movementRadius;
				if (collision == 3) // flip if on the bottom side
					contactPoint *= -1;

				Vector2 borderPosition; // The starting position along the border
				Vector2 borderDirection; // In which direction the border is followed

				if (horizontalCollision) {
					borderPosition = new Vector2(contactPoint, position.y);
					borderDirection = new Vector2(0, flightDirection.y > 0 ? 1 : -1);
				} else {
					borderPosition = new Vector2(position.x, contactPoint);
					borderDirection = new Vector2(flightDirection.x > 0 ? 1 : -1, 0);
				}

				float borderDist = (borderPosition - position).magnitude;

				for (; i < 100; i++) {
					// Keep same distance policy (10 * i) for each new try, but stay at the border
					// Pythagoras' theorem

					float actualDist = i * 10;
					float borderFollowingDist = Mathf.Sqrt((actualDist * actualDist) - (borderDist * borderDist));

					if (collision <= 4) {
						newPosition = borderPosition + (borderFollowingDist * borderDirection);
						if (float.IsNaN(newPosition.x)) {
							Debug.LogError("NaN x at " + transform.name);
							Debug.LogError("borderPosition " + borderPosition);
							Debug.LogError("collision " + collision);
							Debug.LogError("actual dist " + actualDist);
							Debug.LogError("border dist " + borderDist);
							Debug.LogError("border following distance " + borderFollowingDist);
							Debug.LogError("border direction " + borderDirection);
							Debug.LogError("nP " + newPosition);
						}
						// Check if colliding with wall
						collision = Map.IsNearOfWall(newPosition, movementRadius * 3);
					}

					if (collision > 4) {
						// Squad is near a corner
						// ToDo: Make a better flight algorithm for corners

						switch (collision) {
							case 5:
								newPosition = new Vector2(Map.GetLeftBorder() + 3 * movementRadius, Map.GetTopBorder() - 3 * movementRadius);
								break;
							case 6:
								newPosition = new Vector2(Map.GetRightBorder() - 3 * movementRadius, Map.GetTopBorder() - 3 * movementRadius);
								break;
							case 7:
								newPosition = new Vector2(Map.GetRightBorder() - 3 * movementRadius, Map.GetBottomBorder() + 3 * movementRadius);
								break;
							default:
								newPosition = new Vector2(Map.GetLeftBorder() + 3 * movementRadius, Map.GetBottomBorder() + 3 * movementRadius);
								break;
						}

						foundPos = true;
						break;
					}

					// Check if viable

					if (!aStarPath.
						GetNearest(newPosition).
						node.Walkable)
						continue;

					foundPos = true;
					break;
				}
			}

			// Check if viable

			if (!aStarPath.GetNearest(newPosition).node.Walkable)
				continue;

			foundPos = true;
			break;
		}

		if (foundPos) {
			// Set new target

			targetPosition = newPosition;
			hasTarget = true;

			if (fleeingFromCluster) {
				// Gizmos
				Color c = new Color(1, .8f, .6f); Debug.DrawLine(GetPosition(), (GetPosition() * 2 + targetPosition) / 3, c, 5f);
			}
		} else {
			Debug.LogError("no fleeing pos could be found");
		}
	}

	private void CheckCombat() {
		// Check whether combat status is still correct

		List<Squad> removeList = new List<Squad>();
		List<Squad> addList = new List<Squad>();

		// Check if all combats are still active

		for (var i = 0; i < squadsInCombatWith.Count; i++) {
			Squad opponent = squadsInCombatWith[i];
			if (opponent == null || opponent.transform == null) {
				removeList.Add(opponent);
				break;
			}

			float distance = (this.position - opponent.position).magnitude - this.movementRadius -
							 opponent.movementRadius;

			if (distance > 17) {
				// too great distance, abort fight

				removeList.Add(opponent);
			}
		}

		// Check if there are new combats

		foreach (Squad enemy in LevelMaster.squads.Where(squad => squad.team != this.team)) {
			if (squadsInCombatWith.Contains(enemy) || !enemy.gameObject.activeSelf)
				continue;

			float distance = (this.position - enemy.position).magnitude - this.movementRadius - enemy.movementRadius;

			if (distance < 12.5) {
				// near enough, start fight

				addList.Add(enemy);
			}
		}

		// Remove
		foreach (Squad squad in removeList) {
			squadsInCombatWith.Remove(squad);
			if (squadsInCombatWith.Count == 0) {
				// End combat

				EndCombat();
			}

			squad.squadsInCombatWith.Remove(this);
			if (squad.squadsInCombatWith.Count == 0) {
				squad.EndCombat();
			}
		}

		// Add

		foreach (Squad squad in addList) {
			squadsInCombatWith.Add(squad);
			if (!inCombat)
				BeginCombat(squad);

			squad.squadsInCombatWith.Add(this);
			if (!squad.inCombat)
				squad.BeginCombat(this);
		}

		if (inCombat)
			ReassessCombat();
	}

	private void CheckHelp() {
		try {
			if (combatMode == CombatMode.Fleeing && fleeingFromCluster) {
				// Currently fleeing from cluster, check if still active

				if (fleeingCounter >= 7) {
					// Abort flight

					// Update members
					members.ForEach(member => member.GetComponent<Moving>().SetMode(0));

					// Reset values

					fleeingCounter = 0;
					fleeingFrom = null;
					combatMode = CombatMode.None;

					// Set to stationary

					targetPosition = GetPosition();
					movementMode = MovementMode.Stationary;
				}

				float ratio = CalcRatio(fleeingFrom).Item1;

				// Check if odds have turned

				if (ratio * bloodlust > 1.1) {
					// come to help

					combatMode = CombatMode.ComingToHelp;
					fleeingFromCluster = false;
					attackedSquad = fleeingFrom;
					pursuingForSeconds = 0;

					// Update members
					members.ForEach(member => member.GetComponent<Moving>().SetMode(0));

					// Gizmos
					if (attackedSquad != null) {
						Color c = new Color(0, 0.6f, 0); Debug.DrawLine(GetPosition(), (GetPosition() * 2 + attackedSquad.GetPosition()) / 3, c, 5f);
					}
				}
			} else if (combatMode == CombatMode.ComingToHelp) {
				// Check if still active

				if (pursuingForSeconds > 5) {
					// Abort fight

					// Update members
					members.ForEach(member => member.GetComponent<Moving>().SetMode(0));

					// Reset values

					fleeingCounter = 0;
					combatMode = CombatMode.None;
					attackedSquad = null;

					// Set to stationary

					targetPosition = GetPosition();
					movementMode = MovementMode.Stationary;
				} else {
					pursuingForSeconds += 1f / updatesPerSecond;
				}

				// Check if still in mood

				float ratio = CalcRatio(attackedSquad).Item1;

				if (ratio * bloodlust < 0.8) {
					// break off by fleeing

					combatMode = CombatMode.Fleeing;
					fleeingFromCluster = true;
					fleeingFrom = attackedSquad;
					fleeingCounter = 0;
					attackedSquad = null;
					pursuingForSeconds = 0;

					targetPosition = new Vector2(0, 0);

					// Gizmos
					Color c = new Color(1, .8f, .6f); Debug.DrawLine(GetPosition(), (GetPosition() * 2 + targetPosition) / 3, c, 5f);
				}
			}
		} catch (Exception e) {
			Debug.LogError(e.Message);
		}
	}

	// When a fight breaks out (i.d. an opposing squad comes near) this method assesses whether to fight or flight
	public void BeginCombat(Squad opponent) {
		inCombat = true;

		float ratio = CalcRatio(opponent).Item1;

		if (!isPlayer) {

			// Reset values

			pursuingForSeconds = 0;
			fleeingCounter = 0;

			int response = AssessResponse(opponent, ratio);

			if (response == 0) {
				// attack

				combatMode = CombatMode.Attacking;
				attackedSquad = opponent;

				Vector2 offset = new Vector2(0, team);
				Color c = team == 0 ? Color.blue : Color.red; Debug.DrawLine(GetPosition() + offset, opponent.GetPosition() + offset, c, 2f);
			} else {
				// flee

				combatMode = CombatMode.Fleeing;
				fleeingFromCluster = false;
				fleeingFrom = opponent;

				Vector2 offset = new Vector2(0, team);

				Color c = team == 0 ? Color.blue : Color.red; Debug.DrawLine(GetPosition() + offset, GetPosition() * 2 - opponent.GetPosition() + offset, c, 2f);
			}
		}

		// Alarm cluster

		if (cluster != null && pursuingForSeconds < 15) {
			foreach (Transform t in cluster) {
				if (t == null || t.GetComponent<Squad>() == null || t == transform)
					continue;

				Squad s = t.GetComponent<Squad>();
				if (!s.isPlayer) {
					if (opponent.cluster == null)
						s.HelpAgainst(opponent, ratio);
					else
						s.HelpAgainst(opponent/*.cluster*/, ratio);
				}
			}
		}
	}

	// Assesses whether to fight or flight from an opponent. 0 = fight, 1 = flight
	private int AssessResponse(Squad opponent, float ratio) {
		if (ratio * bloodlust > 1.1) {
			// attack

			return 0;
		} else {
			// flee

			return 1;
		}
	}

	public void EndCombat() {
		if (combatMode == CombatMode.Fleeing) {
			// End flight

			Vector2 avgPos = new Vector2();
			members.ForEach(member => avgPos += (Vector2)member.position);
			avgPos /= members.Count;

			targetPosition = avgPos;
			position = avgPos;
			hasTarget = false;
			movementMode = MovementMode.Stationary;
		} else if (combatMode == CombatMode.Pursuing) {
			targetPosition = GetPosition();
			movementMode = MovementMode.Stationary;
		}

		inCombat = false;
		combatMode = CombatMode.None;

		fleeingFrom = null;
		attackedSquad = null;
	}

	// Function for other squads to call for help in combat. The squad this function is called upon, either helps in the fight or flees from the opponent
	public void HelpAgainst(Squad opponent, float ratio) {
		if (!mobile)
			return;

		if (combatMode == CombatMode.ComingToHelp) {
			if (pursuingForSeconds > 3 / updatesPerSecond) {
				// renew instructions

				Squad immobile = null;
				if (cluster != null) {
					foreach (Transform t in cluster) {
						if (t != null) {
							Squad s = t.GetComponent<Squad>();

							if (!s.mobile) {
								immobile = s;
								break;
							}
						}
					}
				}
				if (immobile == null) {
					targetPosition = opponent.position;
				} else {
					targetPosition = immobile.position;
				}

				pursuingForSeconds = 0;

				// Gizmos
				Color c = new Color(0, 0.6f, 0); Debug.DrawLine(GetPosition(), (GetPosition() * 2 + opponent.GetPosition()) / 3, c, 5f);

				return;
			}
		} else if (combatMode == CombatMode.Fleeing && fleeingFromCluster) {
			fleeingCounter %= 5;
		}

		if (combatMode == CombatMode.Attacking || combatMode == CombatMode.Fleeing || combatMode == CombatMode.ComingToHelp)
			return; // return when already engaged in combat

		//if (opponent.combatMode == CombatMode.Fleeing)
		//	return; // return when opponent is fleeing

		int response = AssessResponse(opponent, ratio);

		if (response == 0) {
			// Help in the fight

			combatMode = CombatMode.ComingToHelp;
			pursuingForSeconds = 0;

			// Update members
			members.ForEach(member => member.GetComponent<Moving>().SetMode(0));

			targetPosition = opponent.GetPosition();
			attackedSquad = opponent;
			movementMode = MovementMode.Moving;

			// Gizmos

			Color c = new Color(0, 0.6f, 0); Debug.DrawLine(GetPosition(), (GetPosition() * 2 + opponent.GetPosition()) / 3, c, 5f);
		} else {
			// Flee from the fight

			combatMode = CombatMode.Fleeing;
			fleeingFrom = opponent;
			fleeingFromCluster = true;
			fleeingCounter = 0;
			Flee();

			// Gizmos
			Color c = new Color(1, .8f, .6f); Debug.DrawLine(GetPosition(), (GetPosition() * 2 - opponent.GetPosition()), c, 5f);
		}
	}

	// Function for other squads to call for help in combat. The squad this function is called upon, either helps in the fight or flees from the opponent
	public void HelpAgainst(SquadCluster opponentCluster, float ratio) {
		try {
			Squad opponent = FindNewOpponent(opponentCluster.Select(item => item.GetComponent<Squad>()).ToArray());

			HelpAgainst(opponent, ratio);
		} catch (Exception e) {
			Debug.LogError(e);
		}
	}

	// Checks whether the current behavior (attack or flight) is still appropriate
	public void ReassessCombat() {
		Squad opponent;
		if (isPlayer) {
			opponent = squadsInCombatWith[0];
		} else {
			opponent = (combatMode == CombatMode.Attacking) ? attackedSquad : fleeingFrom;
		}

		float ratio;

		if (isPlayer) {
			ratio = CalcRatio(opponent).Item1;
		} else {
			if (opponent == null || !squadsInCombatWith.Contains(opponent)) {
				// squad was killed or got out of reach

				if (squadsInCombatWith.Count > 0) {
					opponent = FindNewOpponent(squadsInCombatWith.ToArray());
				} else {
					inCombat = false;
					combatMode = CombatMode.None;
					fleeingCounter = 0;
					fleeingFrom = null;
					attackedSquad = null;
					pursuingForSeconds = 0;

					targetPosition = GetPosition();
					movementMode = MovementMode.Stationary;

					return;
				}
			}

			if (opponent == null) {
				return;
			}
			// Assess response

			ratio = CalcRatio(opponent).Item1;

			if (combatMode == CombatMode.Pursuing) {
				// Check if still accurate

				foreach (Squad squad in squadsInCombatWith) {
					if (squad.combatMode == CombatMode.Attacking) {
						// Found new target

						opponent = squad;
						attackedSquad = squad;
						combatMode = CombatMode.Attacking;
						pursuingForSeconds = 0;
						break;
					}
				}

				if (/*still*/ combatMode == CombatMode.Pursuing) {
					// Check if still worth it

					pursuingForSeconds += 1f / updatesPerSecond;
					ratio -= pursuingForSeconds / 30f;

					if (ratio * bloodlust < 0.8 || pursuingForSeconds / 20f > bloodlust) {
						// break off pursuit

						EndCombat();

						// Update members
						members.ForEach(member => member.GetComponent<Moving>().SetMode(0));

						return;
					}
				}
			}

			if (combatMode == CombatMode.Attacking) {
				// Check if still worth it
				if (ratio * bloodlust < 0.8) {
					// break off by fleeing

					combatMode = CombatMode.Fleeing;
					fleeingFromCluster = false;
					fleeingFrom = opponent;
					attackedSquad = null;
					pursuingForSeconds = 0;

					targetPosition = new Vector2(0, 0);

					// Gizmos
					Vector2 offset = new Vector2(0, team - 0.5f);
					Color c = team == 0 ? Color.blue : Color.red; Debug.DrawLine(GetPosition() + offset, GetPosition() * 2 - opponent.GetPosition() + offset, c, 2f);
				}

				// Check if pursuing
				else if (combatMode != CombatMode.Pursuing && attackedSquad.combatMode == CombatMode.Fleeing) {
					combatMode = CombatMode.Pursuing;
					pursuingForSeconds = 0;
				}
			}

			if (combatMode == CombatMode.Fleeing) {
				// Check if odds have turned

				if (ratio * bloodlust > 1.1) {
					// start new attack

					if (fleeingFromCluster) {
						combatMode = CombatMode.ComingToHelp;
						fleeingFromCluster = false;
						attackedSquad = opponent;
						pursuingForSeconds = 0;
					} else {
						combatMode = CombatMode.Attacking;
						fleeingFrom = null;
						attackedSquad = opponent;
					}

					// Gizmos
					Vector2 offset = new Vector2(0, team - 0.5f);
					Color c = team == 0 ? Color.blue : Color.red; Debug.DrawLine(GetPosition() + offset, opponent.GetPosition() + offset, c, 2f);
				}
			}
		}

		// Alarm cluster

		if (inCombat && cluster != null) {
			foreach (Transform t in cluster) {
				if (t == null || t.GetComponent<Squad>() == null || t == transform)
					continue;

				Squad s = t.GetComponent<Squad>();
				if (!s.isPlayer) {
					if (opponent.cluster == null)
						s.HelpAgainst(opponent, ratio);
					else
						s.HelpAgainst(opponent.cluster, ratio);
				}
			}
		}
	}

	// Returns the ratio of own combat power to opponent combat power as well a bool, whether all allied squads are mobile
	private (float, bool) CalcRatio(Squad opponent) {
		if (opponent == null)
			return (4, true);

		bool alliesMobile = mobile;

		// Assess own combat power

		float ownPower = combatPower;

		if (cluster != null) {
			try {
				foreach (Transform squadTransform in cluster) {
					if (squadTransform == null) {
						cluster.RemoveSquad(squadTransform);
						continue;
					}

					Squad s = squadTransform.GetComponent<Squad>();

					if (s.combatMode == CombatMode.Fleeing)
						continue;

					if (!s.mobile)
						alliesMobile = false;

					if (s.isPlayer)
						ownPower += 9;

					ownPower += s.combatPower;
				}
			} catch (InvalidOperationException e) {
				Debug.Log("error: collection modified");
			}
		}

		// Assess opponent combat power

		float opponentPower = opponent.combatPower;

		if (opponent.cluster != null) {
			foreach (Transform squadTransform in opponent.cluster) {
				if (squadTransform == null || squadTransform.GetComponent<Squad>() == null) {
					opponent.cluster.RemoveSquad(squadTransform);
					break;
				}

				Squad s = squadTransform.GetComponent<Squad>();

				if (s.combatMode == CombatMode.Fleeing)
					continue;

				if (!s.mobile)
					alliesMobile = false;

				opponentPower += s.combatPower;
			}
		}

		// Assess response

		float ratio = ownPower / opponentPower;

		return (ratio, alliesMobile);
	}

	// When the current opponent is dead or fleeing, search for best available target
	private Squad FindNewOpponent(Squad[] possibleSquads) {
		Squad opponent = null;
		Squad fleeingOpponent = null;

		float lowestDist = float.PositiveInfinity;
		float lowestFleeingDist = float.PositiveInfinity;
		foreach (Squad squad in possibleSquads) {
			float dist = (squad.GetPosition() - this.GetPosition()).magnitude - squad.movementRadius;
			if (squad.combatMode != CombatMode.Fleeing && dist < lowestDist) {
				opponent = squad;
				lowestDist = dist;
			}

			if (squad.combatMode == CombatMode.Fleeing && dist < lowestFleeingDist) {
				fleeingOpponent = squad;
				lowestFleeingDist = dist;
			}
		}

		return opponent != null ? opponent : fleeingOpponent;
	}

	void OnDrawGizmosSelected() {
		if (showDebugInfo) {
			if (movementMode == MovementMode.Moving) {
				Map.DrawCircle(position, Color.cyan, movementRadius);
				Map.DrawCircle(targetPosition, Color.magenta, movementRadius);
			} else {
				Map.DrawCircle(position, Color.yellow, movementRadius);
			}
		}
	}

	public void RemoveMember(Transform member) {
		members.Remove(member);

		// Calculate movement radius

		movementRadius = movementRadiusMultiplier * Mathf.Sqrt(members.Count);

		if (members.Count < 1) {
			// Squad was terminated

			Destroy(this);
		}

		// Calculate max health
		maxHealth -= member.GetComponent<Entity>().maxHealth;
	}

	public Vector2 GetPosition() {
		return position;
	}
}
