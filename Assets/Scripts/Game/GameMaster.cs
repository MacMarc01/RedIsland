using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

/*
 * This script hosts the game and manages the top level processes
 */
public class GameMaster : MonoBehaviour
{
    // Settings

    public Object[] removeOnPlayerDeath;
    [HideInInspector] public static float minPassDist; // Entities must be at least this far apart to not be considered a cluster and thus can be passed between
    public float minimumPassingDistance; // The minimum gap between to entities to be regarded passable
    public float allyDistance; // How far friendly squads can be apart, to be considered allied
    public float SquadClusterUpdatesEverySeconds; // How often allied squads (same team and in close proximity) are updated
    public GameObject[] disableOnStart;
    public float timeScale = 1f;

    // Object references

    public GameObject friendContainer;
    public GameObject enemyContainer;
    public GameObject playerObject;
    public Transform explosion;
    public static Transform explosionPrefab;
    public GameObject overlay;
    public static Animator overlayAnim;
	public GameObject victoryScreen;

    // entity lists

    [HideInInspector]
    public static System.Collections.Generic.List<GameObject> entities;
    [HideInInspector]
    public static System.Collections.Generic.List<GameObject> friends;
    [HideInInspector]
    public static System.Collections.Generic.List<GameObject> enemies;
    //[HideInInspector]
    public static System.Collections.Generic.List<SquadCluster> friendSquadClusters; // Squads in close proximity (so they can help immediately)
    //[HideInInspector]
    public static System.Collections.Generic.List<SquadCluster> enemySquadClusters; // Squads in close proximity (so they can help immediately
	[HideInInspector]
    public static Transform player;
    [HideInInspector]
    public static System.Collections.Generic.List<EntityCluster> entityClusters;

	// private variables

	public static bool isPlayerDead;
    private static bool isPaused;

	public static GameMaster instance;

	// debug only

	[HideInInspector]
    public bool makeScreenshots;
	public int counter = 0;
	public int counter1 = 0;
	public int counter2 = 0;

	void Awake()
    {
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 120; // limit fps
    }

    void OnEnable()
    {
        GameMaster.player = playerObject.transform;
        instance = GetComponent<GameMaster>();
    }

    // Use this for initialization
    void Start()
    {
        // Inits

        minPassDist = minimumPassingDistance;

        isPlayerDead = false;

        explosionPrefab = explosion;

        overlayAnim = overlay.GetComponent<Animator>();

		ListEntities();

		// Add player as a consistent cluster

		System.Collections.Generic.List<Transform> playerList = new System.Collections.Generic.List<Transform>();
        playerList.Add(player);
        EntityCluster playerCluster = new EntityCluster(playerList);
        playerCluster.isPlayer = true;
        playerCluster.isMoving = true;
        entityClusters.Add(playerCluster);

        // Init clsuters

        InitEntityClusters();
        StartCoroutine(UpdateFriendlySquadClusters());
        StartCoroutine(StartCoroutineDelayed(UpdateEnemySquadClusters, SquadClusterUpdatesEverySeconds / 2f));
        //StartCoroutine(UpdateEnemySquadClusters());

		// start game

		PauseGame();
    }

    void Update()
    {
        isPlayerDead = false;
        foreach (GameObject g in disableOnStart)
        {
            g.SetActive(false);
        }
    }

    public void ListEntities() {
		// Inits

		entities = new System.Collections.Generic.List<GameObject>();
		friends = new System.Collections.Generic.List<GameObject>();
		enemies = new System.Collections.Generic.List<GameObject>();
		entityClusters = new System.Collections.Generic.List<EntityCluster>();

		// List all entities

		// List friends

		for (int i = 0; i < friendContainer.transform.childCount; i++) {
			// Loop every squad

			Transform squad = friendContainer.transform.GetChild(i);

			if (!squad.gameObject.activeSelf)
				continue;

			for (int u = 0; u < squad.childCount; u++) {
				// Loop every friend

				GameObject friend = squad.GetChild(u).gameObject;

				if (!friend.gameObject.activeSelf)
					continue;

				entities.Add(friend);

				friends.Add(friend);
			}
		}

		// List enemies

		for (int i = 0; i < enemyContainer.transform.childCount; i++) {
			// Loop every squad

			Transform squad = enemyContainer.transform.GetChild(i);

			if (!squad.gameObject.activeSelf)
				continue;

			for (int u = 0; u < squad.childCount; u++) {
				// Loop every friend

				Transform enemy = squad.GetChild(u);

				if (!enemy.gameObject.activeSelf)
					continue;

				entities.Add(enemy.gameObject);

				enemies.Add(enemy.gameObject);
			}
		}

		// Add player

		entities.Add(playerObject);

		friends.Add(playerObject);

		player = playerObject.transform;


	}

	public void InitEntityClusters()
    {
        foreach (GameObject obj in entities)
        {
            // Get all entityClusters which are near enough

            var nearClusters = new System.Collections.Generic.List<EntityCluster>();
            float minimumDistance = obj.GetComponent<Entity>().obstacleSize + minPassDist;

            foreach (EntityCluster cluster in entityClusters)
            {
                // Test if near enough
				
                if (cluster.GetClosestPassingDistance(obj.transform.position) < minimumDistance)
                {
                    nearClusters.Add(cluster);

                    // Add cluster connections

                    foreach (Transform companion in cluster)
                    {
                        if ((companion.position - obj.transform.position).magnitude < (minimumDistance + companion.GetComponent<Entity>().obstacleSize)) {
							if (obj.GetComponent<Moving>() != null && !obj.GetComponent<Moving>().clusteredEntities.Contains(companion))
								obj.GetComponent<Moving>().clusteredEntities.Add(companion);
							if (companion.GetComponent<Moving>() != null && !companion.GetComponent<Moving>().clusteredEntities.Contains(obj.transform))
								companion.GetComponent<Moving>().clusteredEntities.Add(obj.transform);
						}
                    }
                }
            }

            if (nearClusters.Count == 0)
            {
				// Create new cluster

				System.Collections.Generic.List<Transform> clusterList = new System.Collections.Generic.List<Transform> {
                    obj.transform
                };
                EntityCluster cluster = new EntityCluster(clusterList);
                entityClusters.Add(cluster);
            }
            else if (nearClusters.Count == 1)
            {
                // Add to cluster

                nearClusters[0].Add(obj.transform);
            }
            else
            {
                // Merge existing entityClusters and add object

                MergeEntityClusters(nearClusters);
                nearClusters[0].Add(obj.transform);
            }

            // Update Moving

			if (obj.GetComponent<Moving>() != null && obj.GetComponent<Moving>().hasStarted)
	            obj.GetComponent<Moving>().UpdateNearEntities();
        }
    }

	// Checks if there are any nearby entityClusters, which the GameObject can be pooled with
	public static bool ReconsiderEntityCluster(Transform t)
	{
		if (t == null)
			return false;

		// Remove from old cluster

		EntityCluster oldCluster = t.GetComponent<Entity>().cluster;

		if (oldCluster != null)
        {
	        if (oldCluster.Count == 1)
		        entityClusters.Remove(oldCluster);
	        oldCluster.RemoveEntity(t);
        }

        // Get all nearby entityClusters

        var nearClusters = new System.Collections.Generic.List<EntityCluster>();
        float minimumDistance = t.GetComponent<Entity>().obstacleSize + minPassDist;

        for (var i = 0; i < entityClusters.Count; i++)
        { // potential loop
	        EntityCluster cluster = entityClusters[i];

	        if (cluster == null)
	        {
		        entityClusters.Remove(cluster);
		        i--;
		        continue;
	        }
	        // Test if near enough

	        if (cluster.GetClosestPassingDistance(t.transform.position) < minimumDistance)
	        {
		        nearClusters.Add(cluster);

		        // Add cluster connections

		        foreach (Transform companion in cluster)
		        {
			        float distance = (companion.position - t.position).magnitude -
			                         companion.GetComponent<Entity>().obstacleSize;
			        if (distance < minimumDistance)
			        {
				        if (t.GetComponent<Moving>() != null &&
				            !t.GetComponent<Moving>().clusteredEntities.Contains(companion))
					        t.GetComponent<Moving>().clusteredEntities.Add(companion);
				        if (companion.GetComponent<Moving>() != null &&
				            !companion.GetComponent<Moving>().clusteredEntities.Contains(t))
					        companion.GetComponent<Moving>().clusteredEntities.Add(t);
			        }
		        }
	        }
        }

        if (nearClusters.Count == 0)
        {
            // Create new cluster

            System.Collections.Generic.List<Transform> clusterList = new System.Collections.Generic.List<Transform> { t };
            EntityCluster cluster = new EntityCluster(clusterList);
            entityClusters.Add(cluster);

			return true;
        }
        else if (nearClusters.Count == 1)
        {
            // Add to cluster

            nearClusters[0].Add(t);

            if (oldCluster != null)
	            return oldCluster == nearClusters[0];
            else return true;
        }
        else
        {
            // Merge existing entityClusters and add object

            MergeEntityClusters(nearClusters);
            nearClusters[0].Add(t);

            return true;
        }
    }

    private static void MergeEntityClusters(System.Collections.Generic.List<EntityCluster> list)
    {
        // Move all entities to first cluster

        for (int i = 1; i < list.Count; i++)
        {
            list[0].AddRange(list[i]);
            foreach (Transform t in list[i])
            {
                t.GetComponent<Entity>().cluster = list[0];
            }
            entityClusters.Remove(list[i]);
        }
	}

	IEnumerator UpdateFriendlySquadClusters() {
		if (!isPlayerDead && player.GetComponent<Squad>().cluster != null)
			player.GetComponent<Squad>().cluster = null;

		for (int x = 0; x < friendContainer.transform.childCount; x++) {

			GameObject squadObj1 = friendContainer.transform.GetChild(x).gameObject;

			if (squadObj1.gameObject.activeSelf) {
				Squad squad1 = squadObj1.GetComponent<Squad>();

				if (squad1 != null)
					squad1.cluster = null;
			}
		}
		// Scrap existing clusters

		if (friendSquadClusters != null) {
			foreach (SquadCluster cluster in friendSquadClusters)
			{
				for (var i = 0; i < cluster.Count; i++)
				{ // potential loop
					try {
						Transform squad = cluster[i];

						if (squad == null) {
							//cluster.RemoveSquad(squad);
							//i--;
							continue;
						}

						squad.GetComponent<Squad>().cluster = null;
					} catch(Exception e) {
						Debug.LogError(e);
					}
				}
			}
		}

		friendSquadClusters = new System.Collections.Generic.List<SquadCluster>();

		// Check all friendly squads (and player)

		for (int x = -1; x < friendContainer.transform.childCount; x++) {
			if (isPlayerDead && x == -1)
				continue;

			GameObject squadObj1;

			if (x == -1) // also add player as a friend
				squadObj1 = GameMaster.player.gameObject;
			else
				squadObj1 = friendContainer.transform.GetChild(x).gameObject;

			if (squadObj1 == null || !squadObj1.gameObject.activeSelf)
				continue;

			Squad squad1 = squadObj1.GetComponent<Squad>();

			if (squad1 == null || squad1.cluster != null)
				continue;

			// Get all squadClusters which are near enough

			var nearSquads = new System.Collections.Generic.List<Squad>();
			var nearClusters = new System.Collections.Generic.List<SquadCluster>();
			float minimumDistance = squadObj1.GetComponent<Squad>().movementRadius + allyDistance;

			// Check 
			for (int y = 0; y < friendContainer.transform.childCount; y++) {
				GameObject squadObj2 = friendContainer.transform.GetChild(y).gameObject;

				if (squadObj1 == squadObj2 || !squadObj2.gameObject.activeSelf)
					continue;

				Squad squad2 = squadObj2.GetComponent<Squad>();

				// Test if near enough

				float distance = (squad2.GetPosition() - squad1.GetPosition()).magnitude - squad2.movementRadius;

				if (distance < minimumDistance) {
					nearSquads.Add(squad2);

					if (squad2.cluster != null)
						nearClusters.Add(squad2.cluster);
				}
			}

			if (nearSquads.Count == 0)
				continue;


			if (nearClusters.Count == 0) {
				// Create new cluster

				SquadCluster cluster = new SquadCluster(nearSquads, 0);
				cluster.Add(squad1);

				friendSquadClusters.Add(cluster);
			} else if (nearClusters.Count == 1) {
				// Add to cluster

				nearClusters[0].AddRange(nearSquads);
				nearClusters[0].Add(squad1);
			} else {
				// Merge existing entityClusters and add object

				MergeSquadClusters(nearClusters);
				nearClusters[0].AddRange(nearSquads);
				nearClusters[0].Add(squad1);
			}
		}

		// Debug
		foreach (SquadCluster cluster in friendSquadClusters) {
			foreach (Transform squad1 in cluster) {
				foreach (Transform squad2 in cluster) {
					Debug.DrawLine(squad1.GetComponent<Squad>().GetPosition(), squad2.GetComponent<Squad>().position, Color.gray, SquadClusterUpdatesEverySeconds);
				}
			}
		}counter1 = friendSquadClusters.Count;

		// recurse

		yield return new WaitForSeconds(SquadClusterUpdatesEverySeconds);
		StartCoroutine(UpdateFriendlySquadClusters());
	}

	IEnumerator UpdateEnemySquadClusters() {
		for (int x = 0; x < enemyContainer.transform.childCount; x++) {

			GameObject squadObj1 = enemyContainer.transform.GetChild(x).gameObject;

			if (squadObj1.gameObject.activeSelf) {
				Squad squad1 = squadObj1.GetComponent<Squad>();

				if (squad1 != null)
					squad1.cluster = null;
			}
		}
		// Scrap existing clusters

		counter++;
		if (makeScreenshots)
		{
			ScreenCapture.CaptureScreenshot("screenshot #" + counter + ".png", 2);
		}
		enemySquadClusters = new System.Collections.Generic.List<SquadCluster>();

		// Check all enemy squads

		for (int x = 0; x < enemyContainer.transform.childCount; x++) {
			GameObject squadObj1 = enemyContainer.transform.GetChild(x).gameObject;

			if (!squadObj1.gameObject.activeSelf)
				continue;

			Squad squad1 = squadObj1.GetComponent<Squad>();

			if (squad1.cluster != null)
				continue;

			// Get all squadClusters which are near enough

			var nearSquads = new System.Collections.Generic.List<Squad>();
			var nearClusters = new System.Collections.Generic.List<SquadCluster>();
			float minimumDistance = squadObj1.GetComponent<Squad>().movementRadius + allyDistance;

			// Check 
			for (int y = 0; y < enemyContainer.transform.childCount; y++) {
				GameObject squadObj2 = enemyContainer.transform.GetChild(y).gameObject;

				if (squadObj1 == squadObj2 || !squadObj2.gameObject.activeSelf)
					continue;

				Squad squad2 = squadObj2.GetComponent<Squad>();

				// Test if near enough

				float distance = (squad2.GetPosition() - squad1.GetPosition()).magnitude - squad2.movementRadius;

				if (distance < minimumDistance) {
					nearSquads.Add(squad2);

					if (squad2.cluster != null)
						nearClusters.Add(squad2.cluster);
				}
			}

			if (nearSquads.Count == 0)
				continue;


			if (nearClusters.Count == 0) {
				// Create new cluster

				SquadCluster cluster = new SquadCluster(nearSquads, 1);
				cluster.Add(squad1);

				enemySquadClusters.Add(cluster);
			} else if (nearClusters.Count == 1) {
				// Add to cluster

				nearClusters[0].AddRange(nearSquads);
				nearClusters[0].Add(squad1);
			} else {
				// Merge existing entityClusters and add object

				MergeSquadClusters(nearClusters);
				nearClusters[0].AddRange(nearSquads);
				nearClusters[0].Add(squad1);
			}
		}

		// Debug
		foreach (SquadCluster cluster in enemySquadClusters) {
			foreach (Transform squad1 in cluster) {
				foreach (Transform squad2 in cluster) {
					Debug.DrawLine(squad1.GetComponent<Squad>().GetPosition(), squad2.GetComponent<Squad>().position, Color.gray, SquadClusterUpdatesEverySeconds);
				}
			}
		}counter2 = enemySquadClusters.Count;

		// recurse

		yield return new WaitForSeconds(SquadClusterUpdatesEverySeconds);
		StartCoroutine(UpdateEnemySquadClusters());
	}

	private static void MergeSquadClusters(System.Collections.Generic.List<SquadCluster> list) {
	    // Move all entities to first cluster

	    for (int i = 1; i < list.Count; i++) {
		    list[0].AddRange(list[i]);
		    foreach (Transform t in list[i]) {
			    t.GetComponent<Squad>().cluster = list[0];
		    }

		    if (list[0].team == 0)
			    friendSquadClusters.Remove(list[i]);
			else
			    enemySquadClusters.Remove(list[i]);
	    }
    }

    void OnDrawGizmos()
    {
	    if (!Application.isPlaying)
		    return;
	}

	public static bool CanSee(Watching bigBrother, Entity target)
    {
        return true;
    }

    public static void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

		// disable player zooming

		player.GetComponent<PlayerZooming>().allowZooming = false;
    }

    public static void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = GameMaster.instance.timeScale;

        // enable player zooming

        player.GetComponent<PlayerZooming>().allowZooming = true;
    }

    public static bool IsPaused()
    {
        return isPaused;
    }

    public static void PlayerDeath(Entity.DeathType deathCause)
    {
		isPlayerDead = true;

        instance.StartCoroutine(instance.PlayerDeathSequence(deathCause));
    }

    private IEnumerator PlayerDeathSequence(Entity.DeathType deathCause)
    {
		if (player != null) {

			// Do these things instantly when player dies

			if (deathCause != Entity.DeathType.DROWNING)
				overlayAnim.SetTrigger("Player_Death");

			Time.timeScale = 0.2f;

			// Disable player scripts

			GameMaster.player.GetComponent<PlayerZooming>().allowZooming = false;

			yield return new WaitForSeconds(0.2f);

			// Do these things when map is not visible (game over animation)

			foreach (Object obj in removeOnPlayerDeath)
				Destroy(obj);

			if (player.GetComponent<PlayerZooming>() != null)
				player.GetComponent<PlayerZooming>().ZoomToValue(26);

			//player = null;

			// Reset camera clamp

			Camera.main.GetComponent<CameraClamp>().ChangeViewport(0, 0, 0, 0);

			Time.timeScale = 1.0f;

			yield return new WaitForSeconds(0.8f);

			// Do these things when map is visible again

		}
    }

	public void Win() {
		StartCoroutine(WinSequence());
	}

	private IEnumerator WinSequence() {
		yield return new WaitForSeconds(0.25f);

		victoryScreen.SetActive(true);
		IngameMenu.instance.OnWin();

		player.GetComponent<PlayerAiming>().enabled = false;
		player.GetComponent<PlayerZooming>().ZoomToValue(26);
		player.GetComponent<PlayerZooming>().allowZooming = false;
	}

    public static void DrawSquare(Vector2 pos)
    {
        // Draw a yellow Debug-Line square with fixed length

        DrawSquare(pos, Color.black, 0.5f);
    }

    public static void DrawSquare(Vector2 pos, Color color)
    {
        // Draw a  Debug-Line square with fixed length

        DrawSquare(pos, color, 0.5f);
    }

    public static void DrawSquare(Vector2 pos, Color color, float duration)
    {
        // Draw a Debug-Line square

        // Calculate corners

        duration /= 2;

        Vector2 topLeft, topRight, lowerLeft, lowerRight;

        topLeft = new Vector2(pos.x - duration, pos.y + duration);
        topRight = new Vector2(pos.x + duration, pos.y + duration);
        lowerLeft = new Vector2(pos.x - duration, pos.y - duration);
        lowerRight = new Vector2(pos.x + duration, pos.y - duration);

        // Draw Lines

        Debug.DrawLine(topLeft, topRight, color, 0.5f);
        Debug.DrawLine(lowerLeft, lowerRight, color, 0.5f);
        Debug.DrawLine(topLeft, lowerLeft, color, 0.5f);
        Debug.DrawLine(topRight, lowerRight, color, 0.5f);
    }

    private IEnumerator StartCoroutineDelayed(Func<IEnumerator> f, float delay)
    {
		yield return new WaitForSeconds(delay);

		StartCoroutine(f());
    }
}
