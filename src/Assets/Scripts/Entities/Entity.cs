using UnityEngine;
using System.Collections;

[SelectionBase]
public class Entity : MonoBehaviour {

    public int maxHealth;
    public int regenerationPerMinute;
    public bool showBar;
    public float distance = 1f;
    public bool CollideWithPathfinder = true;

    public ExplosionMode explosionMode;
    public Object[] removeOnDeath;
    public Transform body; // This Transform's sprite is changed on death
    public Sprite wrecked; // The body's sprite is changed to this
    public Transform explosionEffect;

    [HideInInspector]
    public int team; // 0 = friend (player); 1 = enemy (computer)

	public bool isPlayer = false;

    public int combatStrength; // The estimated combat strength with maximum health. Used to calculate, whether AI attacks or not

    public float obstacleSize = 1f; // determines the avoidance radius for other entities

    public Sprite red;
    public Sprite yellow;
    public Sprite green;

    public  int health;
    private float nextRegeneration;
    private Transform healthBar;
    private Transform bar;

    [HideInInspector] public bool hasSquad;
    public Squad squad;
	[HideInInspector]
	public EntityCluster cluster = null;

	public enum ExplosionMode
	{
		BURN,
		NOTHING,
		EXPLODE
	}

	public enum DeathType
	{
		DESTRUCTION,
		DROWNING
	}

	void Start () {
        health = maxHealth;

        if (showBar)
        {
            healthBar = transform.Find("HealthBar");
            
            bar = healthBar.Find("Bar");

            healthBar.Find("Corner1").localPosition = new Vector3(-0.03f - (health/600f), 0);
            healthBar.Find("Corner2").localPosition = new Vector3(0.03f + (health/600f), 0);

            healthBar.Find("Corner1").localScale = new Vector2(2, 2);
            healthBar.Find("Corner2").localScale = new Vector2(2, 2);

            bar.localScale = new Vector3((health/30f), 1);
        }

        // Check if squad member

        string name = transform.parent.name;

		if (name.Contains("Squad")) {
			// Is squad member

			hasSquad = true;

			squad = transform.parent.GetComponent<Squad>();
		} else if (isPlayer) {
			hasSquad = true;
			squad = GetComponent<Squad>();
		} else {
			hasSquad = false;
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (GameMaster.IsPaused()) return;

        // Check for regeneration

	    if (health < maxHealth && regenerationPerMinute > 0 && Time.time >= nextRegeneration)
        {
            health++;

            nextRegeneration = Time.time + (60f / regenerationPerMinute);

            // Update bar color, if needed

            if (showBar && (1f * health / maxHealth) > 0.4 && (1f * health / maxHealth) <= 0.8)
            {
                bar.GetComponent<SpriteRenderer>().sprite = yellow;
            }
            else if (showBar && (1f * health / maxHealth) > 0.8)
            {
                bar.GetComponent<SpriteRenderer>().sprite = green;
            }

            if (showBar)
            {
                bar.localScale = new Vector3((health / 30f), 1);
                bar.localPosition = new Vector2(-((maxHealth - health) / 600f), 0);
            }
        }

        if (showBar)
        {
            // Rotate bar to normal

            healthBar.eulerAngles = new Vector3(0, 0, 0);

            // Set bar's position under the parent's

            Vector3 pos = transform.position;
            pos.y -= distance; // Keep the distance equal

            healthBar.position = pos;
        }
	}

	public void damage (int amount) {
		damage (amount, true);
	}

	public void damage (int amount, bool letDie)
    {
        health -= amount;
        health = health > -3 ? health : -3; // Clamp

        // Update health bar

        if (showBar)
        {
            bar.localScale = new Vector3((health / 30f), 1);
            bar.localPosition = new Vector2(-((maxHealth - health) / 600f), 0);

            if ((1f * health / maxHealth) <= 0.8 && (1f * health / maxHealth) > 0.4)
            {
                bar.GetComponent<SpriteRenderer>().sprite = yellow;
            }
            else if ((1f * health / maxHealth) <= 0.4)
            {
                bar.GetComponent<SpriteRenderer>().sprite = red;
            }
        }

		// Update squad

		if (squad != null)
			squad.damageReceived += amount;

        if (health <= 0 && letDie)
        {
            // Death

            OnDeath();
        }

        // Test if object is player

        if (transform.name.Equals("Player"))
        {
            // Add camera shake

            Camera.main.transform.GetComponent<CameraShake>().Shake(Mathf.Sqrt(amount) / maxHealth * 7, Mathf.Sqrt(amount) / maxHealth * 50);
        }
    }

	public void SetCombatStrength(int value) {
		combatStrength = value;
	}

	/*
	 * Sometimes there must be time between damage and death, this method checks, if health is below zero
	 */
	public void letDie() {
		if (health < 0) {
			// Death

			OnDeath();
		}
	}

    public int GetHealth()
    {
        return health;
	}

	private void OnDeath()
	{
		Die(DeathType.DESTRUCTION);
	}

	private void OnDeath(DeathType type)
	{
		Die(type);
	}

	private void Die(DeathType type)
	{
		// Move entity

		// DEBUG ONLY

		GameObject parent = GameObject.Find("wrecks") ?? new GameObject("wrecks");
		transform.parent = parent.transform;

		// Remove entity

		cluster?.RemoveEntity(transform);

		if (transform != GameMaster.player && team == 0)
			GameMaster.friends.Remove(gameObject);
		else if (transform != GameMaster.player)
			GameMaster.enemies.Remove(gameObject);

		GameMaster.entities.Remove(gameObject);

		if (hasSquad)
			squad.RemoveMember(transform);

		if (squad != null && squad.members.Count == 0)
		{
			// Remove squad

			if (!isPlayer)
				Destroy(squad.gameObject);

			squad.cluster?.RemoveSquad(squad.transform);

			DebugLevel.squads.Remove(squad);
		}

		if (explosionMode == ExplosionMode.BURN && type == DeathType.DESTRUCTION)
		{
			// Play death animation

			if (explosionEffect != null)
				Instantiate(explosionEffect, transform.position, transform.rotation);

			// Change sprite

			body.GetComponent<SpriteRenderer>().sprite = wrecked;

			// Remove GameObjects

			foreach (Object obj in removeOnDeath)
			{
				Destroy(obj);
			}

			// Change layer

			gameObject.layer = 10;
		} else if (type == DeathType.DROWNING) {

		}

		if (transform.name == "Player" && !GameMaster.isPlayerDead)
		{
			// player death

			GameMaster.PlayerDeath(type);
		}

		// Remove itself

		Destroy(transform.GetComponent<Entity>());

		// Update Map

		Bounds bounds = GetComponent<Collider2D>().bounds;
		bounds.Expand(2f);

		GameObject.Find("A*").GetComponent<UpdatePathfindingMap>().UpdateSection(bounds);

		// Test if won

		if (GameMaster.enemies.Count == 0) {
			GameMaster.instance.Win();
		}
	}
}
