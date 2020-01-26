using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * When entities are nearby, they are pooled in an SquadCluster.
 * This is needed to tell the Path Finder if one can pass through multiple entities.
 * If they are in the same cluster, they won't be passed through. If they're not, they will.
 */
public class EntityCluster : List<Transform>
{
    public bool isMoving = false;
    public bool isPlayer = false;

    public EntityCluster(List<Transform> entities) : base()
    {
        this.AddRange(entities);
    }

    public EntityCluster(List<Transform> entities, bool temporary)
    {
		if (temporary)
			AddRangeTemporary(entities);
		else
			AddRange(entities);
    }

    /*
     * Returns the distance to the nearest object in the cluster
     */
    public float GetClosestPassingDistance(Vector2 pos)
    {
        float closestDistance = 100f;

        for (int i = 0; i < Count; i++)
        {
	        if (this[i] == null || this[i].GetComponent<Entity>() == null)
	        {
				RemoveEntity(this[i]);
				continue;
	        }

            float dist = ((Vector2)this[i].position - pos).magnitude - this[i].GetComponent<Entity>().obstacleSize;

            if (dist < closestDistance)
            {
                closestDistance = dist;
            }
        }

        return closestDistance > 0 ? closestDistance : 0;
    }

    /*
     * returns the entity which is closest to the position
     */
    public Transform GetClosestObject(Vector2 pos)
    {
        int index = 0;
        float closestDistance = ((Vector2)this[0].position - pos).magnitude;

        for (int i = 1; i < this.Count; i++)
        {
            float dist = ((Vector2)this[i].position - pos).magnitude - this[i].GetComponent<Entity>().obstacleSize;

            if (dist < closestDistance)
            {
                index = i;
                closestDistance = dist;
            }
        }

        return this[index];
    }

    public void RemoveEntity(Transform entity)
    {
        // Delete if last entity left in cluster

        if (Count == 1)
        {
            GameMaster.entityClusters.Remove(this);
        }

		if (entity != null && entity.GetComponent<Entity>() != null)
	        entity.GetComponent<Entity>().cluster = null;

        Remove(entity);
	}

    public new void Add(Transform t) {
	    base.Add(t);
	    t.GetComponent<Entity>().cluster = this;
	}

    public new void AddTemporary(Transform t) {
	    base.Add(t);
	}

    public new void AddRange(IEnumerable<Transform> range) {
	    base.AddRange(range);

	    foreach (Transform t in range) {
		    Entity entity = t.GetComponent<Entity>();
		    entity.cluster = this;
	    }
	}

    public new void AddRangeTemporary(IEnumerable<Transform> range) {
	    base.AddRange(range);
    }

	public Vector2 GetAvgPos()
    {
        Vector2 combined = new Vector2();

        foreach (Transform t in this)
        {
            combined += (Vector2) t.position;
        }

        return combined / Count;
    }

    /*
     * method so a Moving entity which is in a cluster doesn't get confused with it's own position affecting the cluster position
     */
    public Vector2 GetAvgPosWithout(Transform exclude)
    {
        if (!Contains(exclude))
            return GetAvgPos();

        Vector2 combined = new Vector2();

        foreach (Transform t in this)
        {
            if (t != exclude)
                combined += (Vector2)t.position;
        }

        return combined / (Count - 1);
    }

    public float GetObstacleSizeWithout(Transform exclude)
    {
        // Sum all obstacle sizes (cubed) except from the given Entity

        float obstacleSize = this.Where(t => t != exclude).Sum(t => Mathf.Pow(t.GetComponent<Entity>().obstacleSize, 3));

        return Mathf.Pow(obstacleSize, 1f / 3f);
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