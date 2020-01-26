using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSelector : MonoBehaviour
{
	public GameObject GearSelection;
	public GameObject turret;
	public GameObject player;
	public GameObject ingameUI;
	public GameObject cam;
	public RectTransform strategyPanel;
	public GameObject[] follows;
	public GameObject angleRange;

	private GameObject weaponObject;
	private Weapon weapon;

	public void SelectWeapon()
	{
		// Destroy old turret

		GameObject oldTurret = player.transform.Find("Textures").Find("Turret").gameObject;
		DestroyImmediate(oldTurret);

		// Instantiate turret object
		
		GameObject newTurret = Instantiate(turret, player.transform.Find("Textures"));
		newTurret.transform.localPosition = new Vector3(0, newTurret.GetComponent<Turret>().posRelY, newTurret.transform.localPosition.z);
		newTurret.name = "Turret";

		// Apply armor offset

		GearSelection gear = GearSelection.GetComponent<GearSelection>();
		newTurret.transform.localPosition += new Vector3(0, gear.turretOffsets[gear.selectedArmor], 0);

		// make adjustments for weapon

		weaponObject = newTurret.transform.Find("Gun").gameObject;
		weapon = weaponObject.GetComponent<Weapon>();

		angleRange.GetComponent<AngleRangeAnimation>().weapon = weaponObject.transform;

		SetPlayerAiming(newTurret);
		SetRanges();
		SetFollows(newTurret);
		SetWeaponState();
		SetCamera();
		setStrategyPanel();
	}

	private void SetPlayerAiming(GameObject obj)
	{
		PlayerAiming aiming = player.GetComponent<PlayerAiming>();
		aiming.setTurret(obj.transform);
		aiming.maxDistance = weapon.range;
		aiming.minDistance = weapon.minRange > 1.5f ? weapon.minRange : 1.5f;
		aiming.OnStart();
	}

	private void SetRanges()
	{
		// Get Range UIs

		GameObject minRange = ingameUI.transform.Find("Ranges").Find("MinimumRange").gameObject;
		GameObject maxRange = ingameUI.transform.Find("Ranges").Find("MaximumRange").gameObject;
		GameObject angRange = ingameUI.transform.Find("Ranges").Find("AngleRange").gameObject;

		// Set Weapon

		minRange.GetComponent<MinRangeAnimation>().SetWeapon(weaponObject.transform);
		maxRange.GetComponent<MaxRangeAnimation>().SetWeapon(weaponObject.transform);
		angRange.GetComponent<AngleRangeAnimation>().SetWeapon(weaponObject.transform);

		// Activate

		minRange.GetComponent<MinRangeAnimation>().StartUp();
		maxRange.GetComponent<MaxRangeAnimation>().StartUp();
		angRange.GetComponent<AngleRangeAnimation>().StartUp();
	}

	private void SetFollows(GameObject newTurret)
	{
		foreach(GameObject g in follows)
		{
			g.GetComponent<Follow>().follow = newTurret.transform;
		}
	}

	private void SetWeaponState()
	{
		player.GetComponent<WeaponState>().playerWeapon = weaponObject;
		player.GetComponent<WeaponState>().OnStart();
	}

	private void SetCamera()
	{
		cam.GetComponent<CameraClamp>().ChangeViewport(0, 0, 0, 260);
	}

	private void setStrategyPanel()
	{
		strategyPanel.position = new Vector3(Screen.width, strategyPanel.position.y);
	}
}
