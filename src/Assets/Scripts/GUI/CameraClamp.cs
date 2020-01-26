using UnityEngine;
using System.Collections;

/**
 * Script to create spacing betweem camera viewport and game window in absolute pixels
 */

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraClamp : MonoBehaviour
{
	public int top;
	public int bottom;
	public int left;
	public int right;

	private int topPixels;
	private int bottomPixels;
	private int leftPixels;
	private int rightPixels;

	private Camera cam;

	private int width;
	private int height;

	private bool changedViewport = true;

	void OnEnable() {
		ChangeViewport (top, bottom, left, right);
	}

	void Start()
	{
		cam = GetComponent<Camera>();
		
		width = 0;
		height = 0;
	}
	
	void Update () {
        if (width != Screen.width || height != Screen.height || changedViewport)
        {
            // Screen size changed

            width = Screen.width;
            height = Screen.height;

            Rect viewport = cam.pixelRect;
            viewport.x = leftPixels;
            viewport.y = topPixels;
            viewport.width = Screen.width - (leftPixels + rightPixels);
            viewport.height = Screen.height - (topPixels + bottomPixels);

            cam.pixelRect = viewport;

            changedViewport = false;

            Camera.main.Render();
        }
    }

	public void ChangeViewport(int topSpacing, int bottomSpacing, int leftSpacing, int rightSpacing) {
		topPixels = topSpacing;
		bottomPixels = bottomSpacing;
		leftPixels = leftSpacing;
		rightPixels = rightSpacing;

		changedViewport = true;
	}
}
