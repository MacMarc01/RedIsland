using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 
public class DebugLevel : LevelMaster
{
	// References

	public GameObject friendContainer;
	public GameObject enemyContainer;

	// Prefabs

	public GameObject FriendlySquadPrefab;
	public GameObject EnemySquadPrefab;

	public GameObject FriendlyTankPrefab;
	public GameObject EnemyTankPrefab;

	// private references

	private Map map;
	public AstarPath aStar;

	private GameObject gameMasterObject;
	private GameMaster gm;

	public void Spawn()
	{
		gameMasterObject = GameObject.Find("GameMaster");
		gm = gameMasterObject.GetComponent<GameMaster>();

		if (GameMaster.entities == null)
			gm.ListEntities();

		aStar.Scan();

		RemoveTanks();
		CheckSquadPositions();
		SpawnTanks(0);
		SpawnTanks(1);
	}

	public void RemoveTanks()
	{
		if (Application.isPlaying)
		{
			foreach (GameObject go in GameMaster.entities) {
				Entity entity = go.GetComponent<Entity>();

				if (!entity.hasSquad)
					continue;

				if (entity == GameMaster.player)
					continue;

				Squad squad = go.transform.parent.GetComponent<Squad>();

				if (!squad.spawned)
					continue;

				// Remove entity

				if (entity.team == 0)
					GameMaster.friends.Remove(go);
				else
					GameMaster.enemies.Remove(go);

				GameMaster.entities.Remove(go);

				squad.
					members.
					Remove(transform);

				if (entity.cluster != null)
					entity.cluster.RemoveEntity(transform);

				// Remove itself

				Destroy(go);

				if (squad.members.Count == 0) {
					Destroy(squad);
				}
			}
		}
		else
		{
			// Remove all spawned squads

			// All friendly squads

			for (int i = 0; i < friendContainer.transform.childCount; i++)
			{
				Transform squad = friendContainer.transform.GetChild(i);
				if (squad.GetComponent<Squad>().spawned)
				{
					DestroyImmediate(squad.gameObject);
					i--;
				}
			}

			// All enemy squads

			for (int i = 0; i < enemyContainer.transform.childCount; i++) {
				Transform squad = enemyContainer.transform.GetChild(i);
				if (squad.GetComponent<Squad>().spawned)
				{
					DestroyImmediate(squad.gameObject);
					i--;
				}
			}
		}
	}

	void CheckSquadPositions()
	{
		// Check both teams

		for (int team = 0; team < 2; team++)
		{
			GameObject container = team == 0 ? friendContainer : enemyContainer;
			for (int i = 0; i < container.transform.childCount; i++)
			{
				Transform t = container.transform.GetChild(i);
				Squad s = t.GetComponent<Squad>();

				// Check if null
				if (true || float.IsInfinity(s.GetPosition().x) || float.IsNaN(s.GetPosition().x) ||
				    (s.GetPosition() - new Vector2(0, 0)).magnitude < 0.01)
				{
					Vector2 position = new Vector2(0, 0);

					for (int m = 0; m < t.childCount; m++)
					{
						position += (Vector2) t.GetChild(m).position;
					}

					position /= t.childCount;

					s.position = position;
				}
			}
		}
	}

	public void SpawnTanks(int team)
	{
		map = GameObject.Find("Map").GetComponent<Map>();
		//aStar = GameObject.Find("A*").GetComponent<Pathfinder>();

		// Spawn tanks of the given team
		// There are 3 configurations of squad sizes, which all have an equal probability to occur:
		// - 1x 5   0x 4   2x 3   2x 2   0x 1
		// - 0x 5   1x 4   2x 3   2x 2   1x 1
		// - 0x 5   0x 4   3x 3   3x 2   0x 1

		int[] squadSizes;
		if (team == 0)
			squadSizes = new[] {3, 3, 2, 2, 2};
		else
			squadSizes = new[] {3, 3, 3, 2, 2, 2};

		// Spawn and populate squads

		for (int n = 0; n < squadSizes.Length; n++)
		{
			SpawnSquad(team, squadSizes[n]);
		}
	}

	private void SpawnSquad(int team, int size)
	{
		if (size == 0)
			return;

		// Find squad location - 100 tries

		Vector2 squadLocation = new Vector2(0, 0);
		float maxX = map.mapWidth * map.tileSize * 0.5f - 15;
		float maxY = map.mapHeight * map.tileSize * 0.5f - 15;

		bool viable = true;
		for (int i = 0; i < 100; i++)
		{
			// Create random position inside map

			float x = Random.Range(-maxX, maxX);
			float y = Random.Range(-maxY, maxY);
			squadLocation = new Vector2(x, y);

			// Check if viable

			viable = true;
			if (!aStar.GetNearest(squadLocation).node.Walkable)
			{
				viable = false;
				continue;
			}

			// Check if not too near of opponent squad

			GameObject opponentContainer = team == 0 ? gm.enemyContainer : gm.friendContainer;

			for (int g = 0; g < opponentContainer.transform.childCount; g++)
			{
				GameObject squadObject = opponentContainer.transform.GetChild(g).gameObject;

				Squad squad = squadObject.GetComponent<Squad>();

				float distance = (squad.GetPosition() - squadLocation).magnitude - squad.movementRadius;
				
				if (distance < 15) {
					viable = false;
				}
			}

			if (!viable)
				continue;

			// Check if not too near of friendly squad

			GameObject friendContainer = team == 1 ? gm.enemyContainer : gm.friendContainer;

			for (int g = 0; g < friendContainer.transform.childCount; g++) {
				GameObject squadObject = friendContainer.transform.GetChild(g).gameObject;

				Squad squad = squadObject.GetComponent<Squad>();

				float distance = (squad.GetPosition() - squadLocation).magnitude - squad.movementRadius;

				if (distance < 5) {
					viable = false;
				}
			}

			if (viable)
				break;
		}

		if (!viable)
		{
			Debug.LogError("no possible squad location");
			return;
		}

		// Spawn squad object

		GameObject container = team == 0 ? friendContainer : enemyContainer;
		GameObject prefab = team == 0 ? FriendlySquadPrefab : EnemySquadPrefab;

		GameObject newSquad = Instantiate(prefab, new Vector3(), new Quaternion(0, 0, 0, 0), container.transform);
		byte[] randomNumbers = new[]
			{(byte) Random.Range(48, 126), (byte) Random.Range(48, 126), (byte) Random.Range(48, 126)};
		newSquad.name = size + " t Squad " + ByteArrayToString(randomNumbers);
		Debug.Log("squad location " + squadLocation);
		newSquad.GetComponent<Squad>().aStar = aStar.transform;
		newSquad.GetComponent<Squad>().position = squadLocation;

		// Get movementRadius

		float movementRadius = newSquad.GetComponent<Squad>().movementRadiusMultiplier * Mathf.Sqrt(size);

		// Spawn tanks

		for (int t = 0; t < size; t++)
		{
			// 100 tries

			bool viableT = true;
			Vector2 tankLocation = new Vector2();
			GameObject tankPrefab = team == 0 ? FriendlyTankPrefab : EnemyTankPrefab;

			for (int i = 0; i < 100; i++)
			{
				viableT = true;

				// Get relative position

				float relX = Random.Range(-movementRadius, movementRadius);
				float relY = Random.Range(-movementRadius, movementRadius);

				// Check if inside circle

				float magnitude = Mathf.Sqrt(relX * relX + relY * relY);
				if (magnitude > movementRadius)
				{
					viableT = false;
					continue;
				}

				tankLocation = squadLocation + new Vector2(relX, relY);

				// Check for walkable terrain

				if (!aStar.GetNearest(tankLocation).node.Walkable) {
					Map.DrawSquare(tankLocation, Color.red, 50f);
					viableT = false;
					continue;
				}

				// Check if not too close to another entity

				List<GameObject> nulls = new List<GameObject>();

				foreach (GameObject entity in GameMaster.entities) {
					if (entity == null) {
						nulls.Add(entity);

						continue;
					}

					float dist = ((Vector2)entity.transform.position - tankLocation).magnitude;

					if (dist > 10)
						continue;

					if (dist < tankPrefab.GetComponent<Entity>().obstacleSize +
						entity.GetComponent<Entity>().obstacleSize) {
						Map.DrawSquare(tankLocation, Color.yellow, 50f);
						viableT = false;
						break;
					}
				}

				foreach (GameObject entity in nulls)
				{
					try {
						GameMaster.friends.Remove(entity);
					} catch (Exception e) {
						GameMaster.enemies.Remove(entity);
					}
					GameMaster.entities.Remove(entity);
				}

				if (viableT)
					break;
			}

			if (!viableT) {
				Debug.LogError("no possible tank location for squad " + newSquad.transform.name);
				continue;
			}

			// Spawn tank

			GameObject newTank = Instantiate(tankPrefab, tankLocation, new Quaternion(0, 0, 0, 0), newSquad.transform);

			// Set references

			newTank.GetComponent<Moving>().aStar = aStar.transform;
			newTank.GetComponent<Vehicle>().mapObject = map.transform;
			newTank.GetComponent<Entity>().squad = newSquad.GetComponent<Squad>();
			newTank.GetComponent<Entity>().hasSquad = true;

			// Add to GameMaster

			GameMaster.entities.Add(newTank);
			if (team == 0)
				GameMaster.friends.Add(newTank);
			else
				GameMaster.enemies.Add(newTank);
		}

		Squad squadComp = newSquad.GetComponent<Squad>();

		squadComp.targetPosition = squadLocation;
		squadComp.movementMode = Squad.MovementMode.Stationary;
		squadComp.spawned = true;
		squadComp.Start();
	}

	private string ByteArrayToString(byte[] arr) {
		System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
		return enc.GetString(arr);
	}
}
