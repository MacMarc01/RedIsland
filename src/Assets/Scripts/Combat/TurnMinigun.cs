using UnityEngine;
using System.Collections;

public class TurnMinigun : MonoBehaviour {
    public Transform audioObject;
    public Transform gun;

    public Transform barrel1;
    public Transform barrel2;
    public Transform barrel3;
    public Transform barrel4;

    private Weapon weapon;
    private AdjustableSound sound;
    private AudioSource src;
    private float maxVolume;

    private float rotation = 0; // From 0 - 360 degrees

	void Start () {
        weapon = gun.GetComponent<Weapon>();

        sound = audioObject.GetComponent<AdjustableSound>();
        src = audioObject.GetComponent<AudioSource>();

        maxVolume = sound.GetVolume();
	}
	
	// Update is called once per frame
	void Update () {
		if (GameMaster.IsPaused()) return;

		// Rotate minigun

		rotation += weapon.speed / 90;
        rotation %= 360;

        barrel1.localPosition = new Vector3(0.06f * Mathf.Sin(rotation), barrel1.localPosition.y, 0);
        barrel2.localPosition = new Vector3(0.06f * Mathf.Sin(rotation+90), barrel1.localPosition.y, 0);
        barrel3.localPosition = new Vector3(0.06f * Mathf.Sin(rotation+180), barrel1.localPosition.y, 0);
        barrel4.localPosition = new Vector3(0.06f * Mathf.Sin(rotation+270), barrel1.localPosition.y, 0);

        // Set rotation volume according to speed

        sound.SetVolume((weapon.speed / weapon.timeTillShoot) * maxVolume);
        src.pitch = ((weapon.speed / weapon.timeTillShoot) + 2);
    }
}
