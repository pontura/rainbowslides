using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

// A handy editor window that generates meshes from input parameters.
// This can be used to define chutes, ladders, and skewed meshes.
public class MeshGenerator : EditorWindow
{
	public enum Mode
	{
		billboardPlane,
		floorPlane,
		skewX,
		skewY,
		skewZ
	}
	
	public static MeshGenerator instance;
	public Vector2 size = Vector2.one;
	public AnimationCurve curve1 = AnimationCurve.Linear(0f, 1f, 1f, 1f);
	public AnimationCurve curve2 = AnimationCurve.Linear(0f, 1f, 1f, 1f);
	public float skew1 = 0f;
	public float skew2 = 0f;
	public int steps = 4;
	public Vector2 uvMax = Vector2.one;
	public string lastName = "newMesh";
	public string lastDirectory = "Assets";
	public Mode mode = Mode.skewZ;
	public bool turnUV;
	public bool invertUVx;
	public bool invertUVy;
	public bool centered;
	public bool invertDirection;
	
	[MenuItem("Window/Mesh Generator")]
	static void Init()
	{
		instance = ScriptableObject.CreateInstance<MeshGenerator>();
		instance.Show();
	}
	
	void OnGUI()
	{
		//GUILayout.Label("Mesh Editor");
		mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);
		
		size = EditorGUILayout.Vector2Field("size", size);
		
		if (mode == Mode.skewX || mode == Mode.skewY || mode == Mode.skewZ)
		{
			steps = EditorGUILayout.IntField("steps", steps);
			curve1 = EditorGUILayout.CurveField("curve1", curve1);
			curve2 = EditorGUILayout.CurveField("curve2", curve2);
			skew1 = EditorGUILayout.FloatField("skew1", skew1);
			skew2 = EditorGUILayout.FloatField("skew2", skew2);
			turnUV = EditorGUILayout.Toggle("Turn UV", turnUV);
			invertDirection = EditorGUILayout.Toggle("Invert Direction", invertDirection);
		}
		if (mode == Mode.skewX || mode == Mode.skewY || mode == Mode.skewZ || mode == Mode.billboardPlane)
			centered = EditorGUILayout.Toggle("Centered", centered);
		invertUVx = EditorGUILayout.Toggle("Invert UV x", invertUVx);
		invertUVy = EditorGUILayout.Toggle("Invert UV y", invertUVy);
		uvMax = EditorGUILayout.Vector2Field("uvMax", uvMax);
		
		if (GUILayout.Button("Generate"))
		{
			string path = EditorUtility.SaveFilePanel("Save Mesh", lastDirectory, lastName, "asset");
			if (!string.IsNullOrEmpty(path))
			{
				string fullSavePath = System.IO.Path.GetFullPath(path);
				string fullAssetsPath = System.IO.Path.GetFullPath("Assets");
				string toRemove = fullAssetsPath + System.IO.Path.DirectorySeparatorChar;
				if (fullSavePath.StartsWith(toRemove))
				{
					lastName = System.IO.Path.GetFileNameWithoutExtension(path);
					lastDirectory = System.IO.Path.GetDirectoryName(path);
					
					string relativeToAssetsPath = "Assets" + System.IO.Path.DirectorySeparatorChar + fullSavePath.Substring(toRemove.Length);
				
					Mesh mesh = new Mesh();
					List<Vector3> vertices = new List<Vector3>();
					List<Vector3> normals = new List<Vector3>();
					List<Vector2> uvs = new List<Vector2>();
					List<int> indices = new List<int>();
					
					bool useTurnUV = turnUV;
					if (mode == Mode.skewX)
						useTurnUV = !useTurnUV;
				
					if (mode == Mode.skewX || mode == Mode.skewY || mode == Mode.skewZ)
					{
						for (int i = 0; i <= steps; i++)
						{
							float k = (float)i / steps;
							Vector3 v = Vector3.zero;
							Vector2 uv = Vector2.zero;
							
							if (centered)
							{
								if (mode == Mode.skewX)
								{
									v.z -= size.x / 2;
									v.x -= (size.y + skew1) / 2;
								}
								else if (mode == Mode.skewZ)
								{
									v.x -= size.x / 2;
									v.z -= (size.y + skew1) / 2;
								}
								else if (mode == Mode.skewY)
								{
									v.x -= size.x / 2;
									v.y -= (size.y + skew1) / 2;
								}
							}
					
							// first vertex
							float kMove = invertDirection ? -k : k;
							if (mode == Mode.skewX)
								v.z += kMove * size.x;
							else
								v.x += kMove * size.x;
							float increment1 = curve1.Evaluate(k) * skew1;
							float increment2 = curve2.Evaluate(k) * skew2;
							if (mode == Mode.skewX)
							{
								v.x += increment1;
								v.y += increment2;
							}
							else if (mode == Mode.skewZ)
							{
								v.z += increment1;
								v.y += increment2;
							}
							else if (mode == Mode.skewY)
							{
								v.y += increment1;
								v.z += increment2;
							}
							vertices.Add(v);
						
							// first vertex uv
							if (useTurnUV)
							{
								uv.x = 0f;
								uv.y = k * uvMax.y;
								if (invertDirection)
									uv.x = uvMax.x - uv.x;
							}
							else
							{
								uv.x = k * uvMax.x;
								uv.y = 0f;
								if (invertDirection)
									uv.x = uvMax.x - uv.x;
							}
							if (invertUVx)
								uv.x = uvMax.x - uv.x;
							if (invertUVy)
								uv.y = uvMax.y - uv.y;
							uvs.Add(uv);
						
							// second vertex
							if (mode == Mode.skewX)
								v.x += size.y;
							else if (mode == Mode.skewZ)
								v.z += size.y;
							else if (mode == Mode.skewY)
								v.y += size.y;
							vertices.Add(v);
						
							// second vertex uv
							if (useTurnUV)
								uv.x = uvMax.x;
							else
								uv.y = uvMax.y;
							if (invertUVx)
								uv.x = uvMax.x - uv.x;
							if (invertUVy)
								uv.y = uvMax.y - uv.y;
							uvs.Add(uv);
						}
				
						for (int i = 0; i < steps; i++)
						{
							int j = i * 2;
							if (mode == Mode.skewX)
							{
								indices.Add(j + 0);
								indices.Add(j + 2);
								indices.Add(j + 1);
								indices.Add(j + 3);
								indices.Add(j + 1);
								indices.Add(j + 2);
							}
							else
							{
								indices.Add(j + 0);
								indices.Add(j + 1);
								indices.Add(j + 2);
								indices.Add(j + 3);
								indices.Add(j + 2);
								indices.Add(j + 1);
							}
						}
						if (invertDirection)
							InvertIndices(indices);
						
						while (normals.Count < vertices.Count)
							normals.Add(mode == Mode.skewZ ? Vector3.up : Vector3.forward);
					}
					else if (mode == Mode.billboardPlane)
					{
						Matrix4x4 m;
						if (centered)
							m = Matrix4x4.TRS(new Vector3(-size.x / 2, -size.y / 2, 0), Quaternion.identity, new Vector3(size.x, size.y, 1));
						else
							m = Matrix4x4.TRS(new Vector3(-size.x / 2, 0, 0), Quaternion.identity, new Vector3(size.x, size.y, 1));
						vertices.Add(m.MultiplyPoint(new Vector3(0, 0, 0)));
						uvs.Add(new Vector2(0, 0));
						vertices.Add(m.MultiplyPoint(new Vector3(1, 0, 0)));
						uvs.Add(new Vector2(uvMax.x, 0));
						vertices.Add(m.MultiplyPoint(new Vector3(0, 1, 0)));
						uvs.Add(new Vector2(0, uvMax.y));
						vertices.Add(m.MultiplyPoint(new Vector3(1, 1, 0)));
						uvs.Add(new Vector2(uvMax.x, uvMax.y));
						
						// invert uv
						for (int i = 0; i < uvs.Count; i++)
						{
							Vector2 uv = uvs[i];
							if (invertUVx)
								uv.x = uvMax.x - uv.x;
							if (invertUVy)
								uv.y = uvMax.y - uv.y;
							uvs[i] = uv;
						}
						
						while (normals.Count < vertices.Count)
							normals.Add(Vector3.forward);
						
						indices.Add(0);
						indices.Add(2);
						indices.Add(1);
						indices.Add(3);
						indices.Add(1);
						indices.Add(2);
					}
					else if (mode == Mode.floorPlane)
					{
						Matrix4x4 m = Matrix4x4.TRS(new Vector3(-size.x / 2, 0, -size.y / 2), Quaternion.identity, new Vector3(size.x, 1, size.y));
						vertices.Add(m.MultiplyPoint(new Vector3(0, 0, 0)));
						uvs.Add(new Vector2(0, 0));
						vertices.Add(m.MultiplyPoint(new Vector3(1, 0, 0)));
						uvs.Add(new Vector2(uvMax.x, 0));
						vertices.Add(m.MultiplyPoint(new Vector3(0, 0, 1)));
						uvs.Add(new Vector2(0, uvMax.y));
						vertices.Add(m.MultiplyPoint(new Vector3(1, 0, 1)));
						uvs.Add(new Vector2(uvMax.x, uvMax.y));
						
						// invert uv
						for (int i = 0; i < uvs.Count; i++)
						{
							Vector2 uv = uvs[i];
							if (invertUVx)
								uv.x = uvMax.x - uv.x;
							if (invertUVy)
								uv.y = uvMax.y - uv.y;
							uvs[i] = uv;
						}
						
						while (normals.Count < vertices.Count)
							normals.Add(Vector3.up);
						
						indices.Add(0);
						indices.Add(2);
						indices.Add(1);
						indices.Add(3);
						indices.Add(1);
						indices.Add(2);
					}
				
					mesh.vertices = vertices.ToArray();
					mesh.normals = normals.ToArray();
					mesh.uv = uvs.ToArray();
					mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
					
					SaveMesh(mesh, relativeToAssetsPath);
					
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
					EditorUtility.DisplayDialog("Mesh Generation", "Mesh generated at: " + path, "Ok");
				}
				else
				{
					EditorUtility.DisplayDialog("Mesh Generation", "Can't save outside Assets folder", "Ok");
				}
			}
		}
	}
	
	private static void InvertIndices(List<int> indices)
	{
		for (int i = 0; i < indices.Count; i += 3)
		{
			int temp = indices[i + 0];
			indices[i + 0] = indices[i + 1];
			indices[i + 1] = temp;
		}
	}
	
	private static void SaveMesh(Mesh mesh, string path)
	{
		Mesh existingMesh = LoadAtPath<Mesh>(path);
		if (existingMesh)
		{
			existingMesh.Clear();
			EditorUtility.CopySerialized(mesh, existingMesh);
			Mesh.DestroyImmediate(mesh, false);
		}
		else
		{
			AssetDatabase.CreateAsset(mesh, path);
		}
	}
	
	private static T LoadAtPath<T>(string path) where T : Object
	{
		Object[] os = AssetDatabase.LoadAllAssetsAtPath(path);
		for (int i = 0; i < os.Length; i++)
		{
			Object o = os[i];
			if (o is T)
				return (T)o;
		}
		return null;
	}	
	
}
