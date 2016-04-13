using UnityEngine;
using System.Collections;

// Class uset to smoothly change the target frame rate of the device.
// Use this to avoid FPS hiccups.
// Note that on some devices its better to let the max FPS run without this limitation.
public class AutoTargetFPS : MonoBehaviour
{
	public int minFPS = 15;
	public int maxFPS = 60;
	public int addFPS = 2;
	private float lastTime;

	void LateUpdate()
	{
		float elapsed = Time.realtimeSinceStartup - lastTime;
		if (lastTime != 0f && elapsed > 0f)
			Application.targetFrameRate = (int)Mathf.Clamp(addFPS + 1f / elapsed, minFPS, maxFPS);
		lastTime = Time.realtimeSinceStartup;
	}
	
}
