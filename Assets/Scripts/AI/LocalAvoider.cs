using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D))]
public class LocalAvoider : MonoBehaviour {
    //[HideInInspector]
    public bool colliding = false;
    public Transform colliderT;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.GetComponent<Entity>() && col.gameObject.GetComponent<Entity>().CollideWithPathfinder == true)
        {
            colliding = true;
            colliderT = col.transform;
        }
    }

	void OnTriggerStay2D(Collider2D col)
	{
		if (col.gameObject.GetComponent<Entity>() && col.gameObject.GetComponent<Entity>().CollideWithPathfinder == true)
		{
			colliding = true;
			colliderT = col.transform;
		}
	}

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.gameObject.GetComponent<Entity>())
        {
            colliding = false;
            colliderT = null;
        }
    }
}
