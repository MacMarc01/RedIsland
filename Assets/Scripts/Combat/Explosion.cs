using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour {
    public AnimationCurve curve;
	private float pushFactor = .2f;

    public void detonate(float radius, float damage)
    {
        // Apply damage
        
		int i = 0;

        foreach (GameObject obj in GameMaster.entities)
        {
	        if (obj.GetComponent<Entity>() == null)
		        continue;

	        Transform t = obj.transform;

            float distance = (new Vector2(t.position.x, t.position.y) - new Vector2(transform.position.x, transform.position.y)).magnitude;
            
            if (distance < radius)
            {
                // Entity near explosion

                // Calc armor

                float armor = 0;

                if (t.GetComponent<Armored>() != null)
                {
                    armor = t.GetComponent<Armored>().ShockwaveArmorMultiplier;
                }

                if (armor < 1)
                    armor = 1;

                // Calc damage

                int dmg = Mathf.RoundToInt(Mathf.Clamp(curve.Evaluate(distance / radius), 0f, 1f) * damage / armor);
				
				// Apply damage
				t.GetComponent<Entity>().damage(dmg, false);

				// Apply backknock, if rigid

				if (t.GetComponent<Rigidbody2D>() != null) {
					Rigidbody2D rb2d = t.GetComponent<Rigidbody2D> ();
					rb2d.AddForce ((t.transform.position - transform.position) * pushFactor * (dmg * armor));
				}
            }

			i++;
        }

		// Let die

		for (int f = 0; f < GameMaster.entities.Count; f++) {
			GameMaster.entities[f].GetComponent<Entity>().letDie();
		}
    }
}
