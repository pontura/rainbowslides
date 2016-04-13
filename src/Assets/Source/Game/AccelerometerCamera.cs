using UnityEngine;
using System.Collections;

// This class handles the accelerometer input. Those values are used to modify the CameraSkew values.
public class AccelerometerCamera : MonoBehaviour
{
	public CameraSkew cameraSkew;
	public Vector2 sensitivity = Vector2.one;
	public Vector3 rotationEulers = Vector3.zero;
	public float accelerationCatchSpeed = 1f;
	public Vector2 min = Vector2.zero;
	public Vector2 max = Vector2.one;
	private Vector2 originalSkew;
	private Vector3 acceleration;
	public bool emulateAcceleration;
	public Vector3 emulatedAcceleration;
	public static AccelerometerCamera instance;
	public bool useEndSkew = true;

	void OnEnable()
	{
		instance = this;
		if (cameraSkew)
			originalSkew = cameraSkew.skew;
	}

	void OnDisable()
	{
		if (cameraSkew)
			cameraSkew.skew = originalSkew;
	}
	
	void Update()
	{
		if (Ingame.instance.state == Ingame.State.endScene && useEndSkew)
		{
			if (cameraSkew)
			{
				cameraSkew.skew.x = Mathf.Lerp(cameraSkew.skew.x, 0, Time.deltaTime * accelerationCatchSpeed);
				cameraSkew.skew.y = Mathf.Lerp(cameraSkew.skew.y, originalSkew.y, Time.deltaTime * accelerationCatchSpeed);
			}
		}
		else if (DualCamera.instance.Active)
		{
			if (cameraSkew)
			{
				cameraSkew.skew.x = originalSkew.x;
				cameraSkew.skew.y = originalSkew.y;
			}
		}
		else
		{
			Vector3 inputAcceleration = emulateAcceleration ? emulatedAcceleration : Input.acceleration;
			acceleration = Vector3.Lerp(acceleration, inputAcceleration, Time.deltaTime * accelerationCatchSpeed);
			if (cameraSkew)
			{
				Vector3 skewVector = Quaternion.Euler(rotationEulers) * acceleration;
				cameraSkew.skew.x = Mathf.Clamp(originalSkew.x + skewVector.x * sensitivity.x, min.x, max.x);
				cameraSkew.skew.y = Mathf.Clamp(originalSkew.y + skewVector.y * sensitivity.y, min.y, max.y);
			}
		}
	}
}
