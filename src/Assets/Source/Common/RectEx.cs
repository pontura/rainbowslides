using UnityEngine;
using System.Collections;

// Handy class to calculate rects.
public class RectEx : MonoBehaviour
{
	[System.Serializable]
	public class Position
	{
		public Rect rect0;
		public TextAnchor anchor = TextAnchor.MiddleCenter;
		
		public Rect rect
		{
			get
			{
				return RectEx.Build0(this);
			}
		}
		
		public Rect SubRect0(Rect root)
		{
			return RectEx.SubRect0(root, rect0, anchor);
		}
		
		public Rect SubRectSquare0(Rect root)
		{
			return RectEx.SubRectSquare0(root, rect0.x, rect0.y, rect0.width, rect0.height, anchor);
		}
	}
	
	public static Rect Build0(Position r0)
	{
		return Build0(r0.rect0.x, r0.rect0.y, r0.rect0.width, r0.rect0.height, r0.anchor);
	}
	
	public static Rect Build0(Rect r0, TextAnchor anchor)
	{
		return Build0(r0.x, r0.y, r0.width, r0.height, anchor);
	}
	
	public static Rect Build0(float x0, float y0, float w0, float h0, TextAnchor anchor)
	{
		Rect r = new Rect(x0, y0, w0, h0);
		r.x *= Screen.width;
		r.y *= Screen.height;
		r.width *= Screen.width;
		r.height *= Screen.height;
		return Utils.ApplyAnchor(anchor, r);
	}
	
	public static Rect BuildSquare0(float x0, float y0, float wh0, float aspectRatio, TextAnchor anchor)
	{
		Rect r = new Rect(x0, y0, 0, 0);
		r.x *= Screen.width;
		r.y *= Screen.height;
		float wh = Mathf.Min(wh0 * Screen.width, wh0 * Screen.height);
		r.width = wh;
		r.height = wh / aspectRatio;
		return Utils.ApplyAnchor(anchor, r);
	}
	
	public static Rect BuildSquare0(float x0, float y0, float wh0, GUIStyle styleAspect, TextAnchor anchor)
	{
		return BuildSquare0(x0, y0, wh0, styleAspect.normal.background, anchor);
	}
	
	public static Rect BuildSquare0(float x0, float y0, float wh0, Texture2D textureAspect, TextAnchor anchor)
	{
		float aspectRatio = textureAspect ? (float)textureAspect.width / textureAspect.height : 1f;
		return BuildSquare0(x0, y0, wh0, aspectRatio, anchor);
	}
	
	public static Rect Build(float x, float y, float width, float height, TextAnchor anchor)
	{
		return Utils.ApplyAnchor(anchor, new Rect(x, y, width, height));
	}
	
	public static Rect Expand(Rect r, float s)
	{
		return new Rect(r.x - s, r.y - s, r.width + s * 2, r.height + s * 2);
	}
	
	public static Rect Expand0(Rect r, float s)
	{
		return new Rect(r.x - s * r.width, r.y - s * r.height, r.width + s * r.width * 2, r.height + s * r.height * 2);
	}

	public static Rect ExpandMax0(Rect r, float s)
	{
		float wh = Mathf.Max(s * r.width, s * r.height);
		return new Rect(r.x - wh, r.y - wh, r.width + wh * 2, r.height + wh * 2);
	}

	public static Rect Expand(Rect r, float sx, float sy)
	{
		return new Rect(r.x - sx, r.y - sy, r.width + sx * 2, r.height + sy * 2);
	}
	
	public static Rect Expand0(Rect r, float sx, float sy)
	{
		return new Rect(r.x - sx * r.width, r.y - sy * r.height, r.width + sx * r.width * 2, r.height + sy * r.height * 2);
	}
	
	public static Rect Grid(Rect r, int x, int y, float sx, float sy)
	{
		return new Rect(r.x + (r.width + sx) * x, r.y + (r.height + sy) * y, r.width, r.height);
	}
	
	public static Rect[] SplitHorizontally(Rect r, int count, float margin)
	{
		Rect[] ret = new Rect[count];
		int separations = Mathf.Max(count - 1, 0);
		float rWidth = (r.width - separations * margin) / count;
		for (int i = 0; i < count; i++)
			ret[i] = new Rect(r.x + (rWidth + margin) * i, r.y, rWidth, r.height);
		return ret;
	}
	
	public static Rect[] SplitHorizontally(Rect r, float margin, params float[] points)
	{
		Rect[] ret = new Rect[points.Length];
		int separations = Mathf.Max(points.Length - 1, 0);
		float totalPoints = 0;
		for (int i = 0; i < points.Length; i++)
			totalPoints += points[i];
		float currentX = r.x;
		float widthToShare = r.width - separations * margin;
		for (int i = 0; i < points.Length; i++)
		{
			float w = widthToShare * (points[i] / totalPoints);
			ret[i] = new Rect(currentX, r.y, w, r.height);
			currentX += margin + w;
		}
		return ret;
	}
	
	public static Rect FitInsideScreen(Rect r)
	{
		if (r.x + r.width >= Screen.width)
			r.x = Screen.width - r.width - 1;
		if (r.x < 0)
			r.x = 0;
		if (r.y + r.height >= Screen.height)
			r.y = Screen.height - r.height - 1;
		if (r.y < 0)
			r.y = 0;
		return r;
	}
	
	public static Rect SubRect(Rect original, float x0, float y0, float width, float height, TextAnchor anchor)
	{
		Rect r = new Rect(original.x + original.width * x0, original.y + original.height * y0, width, height);
		return Utils.ApplyAnchor(anchor, r);
	}
	
	public static Rect SubRect0(Rect original, float x0, float y0, float w0, float h0, TextAnchor anchor)
	{
		Rect r = new Rect(original.x + original.width * x0, original.y + original.height * y0, original.width * w0, original.height * h0 );
		return Utils.ApplyAnchor(anchor, r);
	}
	
	public static Rect SubRect0(Rect original, Rect r0, TextAnchor anchor)
	{
		return SubRect0(original, r0.x, r0.y, r0.width, r0.height, anchor);
	}

	public static Rect SubRect0(Rect original, float x0, float y0, float w0, float h0)
	{
		return new Rect(original.x + original.width * x0, original.y + original.height * y0, original.width * w0, original.height * h0);
	}
		
	public static Rect SubRectSquare0(Rect original, float x0, float y0, float wh0, float aspectRatio, TextAnchor anchor)
	{
		float wh = Mathf.Min(wh0 * original.width, wh0 * original.height);
		Rect r = new Rect(original.x + original.width * x0, original.y + original.height * y0, wh, wh / aspectRatio);
		return Utils.ApplyAnchor(anchor, r);
	}
	
	public static Rect SubRectSquare0(Rect original, float x0, float y0, float wh0, GUIStyle styleAspect, TextAnchor anchor)
	{
		return SubRectSquare0(original, x0, y0, wh0, styleAspect.normal.background, anchor);
	}
	
	public static Rect SubRectSquare0(Rect original, float x0, float y0, float wh0, Texture2D textureAspect, TextAnchor anchor)
	{
		float aspectRatio = textureAspect ? textureAspect.width / textureAspect.height : 1f;
		return SubRectSquare0(original, x0, y0, wh0, aspectRatio, anchor);
	}
}