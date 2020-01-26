using UnityEngine;
using System.Collections;

public class RocketLauncherGraphics : MonoBehaviour {

    public Sprite zeroRockets;
    public Sprite oneRocket;
    public Sprite twoRockets;

    private Weapon weapon;
    private int mode = 0;
    
	void Start () {
        weapon = transform.Find("Gun").GetComponent<Weapon>();

        mode = 0;
	}
	
	void Update () {
		if (GameMaster.IsPaused()) return;

		if (mode != weapon.bulletsLeft)
        {
            // Change sprite

            if (weapon.bulletsLeft == 0)
            {
                transform.GetComponent<SpriteRenderer>().sprite = zeroRockets;

                mode = 0;
            } else if (weapon.bulletsLeft == 1)
            {
                transform.GetComponent<SpriteRenderer>().sprite = oneRocket;

                mode = 1;
            } else
            {
                transform.GetComponent<SpriteRenderer>().sprite = twoRockets;

                mode = 2;
            }
        }
	}
}
