using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

    public int moveSpeed = 100;
	//[HideInInspector]
	public float rangeSubtract = 0;
    [HideInInspector]
    public LayerMask toHit;
    [HideInInspector]
    public float explosionDamage;
    [HideInInspector]
    public float explosionRadius;
    [HideInInspector]
    public Transform parent;

    private Vector3 startPos;
    public float range = 2; // ToDo: public debug only

    // Use this for initialization
    void Start () {
        startPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if (GameMaster.IsPaused()) return;

		transform.Translate(Vector3.up * Time.deltaTime * moveSpeed);

        // Check if already travelled maximum distance

        if ((transform.position - startPos).magnitude > range)
        {
            if (explosionDamage > 0.01)
                explode();

            Destroy(transform.gameObject, 0);
        }
	}

    public void SetRange (float range)
    {
        this.range = range - rangeSubtract;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform == parent) // Bullets cannot hit the Object from where they are shot
            return;
        if (toHit == (toHit | (1 << col.gameObject.layer))) // Check if the object can even be hit by the bullet
        {
            if (col.gameObject.GetComponent<Entity>() != null)
            {
                if (explosionDamage == 0)
                {
                    // Bullet has hit an friend / enemy

                    if (col.gameObject.GetComponent<Armored>() != null)
                    {
                        // Entity is armored
                        
                    }
                } else
                {
                    //TODO: add explosion
                }
            }

            explode();

            Destroy(gameObject);
        }
    }

    private void explode()
    {
        Transform explosion = Instantiate(GameMaster.explosionPrefab, transform.position, Quaternion.Euler(0, 0, 0)) as Transform;
        explosion.GetComponent<Explosion>().detonate(explosionRadius, explosionDamage);
    }
}
