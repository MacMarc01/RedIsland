using UnityEngine;
using System.Collections;
using System.Threading;
using Pathfinding;
using Unity.Jobs;

[RequireComponent(typeof(AstarPath))]
public class UpdatePathfindingMap : MonoBehaviour {

    public float updatesPerSecond = 0.2f;
    AstarPath map;
	
	// Update is called once per frame
	void Start () {
        map = GetComponent<AstarPath>();

        StartCoroutine(UpdateMapRoutine());
	}

    private IEnumerator UpdateMapRoutine()
    {
        yield return new WaitForSeconds(1f / updatesPerSecond);

        map.Scan();

        StartCoroutine(UpdateMapRoutine());
    }

	public void UpdateSection(Bounds bounds)
	{
		var guo = new GraphUpdateObject(bounds) {updatePhysics = true};

		map.UpdateGraphs(guo);
	}
}
