using UnityEngine;
using UnityEditor;
using System.Collections;

// This class defines a prettier inspector GUI to define LevelInfos.
[CustomEditor(typeof(LevelInfo))]
public class LevelInfoInspector : Editor
{
	public override void OnInspectorGUI()
	{
		LevelInfo level = (LevelInfo)this.target;
		EditorGUI.BeginChangeCheck();
		for (int i = 0; i < level.connections.Count; i++)
		{
			ConnectionInfo cInfo = level.connections[i];
			GUILayout.BeginHorizontal();
			cInfo.start = EditorGUILayout.IntField(cInfo.start, GUILayout.Width(50));
			cInfo.end = EditorGUILayout.IntField(cInfo.end, GUILayout.Width(50));
			cInfo.model = (GameObject)EditorGUILayout.ObjectField(cInfo.model, typeof(GameObject), false);
			GUILayout.Space(30);
			if (GUILayout.Button("-", GUILayout.Width(20)))
				level.connections.RemoveAt(i--);
			GUILayout.EndHorizontal();
		}
		if (GUILayout.Button("Add"))
			level.connections.Add(new ConnectionInfo());
		if (GUILayout.Button("Sort"))
			level.connections.Sort(ConnectionInfo.SortComparison);
		if (EditorGUI.EndChangeCheck())
		{
			EditorUtility.SetDirty(target);
			AssetDatabase.SaveAssets();
		}
	}
}
