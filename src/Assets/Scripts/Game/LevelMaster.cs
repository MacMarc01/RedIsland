using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The LevelMaster controls level funcionality, such as squad behavior, changes occuring
// during the level (checkpoints, reinforcements etc.) and player objectives.
// Each level has its own script derived from this class
public abstract class LevelMaster : MonoBehaviour
{
	public static List<Squad> squads;

	public void Start() {
		// Inits

		squads = new List<Squad>();
		Transform friends = GameMaster.instance.friendContainer.transform;
		Transform enemies = GameMaster.instance.enemyContainer.transform;

		for (int i = 0; i < friends.childCount; i++) {
			Squad squad = friends.GetChild(i).GetComponent<Squad>();
			if (squad.gameObject.activeSelf)
				squads.Add(squad);
		}

		for (int i = 0; i < enemies.childCount; i++) {
			Squad squad = enemies.GetChild(i).GetComponent<Squad>();
			if (squad.gameObject.activeSelf)
				squads.Add(squad);
		}
	}
}
