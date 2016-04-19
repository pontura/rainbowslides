using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This component is used to identify the transforms that are used to locate the playes on their start position.
public class StartPlace : MonoBehaviour
{
	public static List<StartPlace> instances = new List<StartPlace>();
	
	// The center of all existing start places.
	public static Vector3 Center
	{
		get
		{
			if (instances.Count == 0)
				return Vector3.zero;
			Vector3 center = Vector3.zero;
			foreach (StartPlace a in instances)
				center += a.transform.position;
			return center / instances.Count;
		}
	}
	
	void OnEnable()
	{
		instances.Add(this);
	}

	void OnDisable()
	{
		instances.Remove(this);
	}
	
	void OnDrawGizmos()
	{
		Vector3 size = Vector3.one;
		Gizmos.DrawCube(transform.position + Vector3.up * size.y * 0.5f, size);
	}
	
}
