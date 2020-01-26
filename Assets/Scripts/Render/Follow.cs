using UnityEngine;
using System.Collections;

public class Follow : MonoBehaviour {

    public Transform follow;
    public float damping = 0; // Smoothly follow the object (damping). 0 to deactivate, value between 0 and 1 for smoothing
    public float minX = 0; // Do not follow beyond this X coordinate, 0 to deactivate
    public float maxX = 0; // Do not follow beyond this Y coordinate, 0 to deactivate
    public float minY = 0; // Do not follow beyond this X coordinate, 0 to deactivate
    public float maxY = 0; // Do not follow beyond this Y coordinate, 0 to deactivate

    // Use this for initialization
    void Start () {
        // Check if there is anything to follow
    }
    
    void Update()
    {
        // Test if follow is null

        if (follow == null)
            return;

        // Do movement

        float x, y;

        if (damping == 0)
        {
            y = follow.position.y;
            x = follow.position.x;
        } else
        {
            y = (1 - damping) * follow.position.y + damping * transform.position.y;
            x = (1 - damping) * follow.position.x + damping * transform.position.x;
        }
        if (minX != 0 || maxX != 0 || minY != 0 || maxY != 0 )
        {
            x = Mathf.Clamp(x, minX, maxX);
            y = Mathf.Clamp(y, minY, maxY);
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
