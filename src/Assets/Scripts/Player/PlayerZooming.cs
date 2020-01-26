using UnityEngine;
using System.Collections;

public class PlayerZooming : MonoBehaviour {

    public int maxZoom;
    public int minZoom;
    public bool allowZooming; // Enable manual zooming by the player
    public float zoomScale;

    public Camera cam; // The main camera
    public Transform t; // The object which is moved along the Z-Axis

    private float goalSize = 8; // The camera zooms smoothly. This is the acquired orthographic size.
    private float smoothFactor = .8f;

    void Start ()
    {
        cam = Camera.main;
    }
    
	void Update () {
        if (allowZooming == true)
        {
            // Get the input of the player (mouse wheel)

            float input = Input.GetAxis("Mouse ScrollWheel");

            // Zoom, if needed
            
            if (input > 0)
                ZoomIn();
            else if (input < 0)
                ZoomOut();
        }

        // Set camera orthographic size closer to acquired value

        cam.orthographicSize =
            cam.orthographicSize * smoothFactor
          + goalSize * (1 - smoothFactor);

        t.position = new Vector3(t.position.x, t.position.y, -cam.orthographicSize);
	}

	public void setMaxZoom(int value)
	{
		maxZoom = value;
	}

	public void setMinZoom(int value)
	{
		maxZoom = value;
	}

	public void setZoomScale(int value) {
		zoomScale = value;
	}

	public void ZoomIn()
    {
        goalSize -= zoomScale;
        if (goalSize < minZoom)
        {
            goalSize = minZoom;
        }
    }

    public void ZoomOut()
    {
        goalSize += zoomScale;
        if (goalSize > maxZoom)
        {
            goalSize = maxZoom;
        }
    }

	public void ZoomToValue(int value) {
		goalSize = value;
	}
}
