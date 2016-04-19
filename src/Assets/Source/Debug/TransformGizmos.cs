using UnityEngine;
using System.Collections;

// A debug script that draws a gizmo on each contained transform.
public class TransformGizmos : MonoBehaviour
{
	public float size = 1f;
	public Vector3 center = Vector3.zero;
	public Color color = Color.white;
	public bool showChildren = true;
	
	void OnDrawGizmos()
	{
		Gizmos.color = color;
		DoTransformGizmo(transform, size, showChildren);
	}
	
	public static void DoTransformGizmo(Transform t, float size, bool children)
	{
		Vector3 vs = Vector3.one * size;
		Gizmos.DrawCube(t.position, vs);
		if (children)
		{
			foreach (Transform t2 in t)
				DoTransformGizmo(t2, size, true);
		}
	}
	
}
