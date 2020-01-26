using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * When squads are nearby, they are pooled in an SquadCluster.
 * This is needed to tell the Path Finder if one can pass through multiple squads.
 * If they are in the same cluster, they won't be passed through. If they're not, they will.
 */
public class SquadCluster : List<Transform>
{
	public int team;

	public SquadCluster(List<Transform> squads, int team) : base() {
		this.team = team;
		this.AddRange(squads);
	}

	public SquadCluster(List<Squad> squads, int team) : base() {
		this.team = team;
		this.AddRange(squads);
	}

	public void RemoveSquad(Transform squad)
    {
        // Delete if last entity left in cluster

        if (Count == 1)
        {
	        if (team == 0)
		        GameMaster.friendSquadClusters.Remove(this);
			else
		        GameMaster.enemySquadClusters.Remove(this);
		}

		if (squad != null)
	        squad.GetComponent<Squad>().cluster = null;

        Remove(squad);
	}

	public new void Add(Transform t) {
		base.Add(t);

		if (t.GetComponent<Squad>() != null)
			t.GetComponent<Squad>().cluster = this;
	}

	public new void Add(Squad s) {
		base.Add(s.transform);

		s.cluster = this;
	}

	public new void AddRange(IEnumerable<Transform> range) {
	    base.AddRange(range);

	    foreach (Transform t in range) {
		    Squad squad = t.GetComponent<Squad>();

		    if (squad != null)
			    squad.cluster = this;
	    }
	}

    public new void AddRange(IEnumerable<Squad> range) {
	    base.AddRange(range.Select(squad => squad.transform));

	    foreach (Squad squad in range) {
		    squad.cluster = this;
	    }
    }

	public new string ToString()
    {
	    string msg = "";
	    for (int i = 0; i < Count; i++)
	    {
		    msg += this[i].name + ", ";
	    }

	    return "[" + msg.Substring(0, msg.Length - 2) + "]";
    }
}
