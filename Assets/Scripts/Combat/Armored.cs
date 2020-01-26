using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]

public class Armored : MonoBehaviour {
    // The damage will be divided by the armor multiplayer in case of an hit

    public float FrontArmorMultiplier = 1.0f; // Active, when armor is hit in the front
    public float SideArmorMultiplier = 1.0f; // Active, when armor is hit in the left or right
    public float BackArmorMultiplier = 1.0f; // Active, when armor is hit from behind
    public float ShockwaveArmorMultiplier = 1.0f; // Active at collision and explosion damage

    public Transform HitPrefab;

    [HideInInspector]
    public float angleFront;
    [HideInInspector]
    public float angleRight;
    [HideInInspector]
    public float angleBack;
    [HideInInspector]
    public float angleLeft;

    void Start()
    {
        if (transform.Find("Textures").Find("Armor") == null)
        {
            Debug.LogError("Armored Script instantiated without gameobject 'Textures' and child 'Armor'");
        }

		ReconsiderAngles();
    }

	public void ReconsiderAngles() {
		BoxCollider2D col = GetComponent<BoxCollider2D>();

		Vector2 frontRightCornerDirection = new Vector2(col.size.x, col.size.y);
		angleRight = Vector2.Angle(Vector2.up, frontRightCornerDirection);

		Vector2 backRightCornerDirection = new Vector2(col.size.x, -col.size.y);
		angleBack = Vector2.Angle(Vector2.up, backRightCornerDirection);

		Vector2 backLeftCornerDirection = new Vector2(-col.size.x, -col.size.y);
		angleLeft = -Vector2.Angle(Vector2.up, backLeftCornerDirection);

		Vector2 frontLeftCornerDirection = new Vector2(-col.size.x, col.size.y);
		angleFront = -Vector2.Angle(Vector2.up, frontLeftCornerDirection);
	}

	public void SetFront(float multiplier) {
		FrontArmorMultiplier = multiplier;
	}

	public void SetSides(float multiplier) {
		SideArmorMultiplier = multiplier;
	}

	public void SetBack(float multiplier) {
		BackArmorMultiplier = multiplier;
	}
}
