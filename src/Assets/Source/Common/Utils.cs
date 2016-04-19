using UnityEngine;
using System.Collections;

// This class contains handy static functions for diverse purposes.
public class Utils : MonoBehaviour
{
		
	public static T FindInParents<T>(Transform t) where T : Component
	{
		T result;
		while (t != null)
		{
			result = t.GetComponent<T>();
			if (result)
			{
				return result;
			}
			t = t.parent;
		}
		return null;
	}
	
	public static void LabelShadow(Rect rect, string text, GUIStyle style)
	{
		LabelShadow(rect, text, GUI.color, Color.black, style);
	}
	
	public static void LabelShadow(Rect rect, string text, Color frontColor, Color shadowColor, GUIStyle style)
	{
		Rect rect2 = rect;
		rect2.x += 1;
		rect2.y += 1;
		Color oldColor = GUI.color;
		GUI.color = shadowColor;
		GUI.Label(rect2, text, style);
		GUI.color = frontColor;
		GUI.Label(rect, text, style);
		GUI.color = oldColor;
	}

	public static Rect ApplyAnchor(TextAnchor anchor, Rect rect)
	{
		switch (anchor)
		{
		case TextAnchor.LowerCenter:
			rect.x -= rect.width * 0.5f;
			break;
		case TextAnchor.LowerLeft:
			rect.x -= rect.width;
			break;
		case TextAnchor.LowerRight:
			break;

		case TextAnchor.MiddleCenter:
			rect.x -= rect.width * 0.5f;
			rect.y -= rect.height * 0.5f;
			break;
		case TextAnchor.MiddleLeft:
			rect.x -= rect.width;
			rect.y -= rect.height * 0.5f;
			break;
		case TextAnchor.MiddleRight:
			rect.y -= rect.height * 0.5f;
			break;

		case TextAnchor.UpperCenter:
			rect.x -= rect.width * 0.5f;
			rect.y -= rect.height;
			break;
		case TextAnchor.UpperLeft:
			rect.x -= rect.width;
			rect.y -= rect.height;
			break;
		case TextAnchor.UpperRight:
			rect.y -= rect.height;
			break;
		}
		return rect;
	}
	
	public static void Fill(Rect rect, Color color)
	{
		Color c = GUI.color;
		GUI.color = color;
		GUI.Box(rect, GUIContent.none, "fill");
		GUI.color = c;
	}	

	public static void Fill(Rect rect, Color color, Texture2D texture, float s)
	{
		Color c = GUI.color;
		GUI.color = color;
		Rect uv = new Rect(0, 0, (float)rect.width / (s * texture.width), (float)rect.height / (s * texture.height));
		GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
		GUI.color = c;
	}	
	
	public static T NextEnumValueInt<T>(T currentValue, int addIndex)
	{
		// this only works if the enum starts at zero and don't have empty spaces
		System.Type tType = typeof(T);
		int[] array = (int[])System.Enum.GetValues(tType);
		int id = System.Array.IndexOf(array, (int)System.Convert.ChangeType(currentValue, typeof(int)));
		id = (id + array.Length + addIndex) % array.Length;
		return (T)System.Enum.ToObject(tType, id);
	}
	
}
