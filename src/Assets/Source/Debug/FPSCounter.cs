using UnityEngine;
using System.Collections;

// As the name says, this class counts frames per second.
// It also displays the computed value on the screen.
public class FPSCounter : MonoBehaviour
{
	public GUIStyle style;
	public Vector2 position = new Vector2(0.5f, 0.8f);
	
	public bool clampFPS = false;
	public int editorFPS = 0;
	
	private float startFSP;
	private float fps;
	private float fpsUpdateFrequency = 1f;
	private int frameCount;
	
	public static FPSCounter instance;
	
	void Awake()
	{
		instance = this;
	}
	
	void Start()
	{
		startFSP = Time.realtimeSinceStartup;
		fps = 0f;
		frameCount = 0;
		this.useGUILayout = false;
		if (clampFPS && editorFPS != 0 && Application.isEditor)
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = editorFPS;
		}
	}
	
	void Update()
	{
		frameCount++;
		if (Time.realtimeSinceStartup > startFSP + fpsUpdateFrequency)
		{
			float elapsed = Time.realtimeSinceStartup - startFSP;
			fps = (float)frameCount / elapsed;
			startFSP = Time.realtimeSinceStartup;
			frameCount = 0;
		}
	}
	
	public void DoGUI()
	{
		if (Event.current.type == EventType.Repaint)
		{
			Rect r = new Rect(Screen.width * position.x, Screen.height * position.y, 80, 30);
			r.x -= r.width / 2;
			Utils.LabelShadow(r, string.Format("{0:0.0} fps", fps), style);
		}
	}
	
}
