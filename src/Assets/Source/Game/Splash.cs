using UnityEngine;
using System.Collections;

// The splash screen. It displays a texture, and after a while, it loads another scene.
public class Splash : AutoStyles
{
	[System.Serializable]
	public class SplashImage
	{
		public Vector2 position;
		public Vector2 size;
		public TextAnchor anchor;
		public Texture texture;
		public ScaleMode scaleMode;
		
		public void Draw()
		{
			if (texture)
			{
				Rect r = RectEx.Build0(position.x, position.y, size.x, size.y, anchor);
				GUI.DrawTexture(r, texture, scaleMode);
			}
		}
	}
	
	// the name of the scene that will be loaded
	public string levelToLoad;
	
	// the rendered images
	public SplashImage[] images;

	// demo stuff
	public bool showDemoText;
	public RectEx.Position demoPosition;
	public string demoText = "This is a demo.";
	public GUIStyle demoTextStyle;

	// the loading text
	public Vector2 textPosition;
	public Vector2 textRectSize;
	public string textStart = "Tap to Continue";
	public string textLoading = "Loading...";
	public TextAnchor textAnchor = TextAnchor.MiddleCenter;
	public GUIStyle textStyle;
	
	// the policy button that opens the dialog
	public RectEx.Position policyButtonPosition;
	public string policyButtonText = "POLICY";
	public GUIStyle policyButtonStyle;
	
	// dialog variables
	public GUISkin skin;
	public string dialogTitleText = "Title";
	public RectEx.Position dialogPosition;
	private Rect dialogRect;
	public GUIStyle dialogWindowStyle;
	public GUIStyle dialogTitleStyle;
	public RectEx.Position dialogTitlePosition;
	public TextAsset[] dialogContentTexts;
	public RectEx.Position dialogContentPosition;
	public GUIStyle dialogContentStyle;
	public RectEx.Position linkPosition;
	public GUIStyle linkStyle;
	public string linkText = "HERE";
	public string linkURL = "www.tipitap.com/privacy";
	public RectEx.Position closeButtonPosition;
	public GUIStyle closeButtonStyle;
	private string[] dialogContentTextCaches;
	
	// scroll variables	
	public RectEx.Position scrollPosition;
	public GUIStyle scrollStyle;
	public float scrollThumbSize;
	public GUIStyle scrollThumbStyle;
	private Vector2 scrollPos;
	private Vector2 oldMousePosition;
	
	// state variables
	public float delay = 5f;
	public bool tapToSkip = true;
	private bool skipRequest;
	private bool loading;
	private bool showDialog;
	private GUIStyle emptyStyle;
	
	void Start()
	{
		// Disable screen dimming
		// http://docs.unity3d.com/Documentation/ScriptReference/Screen-sleepTimeout.html
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		// On Android fullscreen controls the SYSTEM_UI_FLAG_LOW_PROFILE flag to View.setSystemUiVisibility(), on devices running Honeycomb (OS 3.0 / API 11) or later.
		// http://forum.unity3d.com/threads/124744-Fullscreen-Galaxy-Nexus-how-to-disable-the-system-navigation-softkeys
		// http://docs.unity3d.com/Documentation/ScriptReference/Screen-sleepTimeout.html
		Screen.fullScreen = true;

		emptyStyle = new GUIStyle();
		StartCoroutine(StartRoutine());
		this.useGUILayout = true; // required for windows
		dialogContentTextCaches = new string[dialogContentTexts.Length];
		for (int i = 0; i < dialogContentTexts.Length; i++)
			dialogContentTextCaches[i] = dialogContentTexts[i].text;
	}
	
	IEnumerator StartRoutine()
	{
		yield return new WaitForEndOfFrame();
		
		// a pause before loading...
		if (delay > 0f)
		{
			float endTime = Time.realtimeSinceStartup + delay;
			while (!skipRequest && (delay != 0f && Time.realtimeSinceStartup < endTime))
				yield return new WaitForEndOfFrame();
		}
		// no pause, wait for touch
		else
		{
			while (!skipRequest)
				yield return new WaitForEndOfFrame();
		}
		loading = true;
		yield return new WaitForEndOfFrame();
		if (!string.IsNullOrEmpty(levelToLoad))
			Application.LoadLevel(levelToLoad);
		loading = false;
		yield return new WaitForEndOfFrame();
	}

	public static bool BackKeyDown()
	{
		return Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
	}

	void OnGUI()
	{
		GUI.skin = skin;

		if (BackKeyDown())
		{
			Application.Quit();
		}
		
		// the background images (the actual splash texture)
		foreach (SplashImage image in images)
			image.Draw();
		
		// the loading text
		if (!showDialog)
			Utils.LabelShadow(RectEx.Build0(textPosition.x, textPosition.y, textRectSize.x, textRectSize.y, textAnchor), loading ? textLoading : textStart, textStyle);
		
		// the privacy button
		if (GUI.Button(policyButtonPosition.rect, policyButtonText, policyButtonStyle))
			showDialog = true;
		
		// the privacy window
		if (showDialog)
		{
			dialogRect = RectEx.Build0(dialogPosition);
			GUI.Window(1, dialogRect, DoDialogGUI, GUIContent.none, dialogWindowStyle);
		}

		// The demo label
		if (showDemoText)
		{
			GUI.Label(demoPosition.rect, demoText, demoTextStyle);
		}

		// clicking anywhere on the screen will begin the loading of the game
		if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, emptyStyle) && !showDialog)
			skipRequest = true;
	}

	void DoDialogGUI(int windowId)
	{
		GUI.skin = skin;
		
		Rect r = dialogRect;
		r.x = 0;
		r.y = 0;
		GUI.Label(dialogTitlePosition.SubRect0(r), dialogTitleText, dialogTitleStyle);
		//GUI.Label(dialogContentPosition.SubRect0(r), dialogContentTextCache, dialogContentStyle);
		//if (GUI.Button(linkPosition.SubRect0(r), linkText, linkStyle))
		//	Application.OpenURL(linkURL);

		Rect r2 = dialogContentPosition.SubRect0(r);
		float contentHeight = 0;
		float[] heights = new float[dialogContentTextCaches.Length];
		
		for (int i = 0; i < dialogContentTextCaches.Length; i++)
		{
			heights[i] = dialogContentStyle.CalcHeight(new GUIContent(dialogContentTextCaches[i]), r2.width);
			contentHeight += heights[i];
		}
		contentHeight -= r2.height * 0.75f;

		Rect rScroll = scrollPosition.SubRect0(r);
		GUI.Box(rScroll, GUIContent.none, scrollStyle);
		
		Rect rScrollThumb = RectEx.SubRectSquare0(rScroll, 0.5f, -scrollPos.y / contentHeight, scrollThumbSize, scrollThumbStyle, TextAnchor.MiddleCenter);
		Event e = new Event(Event.current);
		GUI.Button(rScroll, GUIContent.none, GUIStyle.none);
		GUI.Button(rScrollThumb, GUIContent.none, scrollThumbStyle);
		if (Event.current.type == EventType.Used && e.isMouse)
		{
			if (e.type == EventType.MouseDown)
				oldMousePosition = e.mousePosition;
			scrollPos.y = Mathf.Clamp((e.mousePosition.y - rScroll.y) * -contentHeight / rScroll.height, -contentHeight, 0);
			oldMousePosition = e.mousePosition;
		}
		
		e = new Event(Event.current);
		GUI.Button(r2, GUIContent.none, GUIStyle.none);
		if (Event.current.type == EventType.Used && e.isMouse)
		{
			if (e.type == EventType.MouseDown)
				oldMousePosition = e.mousePosition;
			Vector2 delta = e.mousePosition - oldMousePosition;
			scrollPos += delta;
			scrollPos.x = 0;
			scrollPos.y = Mathf.Clamp(scrollPos.y, -contentHeight, 0);
			oldMousePosition = e.mousePosition;
		}
		
		GUI.BeginGroup(r2);
		Rect r3 = r2;
		r3.x = 0;
		r3.y = 0;
		r3.x += scrollPos.x;
		r3.y += scrollPos.y;
		for (int i = 0; i < dialogContentTextCaches.Length; i++)
		{
			r3.height = heights[i];
			GUI.Label(r3, dialogContentTextCaches[i], dialogContentStyle);
			r3.y += r3.height;
		}
		GUI.EndGroup();
		
		if (GUI.Button(closeButtonPosition.SubRectSquare0(r), GUIContent.none, closeButtonStyle))
			showDialog = false;
	}
	
}
