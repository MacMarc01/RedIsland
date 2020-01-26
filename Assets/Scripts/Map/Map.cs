using UnityEngine;
using System.Collections;
using UnityEditor;

[ExecuteInEditMode]
public class Map : MonoBehaviour {
	public float tileSize = 3; // The length and width of every tile in absolute game units

	public int mapHeight; // How high the map is (The camera let the player not see the highest and lowest half of the tiles)
	public int mapWidth; // How wide the map is (The camera let the player not see the most left and most right half of the tiles)

	public Camera playerCam; // Used to limit the player's FOV
	public Transform player; // Reference to the player

	private PlayerZooming zoom;
	private Follow camFollow;

	public static float staticTileSize;
	public static int staticMapHeight;
	public static int staticMapWidth;

	void OnEnable()
	{
		zoom = player.gameObject.GetComponent<PlayerZooming>();
		camFollow = playerCam.transform.parent.GetComponent<Follow>();

		staticMapHeight = mapHeight;
		staticMapWidth = mapWidth;
		staticTileSize = tileSize;
	}

	void Update() {
		if (GameMaster.IsPaused()) return;

		if (playerCam != null)
		{
			// Limit player's FOV, so nothing outside the map is visible

			Vector2 currentPos = playerCam.transform.position;

			Vector2 camDimensions = new Vector2(playerCam.orthographicSize * playerCam.aspect, playerCam.orthographicSize);

			camFollow.minX = mapWidth  * tileSize * -0.5f + camDimensions.x;
			camFollow.minY = mapHeight * tileSize * -0.5f + camDimensions.y;
			camFollow.maxX = mapWidth  * tileSize * +0.5f - camDimensions.x;
			camFollow.maxY = mapHeight * tileSize * +0.5f - camDimensions.y;
		}

		// Draw boundaries

		Debug.DrawLine(new Vector2((-mapWidth / 2f), (-mapHeight / 2f)) * tileSize, new Vector2((+mapWidth / 2f), (-mapHeight / 2f)) * tileSize, Color.blue);
		Debug.DrawLine(new Vector2((-mapWidth / 2f), (+mapHeight / 2f)) * tileSize, new Vector2((+mapWidth / 2f), (+mapHeight / 2f)) * tileSize, Color.blue);

		Debug.DrawLine(new Vector2((-mapWidth / 2f), (-mapHeight / 2f)) * tileSize, new Vector2((-mapWidth / 2f), (+mapHeight / 2f)) * tileSize, Color.blue);
		Debug.DrawLine(new Vector2((+mapWidth / 2f), (-mapHeight / 2f)) * tileSize, new Vector2((+mapWidth / 2f), (+mapHeight / 2f)) * tileSize, Color.blue);
	}

	public static void DrawSquare(Vector2 pos)
	{
		// Draw a yellow Debug-Line square with fixed length

		DrawSquare(pos, Color.black, 0.5f);
	}

	public static void DrawSquare(Vector2 pos, Color color)
	{
		DrawSquare(pos, color, 0.5f);
	}

	public static void DrawSquare(Vector2 pos, Color color, float duration)
	{
		// Draw a Debug-Line square

		// Calculate corners

		Vector2 topLeft, topRight, lowerLeft, lowerRight;

		topLeft = new Vector2(pos.x - 0.5f, pos.y + 0.5f);
		topRight = new Vector2(pos.x + 0.5f, pos.y + 0.5f);
		lowerLeft = new Vector2(pos.x - 0.5f, pos.y - 0.5f);
		lowerRight = new Vector2(pos.x + 0.5f, pos.y - 0.5f);

		// Draw Lines

		Debug.DrawLine(topLeft, topRight, color, duration);
		Debug.DrawLine(lowerLeft, lowerRight, color, duration);
		Debug.DrawLine(topLeft, lowerLeft, color, duration);
		Debug.DrawLine(topRight, lowerRight, color, duration);
	}

	public static void DrawCircle(Vector3 pos, Color color, float radius)
	{
#if UNITY_EDITOR
		Handles.color = color;
		Handles.DrawWireDisc(pos, new Vector3(0, 0, 1), radius);
#endif
	}

	public void CreateColliders()
	{
		// create

		BoxCollider2D top = gameObject.AddComponent(typeof(BoxCollider2D)) as BoxCollider2D;
		BoxCollider2D left = gameObject.AddComponent(typeof(BoxCollider2D)) as BoxCollider2D;
		BoxCollider2D right = gameObject.AddComponent(typeof(BoxCollider2D)) as BoxCollider2D;
		BoxCollider2D bottom = gameObject.AddComponent(typeof(BoxCollider2D)) as BoxCollider2D;

		// offset

		top.offset   = new Vector2(0, +tileSize * mapWidth / 2 + 0.5f);
		left.offset  = new Vector2(-tileSize * mapWidth / 2 - 0.5f, 0);
		right.offset = new Vector2(+tileSize * mapWidth / 2 - 0.5f, 0);
		bottom.offset= new Vector2(0, -tileSize * mapWidth / 2 - 0.5f);

		// dimensions

		top.size   = new Vector2(tileSize * mapWidth  + 1, 1);
		left.size  = new Vector2(1, tileSize * mapHeight + 1);
		right.size = new Vector2(1, tileSize * mapHeight + 1);
		bottom.size= new Vector2(tileSize * mapWidth  + 1, 1);
	}

	public static bool IsWithinBounds(Vector2 position)
	{
		return (position.x > -staticMapWidth  / 2f * staticTileSize && position.x < staticMapWidth  / 2f * staticTileSize &&
		        position.y > -staticMapHeight / 2f * staticTileSize && position.y < staticMapHeight / 2f * staticTileSize);
	}

	// Returns which border or borders is too near (nearer than 'gap') the position
	// 0 - no border; 1 - top border; 2 - right border; 3 - bottom border; 4 - left border; 5 - top left corner; 6 - top right corner; 7 - bottom right corner; 8 - bottom left corner
	public static int IsNearOfWall(Vector2 position, float gap)
	{
		bool[] collidingWalls = new bool[4]; // 0t 1r 2b 3l

		// Check horizontally

		if (position.x < -staticMapWidth / 2f * staticTileSize + gap)
			collidingWalls[3] = true;
		else if (position.x > +staticMapWidth / 2f * staticTileSize - gap)
			collidingWalls[1] = true;

		// Check vertically

		if (position.y < -staticMapHeight / 2f * staticTileSize + gap)
			collidingWalls[2] = true;
		else if (position.y > +staticMapHeight / 2f * staticTileSize - gap)
			collidingWalls[0] = true;

		// return collision

		if (collidingWalls[3] && collidingWalls[0])
			return 5;
		if (collidingWalls[0] && collidingWalls[1])
			return 6;
		if (collidingWalls[1] && collidingWalls[2])
			return 7;
		if (collidingWalls[2] && collidingWalls[3])
			return 8;
		if (collidingWalls[0])
			return 1;
		if (collidingWalls[1])
			return 2;
		if (collidingWalls[2])
			return 3;
		if (collidingWalls[3])
			return 4;
		else
			return 0;
	}

	public static float GetLeftBorder() {
		return -staticMapWidth / 2f * staticTileSize;
	}

	public static float GetRightBorder() {
		return +staticMapWidth / 2f * staticTileSize;
	}

	public static float GetTopBorder() {
		return +staticMapHeight / 2f * staticTileSize;
	}

	public static float GetBottomBorder() {
		return -staticMapHeight / 2f * staticTileSize;
	}
}
