using UnityEngine;
using System.Collections;

public class MeshSkew : MonoBehaviour
{
	public Vector2 size = Vector2.one;
	public Vector2 offset = Vector2.zero;
	public float skew;

	Mesh mesh;
	MeshFilter mf;

	void OnEnable()
	{
		mf = GetComponent<MeshFilter>();
		mesh = new Mesh();
		mesh.MarkDynamic();
		UpdateVertices();
		int[] indices = new int[] { 0, 1, 2, 1, 3, 2};
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		Vector2[] uvs = new Vector2[] {
			new Vector2(0f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 0f),
			new Vector2(1f, 1f),
		};
		mesh.uv = uvs;
		mf.sharedMesh = mesh;
	}

	void OnDisable()
	{
		Mesh.DestroyImmediate(mesh, false);
		mesh = null;
		mf.sharedMesh = null;
	}
	
	void Update()
	{
		UpdateVertices();
	}

	void UpdateVertices()
	{
		float usedSkew = 0f;
		if (BoardCamera.instance)
			usedSkew = -BoardCamera.instance.Skew.skew.x;
		float ox = offset.x * usedSkew;
		float sy = size.y * usedSkew;
		Vector3[] vertices = new Vector3[] {
			new Vector3(-size.x - sy + ox, 0, -size.y + offset.y),
			new Vector3(-size.x + sy + ox, 0, size.y + offset.y),
			new Vector3(size.x - sy + ox, 0, -size.y + offset.y),
			new Vector3(size.x + sy + ox, 0, size.y + offset.y)
		};
		mesh.vertices = vertices;
	}
}
