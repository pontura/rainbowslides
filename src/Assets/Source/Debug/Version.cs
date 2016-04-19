using UnityEngine;
using System.Collections;

// A simple class to keep track of the game version.
public class Version : MonoBehaviour
{
	public string version = "0.1";
	public GUIStyle style;
	
	public static Version instance;
	
	void OnEnable()
	{
		instance = this;
	}
	
	public void DoGUI()
	{
		Rect r = new Rect(Screen.width - 80, Screen.height - 40, 70, 30);
		string text = "v" + version;
		if (Dev.Allowed)
			text += " (dev)";
		Utils.LabelShadow(r, text, style);
	}

}
