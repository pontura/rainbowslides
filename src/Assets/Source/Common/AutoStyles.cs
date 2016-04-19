using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// Use this class as a base container of GUIStyles variables.
// It will modify the font size of each style according to the current screen size.
// This automation is required to properly auto-adjust to different device's resolutions.
public class AutoStyles : MonoBehaviour
{
	// The original font sizes should be set for a screen height of this value.
	// Fonts will preserve the original values set on inspector on devices with this screen height.
	public float fontSizeForHeight = 768f;
	
	// Use this to tune the amount of adjustemnt done to the fonts.
	// 0 means they will always keep the original value.
	public float fontSizeChange = 1f;
	
	private Dictionary<GUIStyle, GUIStyle> originalStyles = new Dictionary<GUIStyle, GUIStyle>();
	
	private int lastScreenWidth;
	private int lastScreenHeight;
	
	void Update()
	{
		// If the screen size changed, auto adjust the font sizes.
		if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
		{
			lastScreenWidth = Screen.width;
			lastScreenHeight = Screen.height;
			FitFontSizes();
		}
	}

	public void FitFontSizes()
	{
		// Initialization.
		if (originalStyles.Count == 0)
		{
			foreach (FieldInfo fi in GetType().GetFields())
			{
				if (fi.FieldType != typeof(GUIStyle))
					continue;
				GUIStyle style = (GUIStyle)fi.GetValue(this);
				originalStyles.Add(style, new GUIStyle(style));
			}
		}
		
		// Update font sizes.
		float k = ((Screen.height / fontSizeForHeight) - 1f) * fontSizeChange + 1f;
		foreach (FieldInfo fi in GetType().GetFields())
		{
			if (fi.FieldType != typeof(GUIStyle))
				continue;
			GUIStyle style = (GUIStyle)fi.GetValue(this);
			GUIStyle origianlStyle = originalStyles[style];
			if (origianlStyle.fontSize != 0)
				style.fontSize = Mathf.RoundToInt(origianlStyle.fontSize * k);
		}
	}
}
