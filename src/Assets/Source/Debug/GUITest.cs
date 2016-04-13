using UnityEngine;
using System.Collections;

// A simple class to test a GUI style.
public class GUITest : MonoBehaviour
{
	public GUIStyle style;
	public Rect rect;
	public string text = "";

	void OnGUI()
	{
		GUI.Button(rect, text, style);
	}
}
