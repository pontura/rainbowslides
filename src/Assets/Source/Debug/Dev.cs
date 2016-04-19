using UnityEngine;
using System.Collections;

// Class to help in the development process, for testing and debug on devices.
public class Dev : AutoStyles
{
	public bool demoMode;

	public GUISkin skin;
	public bool allowDev;
	public bool allowSinglePlayer = true;
	
	// If set and in debug mode, this level will be loaded at start.
	public LevelInfo hardcodedStart;
	public bool useHardcodedLevel = true;
	public bool showMenu;
	public bool fastForwardMenues;
	public bool fastForwardTime;
	public bool autoHumanMove;
	public bool cheats;
	public bool highlightMoveMode;
	public bool cheatedStart;
	private bool showDeviceInfo;
	private bool showNotifications;
	public GUIStyle buttonStyle;
	public static Dev instance;

	public static bool DemoMode
	{
		get
		{
			return instance && instance.demoMode;
		}
	}

	public static bool Allowed
	{
		get
		{
			return instance && instance.allowDev;
		}
	}
	
	public static bool ShowingMenu
	{
		get
		{
			return instance && instance.showMenu;
		}
	}
	
	public static bool AllowSinglePlayer
	{
		get
		{
			return instance && instance.allowDev && instance.allowSinglePlayer;
		}
	}
	
	public static bool UseHardcodedLevel
	{
		get
		{
			return instance && instance.allowDev && instance.useHardcodedLevel;
		}
	}
	
	// Use this to skip animations, for fast testing.
	public static bool FastForwardMenues
	{
		get
		{
			return instance && instance.allowDev && instance.fastForwardMenues;
		}
	}
	
	public static bool FastForwardTime
	{
		get
		{
			return instance && instance.allowDev && instance.fastForwardTime;
		}
	}
		
	public static bool AutoHumanMove
	{
		get
		{
			return instance && instance.allowDev && instance.autoHumanMove;
		}
	}

	public static bool Cheats
	{
		get
		{
			return instance && instance.allowDev && instance.cheats;
		}
	}
	
	public static bool HighlightMoveMode
	{
		get
		{
			return instance && instance.allowDev && instance.highlightMoveMode;
			;
		}
	}
	
	public static bool CheatedStart
	{
		get
		{
			return instance && instance.allowDev && instance.cheatedStart;
		}
	}

	void OnEnable()
	{
		if (instance)
			Debug.LogError("Dev must be a singletone!", this);
		instance = this;
	}
	
	bool Button(Rect r, string text)
	{
		return GUI.Button(r, text, buttonStyle);
	}

	public int notificationCount;

	public void DoGUI()
	{
		if (showMenu)
		{
			GUI.skin = skin;
		
			int count = showNotifications ? 10 : showDeviceInfo ? 12 : 16;
			Rect rect = new Rect(Screen.width * 0.1f, 10, Screen.width * 0.8f, (Screen.height - 20) / count);
			float sy = rect.height + 5;
		
			if (showNotifications)
			{
				if (Button(rect, "Close"))
				{
					showNotifications = false;
				}
				rect.y += sy;
#if UNITY_IPHONE
				if (Button(rect, "PresentLocalNotificationNow"))
				{
					UnityEngine.iOS.LocalNotification ln = new UnityEngine.iOS.LocalNotification();
					ln.alertBody = "ola k ase! id:" + notificationCount.ToString();
					notificationCount++;
					UnityEngine.iOS.NotificationServices.PresentLocalNotificationNow(ln);
				}
				rect.y += sy;
				if (Button(rect, "ScheduleLocalNotification"))
				{
					UnityEngine.iOS.LocalNotification ln = new UnityEngine.iOS.LocalNotification();
					ln.fireDate = System.DateTime.Now.AddSeconds(10);
					ln.alertBody = "ola k ase 10s! id:" + notificationCount.ToString();
					notificationCount++;
					UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(ln);
				}
				rect.y += sy;
				if (Button(rect, "ScheduleLocalNotification Repeat"))
				{
					UnityEngine.iOS.LocalNotification ln = new UnityEngine.iOS.LocalNotification();
					ln.fireDate = System.DateTime.Now.AddSeconds(10);
					ln.alertBody = "repeated every minute! id:" + notificationCount.ToString();
					ln.repeatInterval = UnityEngine.iOS.CalendarUnit.Minute;
					ln.soundName = "default"; // with sound
					ln.applicationIconBadgeNumber = 1; // add a number to the app icon
					notificationCount++;
					UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(ln);
				}
				rect.y += sy;
				if (Button(rect, "localNotificationCount: " + UnityEngine.iOS.NotificationServices.localNotificationCount))
				{
					UnityEngine.iOS.NotificationServices.ClearLocalNotifications();
				}
				rect.y += sy;
				if (Button(rect, "scheduledLocalNotifications: " + UnityEngine.iOS.NotificationServices.scheduledLocalNotifications.Length))
				{
					UnityEngine.iOS.NotificationServices.CancelAllLocalNotifications();
				}
				rect.y += sy;
#endif
			}
			else if (showDeviceInfo)
			{
				Button(rect, "deviceModel: " + SystemInfo.deviceModel);
				rect.y += sy;
				Button(rect, "deviceName: " + SystemInfo.deviceName);
				rect.y += sy;
				Button(rect, "deviceType: " + SystemInfo.deviceType);
				rect.y += sy;
				Button(rect, "deviceUniqueIdentifier: " + SystemInfo.deviceUniqueIdentifier);
				rect.y += sy;
				Button(rect, "operatingSystem: " + SystemInfo.operatingSystem);
				rect.y += sy;
#if UNITY_IPHONE
				Button(rect, "iPhone.generation: " + UnityEngine.iOS.Device.generation);
				rect.y += sy;
				Button(rect, "iPhone.vendorIdentifier: " + UnityEngine.iOS.Device.vendorIdentifier);
				rect.y += sy;
#endif
				Button(rect, "Screen.width/height: " + Screen.width + " / " + Screen.height);
				rect.y += sy;
				Button(rect, "Screen.dpi: " + Screen.dpi);
				rect.y += sy;
				
				if (Button(rect, "Close"))
					showDeviceInfo = false;
				rect.y += sy;
			}
			else
			{
				if (Button(rect, "Close Dev Menu"))
					showMenu = false;
				rect.y += sy;
			
				if (Button(rect, "cheats " + cheats))
					cheats = !cheats;
				rect.y += sy;

				if (Button(rect, "cheated start " + cheatedStart))
					cheatedStart = !cheatedStart;
				rect.y += sy;
			
				if (Button(rect, "Fast Forward Time " + fastForwardTime))
					fastForwardTime = !fastForwardTime;
				rect.y += sy;

				if (Button(rect, "Fast Forward Menues " + fastForwardMenues))
					fastForwardMenues = !fastForwardMenues;
				rect.y += sy;
			
				if (Button(rect, "Auto Move " + autoHumanMove))
					autoHumanMove = !autoHumanMove;
				rect.y += sy;

				int ql = QualitySettings.GetQualityLevel();
				if (Button(rect, "Quality " + QualitySettings.names[ql]))
				{
					QualitySettings.SetQualityLevel((ql + 1) % QualitySettings.names.Length, true);
				}
				rect.y += sy;
			
				string lastLoadedLevelText = "Last Loaded Level: " + (BoardInfo.instance ? BoardInfo.instance.lastLoadedLevel.name : "null board info");
				if (Button(rect, lastLoadedLevelText))
					Debug.Log(lastLoadedLevelText, BoardInfo.instance ? BoardInfo.instance.lastLoadedLevel : null);
				rect.y += sy;
			
				Rect[] splits = RectEx.SplitHorizontally(rect, 10, 1, 1);
				if (Button(splits[0], "Load PREVIOUS level"))
				{
					BoardInfo.instance.LoadNextLevel(-1);
				}
				if (Button(splits[1], "Load NEXT level"))
				{
					BoardInfo.instance.LoadNextLevel(1);
				}
				rect.y += sy;
			
				AccelerometerCamera acel = AccelerometerCamera.instance;
				if (Button(rect, "Acceleration Camera " + Input.acceleration + " " + acel.enabled))
					acel.enabled = !acel.enabled;
				rect.y += sy;
			
				if (Button(rect, "Dual Camera"))
				{
					DualCamera.instance.showGUI = !DualCamera.instance.showGUI;
					showMenu = false;
				}
				rect.y += sy;

				if (Button(rect, "Show Device Info"))
				{
					showDeviceInfo = true;
				}
				rect.y += sy;

				if (Button(rect, "Notifications"))
				{
					showNotifications = true;
				}
				rect.y += sy;
			}
		}
	}

}
