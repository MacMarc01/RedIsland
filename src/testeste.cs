using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * This script hosts the game and manages the top level processes
 */
public class GameMaster : MonoBehaviour
{
    // Settings

    public Object[] removeOnPlayerDeath;
    [HideInInspector] public static float minPassDist; // Entities must be at least this far apart to not be considered a cluster and thus can be passed between
    public float minimumPassingDistance;
    public float ClusterUpdatesPerSecond;
    public GameObject[] disableOnStart;

    // Object references

    public GameObject friendContainer;
    public GameObject enemyContainer;
    public GameObject playerObject;
    public Transform explosion;
    public static Transform explosionPrefab;
    public GameObject overlay;
    public static Animator overlayAnim;

    // entity lists

    [HideInInspector]
    public static List<GameObject> entities;
    [HideInInspector]
    public static List<GameObject> friends;
    [HideInInspector]
    public static List<GameObject> enemies;
    [HideInInspector]
    public static Transform player;
    [HideInInspector]
    public static List<EntityCluster> clusters;

    // private variables

    private static bool isPlayerDead;
    private static bool isPaused;

    [HideInInspector]
    public static GameMaster instance;
    void Awake()
    {
#if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 60; // limit fps
#endif
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

        entities = new List<GameObject>();
        friends = new List<GameObject>();
        enemies = new List<GameObject>();
        clusters = new List<EntityCluster>();

        overlayAnim = overlay.GetComponent<Animator>();

        // List all entities

        // List friends

        for (int i = 0; i < friendContainer.transform.childCount; i++)
        {
            // Loop every squad

            Transform squad = friendContainer.transform.GetChild(i);

            if (!squad.gameObject.activeSelf)
                continue;

            for (int u = 0; u < squad.childCount; u++)
            {
                // Loop every friend

                GameObject friend = squad.GetChild(u).gameObject;

                if (!friend.gameObject.activeSelf)
                    continue;

                entities.Add(friend);

                friends.Add(friend);
            }
        }

        // List enemies

        for (int i = 0; i < enemyContainer.transform.childCount; i++)
        {
            // Loop every squad

            Transform squad = enemyContainer.transform.GetChild(i);

            if (!squad.gameObject.activeSelf)
                continue;

            for (int u = 0; u < squad.childCount; u++)
            {
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

        // Add player as a consistent cluster

        List<Transform> playerList = new List<Transform>();
        playerList.Add(player);
        EntityCluster playerCluster = new EntityCluster(playerList);
        playerCluster.isPlayer = true;
        playerCluster.isMoving = true;
        clusters.Add(playerCluster);

        // Init clusters

        InitClusters();

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

    public void InitClusters()
    {
        foreach (GameObject obj in entities)
        {
            if (obj.GetComponent<Moving>() == null)
                continue;

            // Get all clusters which are near enough

            var nearClusters = new List<EntityCluster>();
            float minimumDistance = obj.GetComponent<Entity>().obstacleSize + minPassDist;

            foreach (EntityCluster cluster in clusters)
            {
                // Test if near enough

                if (cluster.GetClosestPassingDistance(obj.transform.position) < minimumDistance)
                {
                    nearClusters.Add(cluster);

                    // Add cluster connections
                    Debug.Log("test");
                    foreach (Transform companion in cluster)
                    {
                        if ((companion.position - obj.transform.position).magnitude < (minimumDistance + companion.GetComponent<Entity>().obstacleSize))
                        {
                            if (obj.GetComponent<Moving>() != null && !obj.GetComponent<Moving>().clusteredEntities.Contains(companion))
                                obj.GetComponent<Moving>().clusteredEntities.Add(companion);
                        }
                    }
                }
            }

            if (nearClusters.Count == 0)
            {
                // Create new cluster

                List<Transform> clusterList = new List<Transform> {
                    obj.transform
                };
                EntityCluster cluster = new EntityCluster(clusterList);
                clusters.Add(cluster);
            }
            else if (nearClusters.Count == 1)
            {
                // Add to cluster

                nearClusters[0].Add(obj.transform);
            }
            else
            {
                // Merge existing clusters and add object

                MergeClusters(nearClusters);
                nearClusters[0].Add(obj.transform);
            }

            // Update Moving

            obj.GetComponent<Moving>().UpdateNearEntities();
        }
    }

    // Checks if there are any nearby clusters, which the GameObject can be pooled with
    public static bool ReconsiderCluster(Transform t)
    {
        // Remove from old cluster

        EntityCluster oldCluster = t.GetComponent<Entity>().cluster;
        if (oldCluster.Count == 1)
            clusters.Remove(oldCluster);
        oldCluster.RemoveEntity(t);

        // Get all nearby clusters

        var nearClusters = new List<EntityCluster>();
        float minimumDistance = t.GetComponent<Entity>().obstacleSize + minPassDist;

        foreach (EntityCluster cluster in clusters)
        {
            // Test if near enough

            if (cluster.GetClosestPassingDistance(t.transform.position) < minimumDistance)
            {
                nearClusters.Add(cluster);

                // Add cluster connections

                foreach (Transform companion in cluster)
                {
                    if (t.GetComponent<Moving>() != null && !t.GetComponent<Moving>().clusteredEntities.Contains(companion))
                        t.GetComponent<Moving>().clusteredEntities.Add(companion);
                    if (companion.GetComponent<Moving>() != null && !companion.GetComponent<Moving>().clusteredEntities.Contains(t))
                        companion.GetComponent<Moving>().clusteredEntities.Add(t);
                }
            }
        }

        if (nearClusters.Count == 0)
        {
            // Create new cluster

            List<Transform> clusterList = new List<Transform> { t };
            EntityCluster cluster = new EntityCluster(clusterList);
            clusters.Add(cluster);

            return true;
        }
        else if (nearClusters.Count == 1)
        {
            // Add to cluster

            nearClusters[0].Add(t);

            return oldCluster == nearClusters[0];
        }
        else
        {
            // Merge existing clusters and add object

            MergeClusters(nearClusters);
            nearClusters[0].Add(t);

            return true;
        }
    }

    private static void MergeClusters(List<EntityCluster> list)
    {
        // Move all entities to first cluster

        for (int i = 1; i < list.Count; i++)
        {
            list[0].AddRange(list[i]);
            foreach (Transform t in list[i])
            {
                t.GetComponent<Entity>().cluster = list[0];
            }
            clusters.Remove(list[i]);
        }
    }

    public static bool CanSee(Watching bigBrother, Entity target)
    {
        return true;
    }

    public static void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0.0f;

        // disable player zooming

        player.GetComponent<PlayerZooming>().allowZooming = false;
    }

    public static void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1.0f;

        // enable player zooming

        player.GetComponent<PlayerZooming>().allowZooming = true;
    }

    public static bool IsPaused()
    {
        return isPaused;
    }

    public static void PlayerDeath(Entity.DeathType deathCause)
    {
        instance.StartCoroutine(instance.PlayerDeathSequence(deathCause));
    }

    private IEnumerator PlayerDeathSequence(Entity.DeathType deathCause)
    {
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

        player.GetComponent<PlayerZooming>().ZoomToValue(26);

        player = null;

        // Reset camera clamp

        Camera.main.GetComponent<CameraClamp>().ChangeViewport(0, 0, 0, 0);

        Time.timeScale = 1.0f;

        yield return new WaitForSeconds(0.8f);

        // Do these things when map is visible again

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
}
