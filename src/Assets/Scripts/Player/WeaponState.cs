using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponState : MonoBehaviour {

	public GameObject playerWeapon;
	public GameObject weaponStateBar;

	public Font arial;

	private Weapon weapon;
	private BarElement weaponState;
	private UnityEngine.UI.Text bulletNumberIndicator;

	private Color noBullets;
	private Color bulletsButNoFiring;
	private Color readyToFire;
	private Color cool;
	private Color mediumTemp;
	private Color hot;

	void Start()
	{
		OnStart();
	}

	public void OnStart () {
		weapon = playerWeapon.GetComponent<Weapon> ();
		weaponState = weaponStateBar.GetComponent<BarElement> ();
		bulletNumberIndicator = weaponStateBar.transform.Find ("BulletIndicator").GetComponent<UnityEngine.UI.Text>();

		readyToFire = new Color (0.0f, 0.8f, 0.2f);
		noBullets = new Color(0.5f, 0.2f, 0.0f);
		bulletsButNoFiring = new Color (0.7f, 0.7f, 0.0f);
		cool = new Color(0.0f, 0.8f, 0.2f);
		mediumTemp = new Color (1.0f, 1.0f, 0.0f);
		hot = new Color (1.0f, -0.4f, 0.0f);

		// change bullet number to infinity if needed
		
		if(!weapon.hasMagazine)
		{
			char character = '\u221E';
			string infinity = character.ToString();
			bulletNumberIndicator.text = infinity;
			bulletNumberIndicator.font = arial;
			bulletNumberIndicator.fontSize = bulletNumberIndicator.fontSize + 5;

			// set bar to full
			
			weaponState.part = 1;
		}
	}

	void Update () {
		if (GameMaster.IsPaused()) return;

		// Update filled part and color

		// Test if weapon is currently reloading

		if (!Mathf.Approximately (weapon.rateWhenHot, 1.0f)) {
			// Set weapon color to hotness

			if (weapon.hotness < weapon.hotnessStart) {
				// Set color between cool and medium temperature

				float degree = 1f * weapon.hotness / weapon.hotnessStart;

				Color color = new Color (
					              degree * mediumTemp.r + (1 - degree) * cool.r,
					              degree * mediumTemp.g + (1 - degree) * cool.g,
					              degree * mediumTemp.b + (1 - degree) * cool.b);
				weaponState.color = color;
			} else {
				// Set color between medium and hot temperature

				float degree = 1f * (weapon.hotness - weapon.hotnessStart) / (weapon.hotnessEnd - weapon.hotnessStart);

				if (degree > 1)
					degree = 1;	

				Color color = new Color (
					degree * hot.r + (1 - degree) * mediumTemp.r,
					degree * hot.g + (1 - degree) * mediumTemp.g,
					degree * hot.b + (1 - degree) * mediumTemp.b);
				weaponState.color = color;
			}
		} else {
			// Weapon can not get hot

			if (!weapon.hasMagazine) {
				// weapon has infinite ammunition

				weaponState.part = 1;
			} else if (weapon.startedReloadingMagazineAt != 0) {
				// Weapon is reloading

				if (weapon.shootWhileReload == true) {
					// Weapon is reloading and can shoot while reloading

					if (weapon.bulletsLeft > 0 && (Time.time - weapon.firedLastBulletAt) > weapon.fireDelay) {
						// Weapon can shoot

						weaponState.color = readyToFire;
					} else {
						// Weapon cannot shoot

						if (weapon.bulletsLeft > 0) {
							// There are still bullets, which can be fired soon

							weaponState.color = bulletsButNoFiring;
						} else {
							// No bullets left

							weaponState.color = noBullets;
						}
					}
				} else {
					weaponState.color = noBullets;
				}

				// Set fill amount to reloading progress

				float progress = (float)(Time.time - weapon.startedReloadingMagazineAt) * 1f / weapon.reloadtimeInSec;

				weaponState.part = progress;
			} else {
				// Set color

				weaponState.color = readyToFire;

				// Set fill amount

				weaponState.part = (float) (1f * weapon.bulletsLeft / weapon.magazineSize);
			}
		}

		// Set bullet number

		if (weapon.hasMagazine)
			bulletNumberIndicator.text = "" + weapon.bulletsLeft;
	}
}
