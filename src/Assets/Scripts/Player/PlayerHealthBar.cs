using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class PlayerHealthBar : MonoBehaviour {

	public GameObject healthBar;

	private BarElement health;
	private Entity player;

	private Color barColor = new Color(0.5f, 0.0f, 0.0f);

	// Use this for initialization
	void Start () {
		health = healthBar.GetComponent<BarElement> ();
		player = GetComponent<Entity> ();

		health.color = barColor;
	}
	
	// Update is called once per frame
	void Update () {
		health.part = 1f * player.GetHealth () / player.maxHealth;
	}
}
