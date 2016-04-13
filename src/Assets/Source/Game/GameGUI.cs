using UnityEngine;
using System.Collections;

public enum GUIState
{
	splash,
	gameSetup,
	ingame,
	pause,
}

// The menues and main GUI code is implemented here.
public class GameGUI : MonoBehaviour
{
	public static GameGUI instance;
	public GUISkin skin;
	private GUIState _state = GUIState.gameSetup;

	// Camera used on controls when Dual Camera is available.
	public Camera controlsCamera;
	
	// Camera used on tv when Dual Camera is available.
	public Camera gameSetupTvCamera;
	
	// Camera used to display the spinner on the TV.
	public Camera spinnerTvCamera;
	
	// Camera used to render only the Spin Again window on the TV.
	public Camera spinAgainTvCamera;
	
	// The maximum player name characters
	public int maxPlayerNameLength = 10;
	private bool showRestartConfirmation;
	private bool showAirPlayDialog;
	
	private bool showingAnyDialog
	{
		get
		{
			return showRestartConfirmation || showAirPlayDialog;
		}
	}
	
	void OnEnable()
	{
		if (instance)
			Debug.LogError("Game must be a singletone", this);
		instance = this;
	}
		
	void Start()
	{
		this.useGUILayout = false;
		Store.Load();
		
		// make sure there is no vSync, and set max fps rate to 60... no matter if it consumes more battery.
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;
	}
			
	private StaticAssets assets
	{
		get
		{
			return StaticAssets.instance;
		}
	}
	
	private Styles styles
	{
		get
		{
			return Styles.instance;
		}
	}
	
	public static GUIState State
	{
		get
		{
			return instance._state;
		}
		set
		{
			if (instance._state != value)
			{
				if (instance._state == GUIState.gameSetup)
					foreach (Player p in Player.all)
						p.OnGameSetup(false);
				instance._state = value;
				if (instance._state == GUIState.gameSetup)
					foreach (Player p in Player.all)
						p.OnGameSetup(true);
			}
		}
	}
	
	public static bool AtIngame
	{
		get
		{
			return instance._state == GUIState.ingame || instance._state == GUIState.pause;
		}
	}
	
	void OnGUI()
	{
		GUI.depth = 1;
		if (DualCamera.instance)
			DualCamera.instance.DoGUI();
		
		// dev stuff
		if (Dev.instance)
		{
			Dev.instance.DoGUI();
			if (Dev.instance.showMenu)
			{
				FPSCounter.instance.DoGUI();
				// hide the spinner (it may be obstructing the board view, in case you want to change the level)
				Spinner.instance.usedCamera.enabled = false;
				return;
			}
		}

		GUI.skin = skin;
		
		switch (State)
		{
		case GUIState.splash:
			OnGUI_SplashScreen();
			break;
		case GUIState.gameSetup:
			OnGUI_GameSetup();
			break;
		case GUIState.ingame:
			OnGUI_Ingame();
			break;
		case GUIState.pause:
			OnGUI_Pause();
			break;
		}
		
		// version and fps on top of everithing else
		if (Dev.Allowed)
		{
			if (State == GUIState.splash || State == GUIState.gameSetup)
				Version.instance.DoGUI();
			if (FPSCounter.instance)
				FPSCounter.instance.DoGUI();
		}
	}

	public static bool BackKeyDown()
	{
		return Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
	}
	
	public static bool Button(Rect r, GUIContent content, GUIStyle style, bool playSound)
	{
		if (GUI.Button(r, content, style) && !IsShowingKeyboard)
		{
			if (playSound)
				Sound.PlayButton();
			return true;
		}
		return false;
	}
	
	public static bool Button(Rect r, GUIContent content, GUIStyle style)
	{
		return Button(r, content, style, true);
	}
	
	public static bool Button(Rect r, string content, GUIStyle style, bool playSound)
	{
		return Button(r, new GUIContent(content), style, playSound);
	}
	
	public static bool Button(Rect r, string content, GUIStyle style)
	{
		return Button(r, new GUIContent(content), style, true);
	}
	
	public void DoGUIBackground(bool alternate)
	{
		Texture2D texture = alternate ? assets.backgroundPattern2 : assets.backgroundPattern;
		if (texture)
		{
			Color c = alternate ? assets.backgroundPatternColor2 : assets.backgroundPatternColor;
			float size = alternate ? assets.backgroundPatternSize2 : assets.backgroundPatternSize;
			Rect r = new Rect(0, 0, Screen.width, Screen.height);
			Utils.Fill(r, c, texture, size);
		}
	}
	
	int DoGUIOption(Rect r, string title, string content, bool playSound)
	{
		Rect rTop = RectEx.SubRect0(r, 0.5f, 0f, 1f, 0.5f, TextAnchor.LowerCenter);
		Rect rBottom = RectEx.SubRect0(r, 0.5f, 0.5f, 0.9f, 0.5f, TextAnchor.LowerCenter);
		Rect rLeft = RectEx.SubRectSquare0(rBottom, -0.01f, 0.5f, 1f, 1f, TextAnchor.MiddleLeft);
		Rect rRight = RectEx.SubRectSquare0(rBottom, 1.01f, 0.5f, 1f, 1f, TextAnchor.MiddleRight);
		GUI.Label(rTop, title, styles.optionTitle);
		GUI.Label(rBottom, content, styles.optionContent);
		int result = 0;
		if (Button(rLeft, GUIContent.none, styles.optionLeft, playSound))
			result--;
		if (Button(rRight, GUIContent.none, styles.optionRight, playSound))
			result++;
		return result;
	}

	void DoGUIPlayer(Rect r, Player p, bool left)
	{
		Rect rTop = RectEx.SubRect0(r, 0f, 0f, 1f, 0.25f);
		Rect rNameBox = RectEx.SubRect0(rTop, 0.2f, 0.1f, 0.6f, 0.9f);
		Rect rNameText = RectEx.SubRect0(rNameBox, 0.5f, 0.55f, 1f, 1f, TextAnchor.MiddleCenter);
		Rect rControl = RectEx.SubRectSquare0(rNameBox, 1f, 0.45f, 0.8f, 1f, TextAnchor.MiddleLeft);
		if (!p.isPlaying)
		{
			if (Button(r, string.Empty, styles.player))
			{
				p.Switch();
				GUI.FocusControl("");
			}
			Rect rAdd = RectEx.SubRectSquare0(r, 0.5f, 0.55f, 0.25f, 1f, TextAnchor.MiddleCenter);
			GUI.Box(rAdd, assets.iconAdd, styles.setupButton);
		}
		else
		{
			Color oldColor = GUI.color;
			GUI.color = p.color;
			GUI.Box(r, GUIContent.none, styles.player);
			GUI.color = oldColor;
			if (p.isHuman)
			{
				if (Event.current.Equals(Event.KeyboardEvent("return")))
					GUI.FocusControl("");
				GUI.Box(rNameBox, assets.textFieldBackground, styles.setupButton);
				GUI.color = p.color;
				p.usedName = DoTextField(rNameText, p.usedName, styles.playerTextField, maxPlayerNameLength).ToUpper();

				GUI.color = oldColor;
			}
			else
			{
				GUIContent nameTextContent = new GUIContent(p.usedName);
				Vector2 textSize = styles.playerLabel.CalcSize(nameTextContent);
				float separation = rNameText.width * 0.05f;
				float totalSizeX = textSize.x + rControl.width + separation;
				Vector2 center = rNameBox.center;
				rNameText.x = center.x - totalSizeX / 2;
				rNameText.width = textSize.x;
				rControl.x = center.x + totalSizeX / 2 - rControl.width;
				GUI.Label(rNameText, nameTextContent, styles.playerLabel); // p.defaultName // feedback: always use the name set, even if its playing a computer
				GUI.Box(rControl, assets.iconAI, styles.setupButton);
			}
			if (Button(r, GUIContent.none, GUIStyle.none))
			{
				p.Switch();
				GUI.FocusControl("");
			}
		}
	}
	
	void OnGUI_SplashScreen()
	{
	}
	
	// TextFields are handled differently on the devices in order to properly clamp the text length.
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
	private TouchScreenKeyboard keyboard;
	private int maxTextLength = 0;
	private Rect focusRect;
	
	public string DoTextField(Rect r, string text, GUIStyle style, int maxTextLength)
	{
		if (keyboard == null || !keyboard.active)
		{
			if (GUI.Button(r, text, style) && !IsShowingKeyboard)
			{
				Sound.PlayButton();
				keyboard = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.Default, false, false, false, false, null);
				if (text != keyboard.text)
					keyboard.text = text;
				this.maxTextLength = maxTextLength;
				this.focusRect = r;
			}
			return text;
		}
		else if (r == focusRect)
		{
			GUI.Button(r, text, style);
			return keyboard.text;
		}
		else
		{
			GUI.Button(r, text, style);
			return text;
		}
	}
	
	public void UpdateTextField()
	{
		if (keyboard != null)
		{
			if (keyboard.active)
			{
				if (keyboard.text.Length > maxTextLength)
				{
					keyboard.text = keyboard.text.Substring(0, maxTextLength);
				}
			}
			else //if (keyboard.done || keyboard.wasCanceled)
				keyboard = null;
		}
	}
	
	public static bool IsShowingKeyboard
	{
		get
		{
			return instance && instance.keyboard != null;
		}
	}
#else
	public string DoTextField(Rect r, string text, GUIStyle style, int maxTextLength)
	{
		string newText = GUI.TextField(r, text, style);
		if (newText.Length > maxTextLength)
			newText = newText.Substring(0, maxTextLength);
		return newText;
	}
	
	public void UpdateTextField()
	{
	}
	
	public static bool IsShowingKeyboard
	{
		get
		{
			return false;
		}
	}
#endif
	
	void OnGUI_GameSetup()
	{
		// On Android, pressing the back key, quits the application.
		if (BackKeyDown())
		{
			Application.Quit();
		}

		// Calculate rects
		Rect r0 = RectEx.Build0(0.5f, 0.5f, 1f, 1f, TextAnchor.MiddleCenter);
		float w = 0.5f;
		float h = 0.5f;
		Rect[] rects = new Rect[4];
		rects[0] = RectEx.SubRect0(r0, 0f, 0f, w, h);
		rects[1] = RectEx.SubRect0(r0, 1 - w, 0f, w, h);
		rects[2] = RectEx.SubRect0(r0, 0f, 1 - h, w, h);
		rects[3] = RectEx.SubRect0(r0, 1 - w, 1 - h, w, h);
		Rect rPlay = RectEx.SubRectSquare0(r0, 0.5f, 0.5f, 0.3f, 1f, TextAnchor.MiddleCenter);
		
		// The difficulty option
		Vector2 pos = assets.difficultyPosition;
		Vector2 size = assets.difficultySize;
		Rect rDifficulty = RectEx.Build0(pos.x, pos.y, size.x, size.y, TextAnchor.UpperCenter);
		string dText = Ingame.instance.difficulty.ToString().ToUpper();
		int opt = DoGUIOption(rDifficulty, "DIFFICULTY", dText, true);
		if (opt != 0)
		{
			Ingame.instance.difficulty = Utils.NextEnumValueInt<Ingame.Difficulty>(Ingame.instance.difficulty, opt);
			Store.Save();
		}
		Rect rEventEater = RectEx.Expand0(rDifficulty, 0.05f);
		if (GUI.Button(rEventEater, GUIContent.none, GUIStyle.none))
		{
			// eat the event so its not handled by the players below
		}
		
		// Players must be drawn below the ready button...
		if (Event.current.type == EventType.Repaint)
		{
			DoGUIPlayers(rects);
			DoGUIReady(rPlay);
		}
		// but the ready button must handle the click before players
		else
		{
			DoGUIReady(rPlay);
			DoGUIPlayers(rects);
		}
		// note that after doing this, there will be an issue detecting when you are pressing a button to display the "active" texture
	}
	
	void DoGUIPlayers(Rect[] rects)
	{
		for (int i = 0; i < rects.Length; i++)
			DoGUIPlayer(rects[i], Player.all[i], i % 2 == 0);
	}
	
	void DoGUIReady(Rect r)
	{
		// Is playing allowed? Only if there are at least 2 players.
		int playerCount = 0;
		for (int i = 0; i < Player.all.Count; i++)
		{
			Player p = Player.all[i];
			if (p.isPlaying)
				playerCount++;
		}
		
		bool ready = playerCount > 1 || (Dev.AllowSinglePlayer && playerCount == 1);
		if (ready)
		{
			if (Button(r, string.Empty, styles.ready, false) && !IsShowingKeyboard)
			{
				Sound.PlayButton2();
				State = GUIState.ingame;
				Ingame.instance.StartRound(true);
				UpdateCameras(); // invoke this now, to avoid the first black frame issue
				GUI.FocusControl("");
			}
		}
	}
	
	void OnGUI_Ingame()
	{
		Ingame ingame = Ingame.instance;
		if (ingame.state != Ingame.State.endScene)
		{
			Rect rBackground = RectEx.BuildSquare0(0f, 0f, assets.pauseButtonBackSize, assets.pauseButtonBack, TextAnchor.LowerRight);
			GUI.DrawTexture(rBackground, assets.pauseButtonBack, ScaleMode.ScaleToFit, true);
			Rect rButton = RectEx.SubRectSquare0(rBackground, assets.pauseButtonPosition.x, assets.pauseButtonPosition.y, assets.pauseButtonSize, styles.pause, TextAnchor.MiddleCenter);
			if (Button(rButton, GUIContent.none, styles.pause, false) || BackKeyDown())
			{
				if (ingame.state != Ingame.State.gameStart)
				{
					Sound.PlayButton2();
					State = GUIState.pause;
					lastPauseClickTime = Time.realtimeSinceStartup;
				}
			}
			Ingame.instance.IgnoreTouchBoardsInRect(rBackground);
		}
		else if (BackKeyDown())
		{
			GameGUI.State = GUIState.gameSetup;
		}
		Ingame.instance.DoIngameGUI();
	}
	
	// variables for pause transition effect
	float pauseTransition;
	bool requestUnpause;
	float lastTime;
	float lastPauseClickTime;
	
	void OnGUI_Pause()
	{
		if (assets.pausedBlack && !showingAnyDialog)
			DoGUIBackground(false);
		
		if (Event.current.type == EventType.Repaint)
		{
			float elapsed = lastTime == 0f ? 0f : Time.realtimeSinceStartup - lastTime;
			lastTime = Time.realtimeSinceStartup;
			float delta = 10f * elapsed;
			pauseTransition = Mathf.Clamp01(pauseTransition + (requestUnpause ? -1f : 1f) * delta);
			if (requestUnpause && pauseTransition == 0f)
			{
				requestUnpause = false;
				State = GUIState.ingame;
				lastTime = 0f;
			}
		}
		float usedSize = assets.pauseButtonBackSize;
		if (Screen.dpi > 300)
			usedSize *= assets.superDPIscale;
		Rect rPauseBack = RectEx.BuildSquare0(0f, 0f, usedSize, assets.pauseButtonBack, TextAnchor.LowerRight);
		GUI.DrawTexture(rPauseBack, assets.pauseButtonBack, ScaleMode.ScaleToFit, true);
		Rect rPauseButton = RectEx.SubRectSquare0(rPauseBack, assets.pauseButtonPosition.x, assets.pauseButtonPosition.y, assets.pauseButtonSize, styles.unpause, TextAnchor.MiddleCenter);
		rPauseButton = RectEx.Expand0(rPauseButton, assets.moveButtonScaling.Evaluate(Time.realtimeSinceStartup - lastPauseClickTime) - 1f);
		if ((Button(rPauseButton, GUIContent.none, styles.unpause, false) || BackKeyDown()) && !showingAnyDialog)
		{
			Sound.PlayButton2();
			requestUnpause = true;
			lastPauseClickTime = Time.realtimeSinceStartup;
		}

		Vector2 pausedRestartPosition;
		Vector2 pausedSoundPosition;
		Vector2 pausedMusicPosition;
		Vector2 pausedAirPlayPosition;
		
		float angleStep = (assets.pausedItemEndAngle - assets.pausedItemStartAngle) / (DualCamera.AirPlayCompatible ? 3 : 2);
		Vector2 radiusVector = Vector2.right * assets.pausedItemRadius;
		Vector3 radiusOffset = assets.pausedItemOffset;
		pausedRestartPosition = radiusOffset + (Quaternion.Euler(0, 0, assets.pausedItemStartAngle + angleStep * 0) * radiusVector);
		pausedSoundPosition = radiusOffset + (Quaternion.Euler(0, 0, assets.pausedItemStartAngle + angleStep * 1) * radiusVector);
		pausedMusicPosition = radiusOffset + (Quaternion.Euler(0, 0, assets.pausedItemStartAngle + angleStep * 2) * radiusVector);
		pausedAirPlayPosition = radiusOffset + (Quaternion.Euler(0, 0, assets.pausedItemStartAngle + angleStep * 3) * radiusVector);
		
		usedSize = assets.pausedBackgroundSize;
		if (Screen.dpi > 300)
			usedSize *= assets.superDPIscale;
		Rect rPausedBackground = RectEx.BuildSquare0(0f, 0f, usedSize, assets.pausedBackground, TextAnchor.LowerRight);
		rPausedBackground.width *= pauseTransition;
		rPausedBackground.height *= pauseTransition;
		GUI.DrawTexture(rPausedBackground, assets.pausedBackground, ScaleMode.ScaleToFit, true);
		
		Rect rRestart = RectEx.SubRectSquare0(rPausedBackground, pausedRestartPosition.x, pausedRestartPosition.y, assets.pausedItemSize, styles.pausedRestart, TextAnchor.MiddleCenter);
		if (GUI.Button(rRestart, GUIContent.none, styles.pausedRestart) && !showingAnyDialog)
		{
			Sound.PlayButton2();
			showRestartConfirmation = true;
		}
		
		GUIStyle soundStyle = Sound.instance.fxEnabled ? styles.pausedSoundOn : styles.pausedSoundOff;
		Rect rSound = RectEx.SubRectSquare0(rPausedBackground, pausedSoundPosition.x, pausedSoundPosition.y, assets.pausedItemSize, soundStyle, TextAnchor.MiddleCenter);
		if (GUI.Button(rSound, GUIContent.none, soundStyle) && !showingAnyDialog)
		{
			Sound.instance.fxEnabled = !Sound.instance.fxEnabled;
			Sound.PlayButton();
			Store.Save();
		}
		
		GUIStyle musicStyle = Sound.instance.musicEnabled ? styles.pausedMusicOn : styles.pausedMusicOff;
		Rect rMusic = RectEx.SubRectSquare0(rPausedBackground, pausedMusicPosition.x, pausedMusicPosition.y, assets.pausedItemSize, musicStyle, TextAnchor.MiddleCenter);
		if (GUI.Button(rMusic, GUIContent.none, musicStyle) && !showingAnyDialog)
		{
			Sound.instance.musicEnabled = !Sound.instance.musicEnabled;
			Sound.PlayButton();
			Store.Save();
		}
		
		if (DualCamera.AirPlayCompatible)
		{
			GUIStyle airPlayStyle = styles.pausedAirPlay;
			Rect rAirPlay = RectEx.SubRectSquare0(rPausedBackground, pausedAirPlayPosition.x, pausedAirPlayPosition.y, assets.pausedItemSize, airPlayStyle, TextAnchor.MiddleCenter);
			if (GUI.Button(rAirPlay, GUIContent.none, airPlayStyle) && !showAirPlayDialog)
			{
				Sound.PlayButton();
				showAirPlayDialog = true;
			}
		}
		
		if (showRestartConfirmation)
		{
			DoGUIBackground(true);
		
			Rect rConfirm = RectEx.BuildSquare0(0.5f, 0.5f, 0.7f, 1.5f, TextAnchor.MiddleCenter);
			GUI.Box(rConfirm, GUIContent.none, styles.confirmationWindow);
			
			Rect rText = RectEx.SubRect0(rConfirm, 0.5f, 0.32f, 0.9f, 0.2f, TextAnchor.MiddleCenter);
			GUI.Label(rText, "QUIT GAME?", styles.confirmationText);
			
			Rect rCancel = RectEx.SubRectSquare0(rConfirm, 0.34f, 0.62f, 0.3f, styles.confirmationCancel, TextAnchor.MiddleCenter);
			if (GUI.Button(rCancel, GUIContent.none, styles.confirmationCancel) || BackKeyDown())
			{
				showRestartConfirmation = false;
			}
			
			Rect rOk = RectEx.SubRectSquare0(rConfirm, 0.66f, 0.62f, 0.3f, styles.confirmationOk, TextAnchor.MiddleCenter);
			if (GUI.Button(rOk, GUIContent.none, styles.confirmationOk))
			{
				showRestartConfirmation = false;
				foreach (Player p in Player.all)
					p.Kill();
				State = GUIState.gameSetup;
				if (Dev.DemoMode)
					Player.ResetPlayerNames();
			}
		}
		
		if (showAirPlayDialog)
		{
			DoGUIBackground(true);
		
			Rect rWindow = RectEx.BuildSquare0(0.5f, 0.5f, 0.85f, 1.5f, TextAnchor.MiddleCenter);
			GUI.Box(rWindow, GUIContent.none, styles.confirmationWindow);
			
			string titleAirPlayOff = "PLAY ON APPLE TV";
			string titleAirPlayOn = "PLAY ONLY IN DEVICE";
			Rect rTitle = RectEx.SubRect0(rWindow, 0.45f, 0.2f, 0.8f, 0.1f, TextAnchor.MiddleCenter);
			GUI.Label(rTitle, DualCamera.instance.Active ? titleAirPlayOn : titleAirPlayOff, styles.airplayTitle);
			
			Rect rText = RectEx.SubRect0(rWindow, 0.5f, 0.6f, 0.8f, 0.7f, TextAnchor.MiddleCenter);
			string textAirPlayOff = "This game is awesome on a big screen!\n\nEnable AirPlay by bringing up the Control Center.\n\niOS 6 use the Multitasking Bar";
			string textAirPlayOn = "Turn off Airplay by bringing back up the Control Center. \n\n (On iOS 6 use the Multitasking Bar)";
			GUI.Label(rText, DualCamera.instance.Active ? textAirPlayOn : textAirPlayOff, styles.airplayText);
			
			Rect rClose = RectEx.SubRectSquare0(rWindow, 0.9f, 0.1f, 0.3f, styles.confirmationCancel, TextAnchor.MiddleCenter);
			if (GUI.Button(rClose, GUIContent.none, styles.confirmationCancel) || BackKeyDown())
				showAirPlayDialog = false;
		}		
		
		if (DualCamera.instance.Active && !showingAnyDialog)
		{
			Rect rPaused = RectEx.Build0(0.5f, 0.5f, 0.7f, 0.3f, TextAnchor.MiddleCenter);
			GUI.Label(rPaused, "PAUSED", styles.devicePausedText);
		}
		
		if (Dev.instance && Dev.instance.allowDev)
		{
			Rect rDev = RectEx.Build0(0.99f, 0.01f, 0.2f, 0.1f, TextAnchor.LowerLeft);
			if (Button(rDev, "Dev Menu", styles.menuButton))
				Dev.instance.showMenu = true;
		}
	}
	
	void Update()
	{
		UpdateTextField();
	}
	
	void LateUpdate()
	{
		UpdateCameras();
	}
	
	public void UpdateCameras()
	{
		Ingame ingame = Ingame.instance;
		
		// Render the cameras only at ingame...
		bool renderCameras = AtIngame;
		BoardCamera bc = BoardCamera.instance;
		if (bc)
			bc.Camera.enabled = renderCameras;
		Spinner.instance.usedCamera.enabled = renderCameras;
		
		// Use the current player background color on controls.
		bool usingDualCameras = DualCamera.instance.Active;
		if (usingDualCameras && AtIngame)
		{
			if (ingame.PlayerTurn != null)
			{
				controlsCamera.cullingMask = 1 << ingame.PlayerTurn.gameObject.layer;
				controlsCamera.enabled = true;
			}
			else
			{
				controlsCamera.enabled = false;
			}
		}
		else
		{
			controlsCamera.enabled = false;
		}
		
		gameSetupTvCamera.enabled = usingDualCameras;
		if (gameSetupTvCamera.enabled)
			gameSetupTvCamera.clearFlags = State == GUIState.gameSetup ? CameraClearFlags.Color : CameraClearFlags.Depth;
		
		StaticAssets assets = StaticAssets.instance;
		
		// update the game setup object on the tv.
		Transform tGameSetupBack = assets.gameSetupBackOnTV;
		Transform tGameSetupLogo = assets.gameSetupLogoOnTV;
		tGameSetupBack.gameObject.SetActive(gameSetupTvCamera.enabled && State == GUIState.gameSetup);
		tGameSetupLogo.gameObject.SetActive(tGameSetupBack.gameObject.activeSelf);
		if (tGameSetupBack.gameObject.activeSelf)
		{
			gameSetupTvCamera.ResetAspect();
			
			// set the scale Y to fit in screen height.
			Vector3 ls = tGameSetupBack.localScale;
			ls.x = gameSetupTvCamera.orthographicSize * gameSetupTvCamera.aspect * 2.001f;
			tGameSetupBack.localScale = ls;
			FitScale(tGameSetupBack, false);
			
			ls = tGameSetupLogo.localScale;
			ls.y = gameSetupTvCamera.orthographicSize * assets.gameSetupLogoSize;
			tGameSetupLogo.localScale = ls;
			FitScale(tGameSetupLogo, true);
		}
		
		UpdateReadySetGoBillboard(assets.readySetGoBackOnTV, assets.gameStartTransition0);
		UpdateReadySetGoBillboard(assets.readyOnTV, assets.gameStartTransition1);
		UpdateReadySetGoBillboard(assets.setOnTV, assets.gameStartTransition2);
		UpdateReadySetGoBillboard(assets.goOnTV, assets.gameStartTransition3);
		
		spinnerTvCamera.enabled = usingDualCameras && AtIngame;
		spinAgainTvCamera.enabled = usingDualCameras && AtIngame && ingame.ShowingSpinAgain;
		if (spinAgainTvCamera.enabled)
		{
			float oldDepth = spinAgainTvCamera.depth;
			int oldCullingMask = spinAgainTvCamera.cullingMask;
			CameraClearFlags oldClearFlags = spinAgainTvCamera.clearFlags;
			spinAgainTvCamera.CopyFrom(ingame.boardCamera.Camera);
			spinAgainTvCamera.depth = oldDepth;
			spinAgainTvCamera.cullingMask = oldCullingMask;
			spinAgainTvCamera.clearFlags = oldClearFlags;
		}
		
		assets.grayedTV.enabled = usingDualCameras && _state == GUIState.pause;
		assets.pausedTV.enabled = usingDualCameras && _state == GUIState.pause;
		assets.tapToExitTV.enabled = usingDualCameras && AtIngame && ingame.ShowTapToExit;
		assets.spinAgainOnTV.SetActive(usingDualCameras && AtIngame && ingame.ShowingSpinAgain && ingame.PlayerTurn);
		if (assets.spinAgainOnTV.activeInHierarchy)
			assets.spinAgainOnTV.transform.position = ingame.PlayerTurn.character.transform.position + assets.spinAgainOnTVOffset;
	}
	
	private void UpdateReadySetGoBillboard(Transform t, AnimationCurve curve)
	{
		Ingame ingame = Ingame.instance;
		t.gameObject.SetActive(AtIngame && ingame.state == Ingame.State.gameStart && gameSetupTvCamera.enabled);
		if (t.gameObject.activeSelf)
		{
			// set position
			float tk = ingame.stateTime / ingame.gameStartDuration;
			float x = curve.Evaluate(tk);
			Vector3 pos = t.transform.position;
			pos.x = x * gameSetupTvCamera.orthographicSize * gameSetupTvCamera.aspect * 2.001f;
			t.transform.position = pos;
			
			// set scale
			Vector3 scale = t.transform.localScale;
			scale.x = gameSetupTvCamera.orthographicSize * gameSetupTvCamera.aspect * 2.001f;
			t.localScale = scale;
			FitScale(t, false);
		}
	}
	
	private void FitScale(Transform t, bool horizontal)
	{
		Renderer r = t.GetComponent<Renderer>();
		if (r)
		{
			Material m = r.sharedMaterial;
			if (m)
			{
				Texture texture = m.mainTexture;
				if (texture)
				{
					Vector3 scale = t.localScale;
					float a = (float)texture.width / (float)texture.height;
					if (horizontal)
						scale.x = scale.y * a;
					else
						scale.y = scale.x / a;
					t.localScale = scale;
				}
			}
		}		
	}

	
}
