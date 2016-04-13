using UnityEngine;
using System.Collections;

// This class shows a gizmo on whatever its attached to.
public class Gizmer : MonoBehaviour
{
	public Vector3 size = Vector3.one;
	public Vector3 center = Vector3.zero;
	
	void OnDrawGizmos()
	{
		Gizmos.DrawCube(transform.TransformPoint(center), size);
	}
}
