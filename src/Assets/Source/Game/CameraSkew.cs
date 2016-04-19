using UnityEngine;
using System.Collections;

// This scrips skewes a camera.
[ExecuteInEditMode]
public class CameraSkew : MonoBehaviour
{
	public Camera _camera;
	public Vector2 skew;
	
	void OnDisable()
	{
		if (_camera)
			_camera.ResetWorldToCameraMatrix();
	}
	
	public void Update()
	{
		if (_camera)
		{
			_camera.ResetWorldToCameraMatrix();
			Matrix4x4 camMatrix = _camera.worldToCameraMatrix;
			camMatrix[0, 2] = skew.x;
			camMatrix[1, 2] = skew.y;
			_camera.worldToCameraMatrix = camMatrix;
		}
	}
}
