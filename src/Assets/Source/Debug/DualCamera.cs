using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
public class DualCamera : MonoBehaviour
{
	[System.Serializable]
	public class DualCameraInfo
	{
		public Camera camera;
		public bool targetTV = false;
		
		// if true, the camera will be enabled only if there are more than one display available.
		public bool autoEnable = false;
	}
	
	public List<DualCameraInfo> cameras;
	public Camera guiTarget;
	public bool autoEnable = true;
	public int customRenderWidthHigh = 1920;
	public int customRenderHeightHigh = 1080;
	public int customRenderWidthLow = 320;
	public int customRenderHeightLow = 240;
	public bool showFPS = false;
	public bool emulateTvOnEditor;
	public int emulateTvWidth = 640;
	public int emulateTvHeight = 480;
	
	public bool emulateTV
	{
		get
		{
			return emulateTvOnEditor && Application.isEditor;
		}
	}
	
	public bool showGUI = true;
	public GUISkin skin;
	public static DualCamera instance;
	private RenderTexture rt;
	private Texture2D t2d;
	public bool realEnabled = true;
	public Color emulateColor = Color.white;
	private bool lastSetActive = false;
 
	public bool Available
	{
		get
		{
			if (!realEnabled)
				return false;
			return enabled && (Display.displays.Length > 1 || emulateTV);
		}
	}
 
	public bool Active
	{
		get
		{
			return lastSetActive;
		}
		set
		{
			if (!realEnabled)
				return;
			Display ipadDisplay = Display.displays[0];
			if (!value || !Available)
			{
				foreach (DualCameraInfo c in cameras)
				{
					c.camera.SetTargetBuffers(ipadDisplay.colorBuffer, ipadDisplay.depthBuffer);
					if (c.autoEnable)
						c.camera.enabled = false;

					// make sure the aspect is recalculated
					if (lastSetActive != value)
						c.camera.ResetAspect();
				}
			}
			else
			{
				if (emulateTV)
				{
					if (rt == null)
						rt = new RenderTexture(emulateTvWidth, emulateTvHeight, 16, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
					
					foreach (DualCameraInfo c in cameras)
					{
						if (c.targetTV)
							c.camera.targetTexture = rt;
						if (c.autoEnable)
							c.camera.enabled = true;
						// make sure the aspect is recalculated
						if (lastSetActive != value)
							c.camera.ResetAspect();
					}
				}
				else
				{
					Display tvDisplay = Display.displays[1];
 
					foreach (DualCameraInfo c in cameras)
					{
						// tv (secondDisplay)
						if (c.targetTV)
							c.camera.SetTargetBuffers(tvDisplay.colorBuffer, tvDisplay.depthBuffer);
						// ipad (Display.main)
						else
							c.camera.SetTargetBuffers(ipadDisplay.colorBuffer, ipadDisplay.depthBuffer);
						if (c.autoEnable)
							c.camera.enabled = true;
						// make sure the aspect is recalculated
						if (lastSetActive != value)
							c.camera.ResetAspect();
					}
				}
			}
			
			lastSetActive = value;
		}
	}
	
	void Awake()
	{
		instance = this;
	}
	
	void OnDestroy()
	{
		if (rt)
		{
			rt.Release();
			rt.DiscardContents();
			RenderTexture.Destroy(rt);
			rt = null;
		}
		if (t2d)
		{
			Texture2D.Destroy(t2d);
			t2d = null;
		}
	}
 
	void Start()
	{ 
		this.useGUILayout = false;
		if (emulateTV)
			StartCoroutine(CaptureRoutine());
	}
 
	void Update()
	{
		if (Available)
		{
			if (autoEnable)
			{
				Active = true;
			}
		}
		else
		{
			Active = false;
		}
	}
	
	void LateUpdate()
	{
		// the gui is always displayed on the last rendered main camera. It must have the biggest depth.
		if (guiTarget && Available)
			guiTarget.SetTargetBuffers(Display.displays[0].colorBuffer, Display.displays[0].depthBuffer);
	}
	
	IEnumerator CaptureRoutine()
	{
		while (true)
		{
			if (rt)
			{
				yield return new WaitForEndOfFrame();
				if (t2d == null)
					t2d = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, true);
				RenderTexture prev = RenderTexture.active;
				RenderTexture.active = rt;
				t2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
				t2d.Apply();
				RenderTexture.active = prev;
			}
			else
				yield return new WaitForEndOfFrame();
		}
	}
	
	public void OnTestCameraRender()
	{

	}
	
	public void DoGUI()
	{
		if (t2d)
		{
			Color oldColor = GUI.color;
			GUI.color = emulateColor;
			GUI.DrawTexture(new Rect(50, 50, DualCamera.GetWidth(0) * 0.6f - 50, DualCamera.GetHeight(0) * 0.6f - 50), t2d, ScaleMode.ScaleToFit);
			GUI.color = oldColor;
		}
		
		if (!showGUI)
			return;
		
		GUI.skin = skin;

		Rect r = new Rect(10, 10, Screen.width - 20, 30);
		float sy = r.height + 10;
		
		GUI.Label(r, "Dual Camera Test " + (emulateTV ? "EMULATING!" : ""));
		r.y += sy;
		if (GUI.Button(r, "Hide GUI"))
			showGUI = false;
		r.y += sy;
		
		if (!realEnabled)
		{
			if (GUI.Button(r, "ENABLE DUAL CAMERA SCRIPT"))
			{
				realEnabled = true;
			}
			return;
		}

		if (GUI.Button(r, "DISABLE DUAL CAMERA SCRIPT"))
			realEnabled = false;
		r.y += sy;
		
		if (guiTarget != null)
		{
			Rect[] r3 = RectEx.SplitHorizontally(r, 2f, 1, 1, 1, 1, 1, 1);
			GUI.Label(r3[0], "Depth: " + guiTarget.depth);
			if (GUI.Button(r3[1], "- Depth"))
				guiTarget.depth -= 5;
			if (GUI.Button(r3[2], "+ Depth"))
				guiTarget.depth += 5;
			if (GUI.Button(r3[3], "Enabled " + guiTarget.enabled))
				guiTarget.enabled = !guiTarget.enabled;
			if (GUI.Button(r3[4], "Set Display 0"))
				guiTarget.SetTargetBuffers(Display.displays[0].colorBuffer, Display.displays[0].depthBuffer);
			if (GUI.Button(r3[5], "Set Display 1"))
				guiTarget.SetTargetBuffers(Display.displays[1].colorBuffer, Display.displays[1].depthBuffer);
			r.y += sy;
		}
		
		GUI.Label(r, "                                 Main camera: " + (Camera.main ? Camera.main.name : "null"));
		r.y += sy;
		
		Rect[] r2 = RectEx.SplitHorizontally(r, 2f, 1, 1, 1);
		if (GUI.Button(r2[0], "AutoEnable: " + autoEnable))
			autoEnable = !autoEnable;
		if (GUI.Button(r2[1], "Active = FALSE"))
			Active = false;
		if (GUI.Button(r2[2], "Active = TRUE"))
			Active = true;
		r.y += sy;
		
		
		GUI.Label(r, "Display.displays: " + Display.displays.Length);
		r.y += sy;
		
		for (int i = 0; i < Display.displays.Length; i++)
		{
			Display d = Display.displays[i];
			GUI.Label(r, string.Format("id: {0} renderingRect: {1}x{2} systemRect: {3}x{4}", i.ToString(), d.renderingWidth, d.renderingHeight, d.systemWidth, d.systemHeight));
			r.y += sy;
			
			r2 = RectEx.SplitHorizontally(r, 2f, 1, 1, 1, 1);
			
			if (GUI.Button(r2[0], "Set System Res"))
				d.SetRenderingResolution(d.systemWidth, d.systemHeight);
			if (GUI.Button(r2[1], "Set Rendering Res"))
				d.SetRenderingResolution(d.renderingWidth, d.renderingHeight);
			if (GUI.Button(r2[2], "Set Custom Res Low"))
				d.SetRenderingResolution(customRenderWidthLow, customRenderHeightLow);
			if (GUI.Button(r2[3], "Set Custom Res High"))
				d.SetRenderingResolution(customRenderWidthHigh, customRenderHeightHigh);
			r.y += sy;
		}
		
		if (showFPS && FPSCounter.instance)
			FPSCounter.instance.DoGUI();
	}
	
	public static int GetWidth(int displayId)
	{
		if (instance.emulateTV && displayId == 1)
			return instance.rt.width;
		return Display.displays[displayId % Display.displays.Length].renderingWidth;
	}
	
	public static int GetHeight(int displayId)
	{
		if (instance.emulateTV && displayId == 1)
			return instance.rt.height;
		return Display.displays[displayId % Display.displays.Length].renderingHeight;
	}
	
	public static bool AirPlayCompatible
	{
		get
		{
			return Application.platform == RuntimePlatform.IPhonePlayer || (Application.isEditor && instance.emulateTvOnEditor);
		}
	}
}
