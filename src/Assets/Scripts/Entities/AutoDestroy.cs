using UnityEngine;
using System.Collections;

public class AutoDestroy : MonoBehaviour {
    public float destroyAfter;

	void Start () {
        Destroy(gameObject, destroyAfter);
	}
}
