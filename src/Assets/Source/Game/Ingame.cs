using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class contains the ingame logic and rules. Some of responsabilities it handles:
// * Set the spinner appropiate position for the current state. If the user is panning, hide it.
// * Show / hide the avatars. (with smooth transitions)
// * Animate and display the "ready set go!" starting effect.
// * Set the board camera focus (whenever a player is moving or doing an important action).
// * Handle times and delays between states.
// * Keep track of the current player turn. Determine the win condition.
// * Play a hello animation if no input is received for a long time.
// * Load the adequate spinner probabilites table depending on the character position and game rules.
// * Handle spinner input, make the spinner spin.
// * Handle the move button input. Send the move request to the involved character.
public class Ingame : MonoBehaviour
{
	public enum State
	{
		gameStart,
		turnAdvice,
		waitingSpinnerInput,
		waitingSpinnerStop,
		spinnerResultAdvice,
		waitingPlayerMoveInput,
		waitingCharactersStop,
		turnLostWait,
		endScene,
	}
	
	public enum Difficulty
	{
		easy,
		medium,
		hard
	}
	
	public static Ingame instance;
		
	// Cached references.
	private Spinner spinner;
	private StaticAssets assets;
	
	// The camera used to render the current player turn's character.
	public Camera characterCamera;
	public float characterCameraStartY = 1.3f;
	public float characterCameraAddY = 2.0f;
	public float characterCameraTVAddY = -0.3f; // a little correction on Y is required to avoid the bottom hidden section on the tv
		
	// The board camera. It will be ordered to focus on the current player's character.
	public BoardCamera boardCamera;
	public Difficulty difficulty = Difficulty.medium;
	
	// The current game state and state related variables.
	private State _state = State.gameStart;
	private float stateTimeStart;
	private float lastFrameTime;
	private float lastFrameTime2;
	private bool fastForwardSkipTurn;
	private float elapsedTimeNotMoving;
	private int skipUpdateFramesLeft; // used to avoid the first lag after first character instantiations
		
	// Is the game ready to switch states?
	// While there is something happening, the state should not change, for example, while the barking dog is running.
	private bool ready;

	// The direction the characters will face when standing still.
	public Vector3 forward = -Vector3.forward;
	public bool adviceEnabled = false;
	
	// Game start GUI variables.
	public float gameStartDuration = 3f;
	
	// Advice turn state and GUI variables.
	public float turnAdviceDuration = 1f;
	private bool loseTurn;
	
	// Turn lost state variables.
	public float turnLostWait = 2f;
	
	// Spinner state and GUI variables.
	public float spinnerAdviceWait = 3f;
	private bool draggingSpinner;
	private float dragSpinnerStartTime;
	private Spinner.Result lastResult;
	private float momentarySpinnerHideTimeLeft;
	public float momentarySpinnerHideTime = 1f;
	private bool momentaryHideAllowed;
	
	// Tha current player turn.
	private Player playerTurn;

	// Move state and GUI variables.
	public float avatarDuration = 1f;
	private bool avatarShow;
	private float avatarTime;
	private Player avatarPlayer;
	private int queuedMoves;
	private float characterPlayHelloTime;
	public float timeToStartAutomove = 1f;
	public float waitAfterMoveTwirlAgain = 0.2f;
	public float waitAfterMove = 1f;
	private TouchButton spinnerButton = new TouchButton();
	private float movePlayHintStartTime;
	
	// Move button.
	public float moveButtonDuration = 1f;
	private bool moveButtonShow;
	private float moveButtonTime;
	
	// The move button handles touches individually.
	private TouchButton moveButton = new TouchButton();
	private float lastMoveTime;

	// After the current player has ended moving, the camera will focus on any character that is moving (in case they start pushing each other or resolve connections).
	// This delay is to avoid focusing the player right when it has ended moving (it has to let a frame pass to ensure its on "idle and ready" state).
	public float ignoreCameraFocusDelay = 0.5f;
	
	// Layer masks used to determie the objects that can be touched.
	public LayerMask spinnerMask;
	public LayerMask boardItemsMask;
	
	// Variables used to detect single clicks for touching board objects.
	private List<Vector2> queuedTouchBoardPoints = new List<Vector2>();
	private float mouseDownTime;
	private Dictionary<int, float> touchDownTime = new Dictionary<int, float>();
	private List<Rect> ignoreRects = new List<Rect>();
	
	// Rule of 6.
	public bool sixTwirlsAgain = false;
	
	// Special bubble text message when you get the skate.
	private float showSkateAdvice;
	
	// The winner
	private Player theWinnerPlayer;
	public float victoryZoom = 3f;
	public Vector3 victoryFocusOffset = Vector3.up * 0.5f;
	public bool allowMovingCameraAtEnd = false;
	public float timeToSkipVictory = 5f;
	
	public State state
	{
		get
		{
			return _state;
		}
		set
		{
			SetState(value, true, false);
		}
	}
		
	public void SetState(State newState, bool resetStateTime, bool force)
	{
		if (_state != newState || force)
		{
			_state = newState;
			if (resetStateTime)
			{
				stateTimeStart = Time.time;
				lastFrameTime = Time.time;
				lastFrameTime2 = Time.time;
				ResetHelloTime();
				ResetMovePlayHintTime();
			}
		}
	}
	
	public float stateTime
	{
		get
		{
			return Time.time - stateTimeStart;
		}
	}
	
	bool IsStateTime(float t)
	{
		return Time.time - stateTimeStart > t && lastFrameTime2 - stateTimeStart <= t;
	}
	
	public Player PlayerTurn
	{
		get
		{
			return playerTurn;
		}
	}
	
	public int QueuedMoves
	{
		get
		{
			return queuedMoves;
		}
	}
	
	private Styles styles
	{
		get
		{
			return Styles.instance;
		}
	}
	
	public Player TheWinner
	{
		get
		{
			return theWinnerPlayer;
		}
	}
	
	public bool ShowTapToExit
	{
		get
		{
			return state == State.endScene && stateTime > timeToSkipVictory;
		}
	}
	
	public bool ShowingSpinAgain
	{
		get
		{
			return showSkateAdvice != 0f && state == State.waitingSpinnerInput;
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
		spinner = Spinner.instance;
		assets = StaticAssets.instance;
		
		// instantiate the character models on the first frame, to avoid the lag when the round starts.
		foreach (Player p in Player.all)
		{
			// instantiate the model (generates some lag)
			p.GetCharacter();
			// make it inactive (hide it)
			p.Kill();
		}
	}
	
	public void StartRound(bool hardReset)
	{
		// Set random chutes and ladders.
		if (hardReset)
			BoardInfo.instance.LoadRandomLevel();
		
		// Kill all characters.
		foreach (Player p in Player.all)
		{
			p.Restore();
			p.SetDoubleActive(false);
		}
		
		// Spawn all playing characters.
		foreach (Player p in Player.all)
			if (p.isPlaying)
				p.Spawn();
		
		// Find the first playing player.
		playerTurn = NextTurn(null);
		playerTurn.SetDoubleActive(true);
		
		// make sure no time passes to prevent the first frame lag
		Time.timeScale = 0f;
		skipUpdateFramesLeft = 3;
			
		// Reset the game state.
		SetState(State.gameStart, true, true);
		
		// Reset variables...
		fastForwardSkipTurn = false;
		elapsedTimeNotMoving = 0f;
		avatarTime = 0f;
		avatarShow = false;
		avatarPlayer = null;
		lastResult = Spinner.Result.unknown;
		loseTurn = false;
		theWinnerPlayer = null;
		showSkateAdvice = 0f;
		momentarySpinnerHideTimeLeft = 0f;
		spinner.position = Spinner.PositionType.hidden;
		characterCamera.enabled = false;
		
		// Hard reset: avoid fadings...
		if (hardReset)
		{
			Spinner.instance.Restore();
			BoardCamera.instance.Restore();
		}
		
		boardCamera.ResetView();
	}

	public Player NextTurn(Player currentTurn)
	{
		int i = currentTurn != null ? Player.all.IndexOf(currentTurn) : -1;
		if (i == -1)
		{
			foreach (Player p in Player.all)
				if (p.isPlaying)
					return p;
		}
		else
		{
			for (int j = 1; j < Player.all.Count; j++)
			{
				int index = (i + j) % Player.all.Count;
				Player p = Player.all[index];
				if (p.isPlaying)
					return p;
			}
		}
		return currentTurn;
	}

	void LoadTransitionMatrix(float t, AnimationCurve curveX, AnimationCurve curveY)
	{
		float x = curveX != null ? (curveX.Evaluate(t) * Screen.width) : 0f;
		float y = curveY != null ? (curveY.Evaluate(t) * Screen.height) : 0f;
		GUI.matrix *= Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, Vector3.one);
	}
	
	void DoTransitionTexture(Texture2D texture, Vector2 position, Vector2 size, float time, AnimationCurve curveX, AnimationCurve curveY)
	{
		if (!texture)
			return;
		Matrix4x4 oldMatrix = GUI.matrix;
		Rect r = RectEx.Build0(position.x, position.y, size.x, size.y, TextAnchor.MiddleCenter);
		LoadTransitionMatrix(time, curveX, curveY);
		GUI.DrawTexture(r, texture, ScaleMode.StretchToFill);
		IgnoreTouchBoardsInRect(r);
		GUI.matrix = oldMatrix;
	}

	public void DoIngameGUI()
	{
		bool isRepaint = Event.current.type == EventType.Repaint;

		if (state == State.gameStart && isRepaint)
		{
			if (IsStateTime(gameStartDuration * assets.gameStartMenuInTime))
				Sound.PlayMenuIn();
			if (IsStateTime(gameStartDuration * assets.gameStartMelodyTime))
				Sound.Play(Sound.instance.fxGameStart);
			if (IsStateTime(gameStartDuration * assets.gameStartMenuOutTime))
				Sound.PlayMenuOut();
			
			float tk = stateTime / gameStartDuration;

			Vector3 pos = assets.gameStartPosition;
			Vector3 size = assets.gameStartSize;
			DoTransitionTexture(assets.gameStart0, pos, size, tk, assets.gameStartTransition0, null);
			DoTransitionTexture(assets.gameStart1, pos, size, tk, assets.gameStartTransition1, null);
			DoTransitionTexture(assets.gameStart2, pos, size, tk, assets.gameStartTransition2, null);
			DoTransitionTexture(assets.gameStart3, pos, size, tk, assets.gameStartTransition3, null);
		}
		
		// player avatar
		if (moveButtonTime != 0f && avatarPlayer)
		{
			int qm = avatarPlayer == playerTurn && (state == State.waitingPlayerMoveInput || state == State.spinnerResultAdvice) ? queuedMoves : 0;
			bool canMove = qm > 0 && playerTurn.isHuman;
			
			Color oldColor = GUI.color;
			bool useDisabledColor = DualCamera.instance.Active && (state != State.waitingPlayerMoveInput || !canMove);
			if (useDisabledColor)
				GUI.color = assets.disabledColor;
			
			float buttonSize = assets.moveButtonBackSize;
			Vector2 rBackPos = assets.moveButtonPosition;
			Rect rBack = RectEx.BuildSquare0(rBackPos.x, rBackPos.y, buttonSize, assets.moveButtonBack, TextAnchor.MiddleCenter);
			
			// make sure the move button does not get out of screen bounds
			float maxX = Screen.width - 10;
			if (rBack.xMax > maxX)
				rBack.x += maxX - rBack.xMax;
			
			// scale animation value
			float k = moveButtonTime / moveButtonDuration;
			k = 1 - k;
			rBack.y += rBack.height * (k * k);
			
			GUI.DrawTexture(rBack, assets.moveButtonBack, ScaleMode.StretchToFill);
			IgnoreTouchBoardsInRect(rBack);
			
			Rect rMove = RectEx.SubRect0(rBack, 0.475f, 0.46f, 0.7f, 0.7f, TextAnchor.MiddleCenter);
			rMove = RectEx.Expand0(rMove, assets.moveButtonScaling.Evaluate(Time.realtimeSinceStartup - lastMoveTime) - 1f);
			Rect rMove2 = RectEx.Expand(rMove, rMove.width * 0.2f);
			IgnoreTouchBoardsInRect(rMove2);
			
			moveButton.DoGUI(rMove, rMove2, avatarPlayer.coloredButton, ScaleMode.StretchToFill, canMove);
			if (qm > 0 && !useDisabledColor)
			{
				float movePlayTimeElapsed = Time.realtimeSinceStartup - movePlayHintStartTime - 10f;
				if (movePlayTimeElapsed < 0 || Mathf.Repeat(movePlayTimeElapsed / assets.moveButtonPlayFrequency, 2f) < 1f)
				{
					Texture2D[] numbers = assets.moveButtonNumbers;
					if (numbers.Length > qm && numbers[qm] != null)
						GUI.DrawTexture(RectEx.SubRect0(rMove, assets.moveButtonNumberPosition, TextAnchor.MiddleCenter), numbers[qm], ScaleMode.ScaleToFit);
					else
						GUI.Label(rMove, "" + qm, styles.moveButtonText);
				}
				else
				{
					GUI.DrawTexture(RectEx.SubRect0(rMove, assets.moveButtonPlayPosition, TextAnchor.MiddleCenter), assets.moveButtonPlay, ScaleMode.ScaleToFit);
				}
			}
			if (isRepaint && canMove)
			{
				bool moved = false;
				if (moveButton.holded)
				{
					moved |= Move(false);
				}
				else if (moveButton.singlePress)
				{
					moved |= Move(true);
				}
				if (moved)
				{
					lastMoveTime = Time.realtimeSinceStartup;
				}
			}
			if (moveButton.released)
				Sound.Play(Sound.instance.fxMoveButton);
			
			GUI.color = oldColor;
		}
		
		if (state == State.waitingSpinnerInput && playerTurn.isHuman && (stateTime > 1f || Dev.FastForwardMenues))
		{
			if (Dev.Cheats)
			{
				Rect rStart = new Rect(5, 5, (Screen.width - 5 * 4) / 3f, Screen.height * 0.1f);
				float sy = rStart.height + 5;
				Rect r3 = rStart;
				float t = spinner.table.Total;
				for (int i = 0; i < Spinner.RESULT_COUNT; i++)
				{
					string text = ((Spinner.Result)i).ToString();
					text += string.Format(" ({0:0.00}%)", spinner.table.Get(i) * 100 / t);
					if (GameGUI.Button(r3, text, styles.menuButton))
						Spin((Spinner.Result)i);
					r3.y += sy;
					if (i % 6 == 5)
					{
						r3.x += r3.width + 5;
						r3.y = rStart.y;
					}
				}
				GUI.Label(r3, string.Format("Good: {0:0.00}%", spinner.table.Good * 100 / t), styles.menuButton);
				r3.y += sy;
				GUI.Label(r3, string.Format("Bad: {0:0.00}%", spinner.table.Bad * 100 / t), styles.menuButton);
				r3.y += sy * 2;
				if (GUI.Button(r3, "GO SPINNER GO!!!", styles.menuButton))
					Spin();
			}
		}
		
		if (state == State.endScene)
		{
			if (DualCamera.instance.Active)
			{
				GameGUI.instance.DoGUIBackground(false);
				Rect rVictoryName = assets.victoryNamePosition.rect;
				string text = theWinnerPlayer.usedName + " WINS!";
				GUI.Label(rVictoryName, text, styles.deviceVictoryNameText);
			}
			
			if (stateTime > timeToSkipVictory)
			{
				/*
				Vector2 rPos = assets.tapToExitPosition;
				Vector2 rSize = assets.tapToExitSize;
				Rect r = RectEx.Build0(rPos.x, rPos.y, rSize.x, rSize.y, TextAnchor.UpperCenter);
				GUI.Box(r, "TAP TO EXIT", styles.tapToExit);
				
				if (Input.GetMouseButtonDown(0) && !IsInsideIgnoreRect(TouchButton.Flip(Input.mousePosition)))
				{
					GameGUI.State = GUIState.gameSetup;
				}
				*/

				if (GUI.Button(styles.exitButtonPosition.rect, "", styles.exitButtonStyle))
				{
					GameGUI.State = GUIState.gameSetup;
					if (Dev.DemoMode)
						Player.ResetPlayerNames();
				}

				if (GUI.Button(styles.rematchButtonPosition.rect, "", styles.rematchButtonStyle))
				{
					StartRound(true);
				}
			}
		}

		// Skip AI button (fast forward)
		if (state != State.endScene && state != State.gameStart && state != State.endScene && !fastForwardSkipTurn)
		{
			if (playerTurn && !playerTurn.isHuman && !fastForwardSkipTurn)
			{
				if (Event.current.type == EventType.MouseDown && !IsInsideIgnoreRect(TouchButton.Flip(Input.mousePosition)))
				{
					fastForwardSkipTurn = true;
					// If its waiting to stop AI moves, zoom out the camera to avoid dizzyng the user.
					if (state == State.waitingPlayerMoveInput)
						boardCamera.ResetView();
				}
			}
		}
		
		if (showSkateAdvice != 0f && state == State.waitingSpinnerInput && momentarySpinnerHideTimeLeft <= 0f && !DualCamera.instance.Active)
		{
			Vector2 bubblePos = assets.skateTextBubblePosition;
			Vector2 bubbleSize = assets.skateTextBubbleSize;
			Rect rBubble = RectEx.Build0(bubblePos.x, bubblePos.y, bubbleSize.x, bubbleSize.y, assets.skateTextAnchor);
			GUI.Box(rBubble, "SPIN AGAIN!", styles.skateTextBubble);
		}
		
		if (DualCamera.instance.Active)
		{
			if (state == State.waitingSpinnerInput || state == State.waitingPlayerMoveInput)
			{
				Rect rHint = assets.ingameHintPosition.rect;
				float rt = Mathf.Repeat(stateTime, assets.ingameHintFrequency) / assets.ingameHintFrequency;
				string text = rt < assets.ingameHint1T ? "Pinch here\nto Zoom!" : rt < assets.ingameHint2T ? "Drag from here\nto Pan!" : "";
				GUI.Label(rHint, text, styles.devicePinchPanText);
			}
			if (state == State.gameStart && isRepaint)
				GameGUI.instance.DoGUIBackground(false);
		}
		
	}
	
	void HandleSpinnerInput()
	{
		// get the spinner bounds
		Transform spinnerT = spinner.modelRoot.transform;
		Bounds b = spinner.GetBounds();
		Vector2 bMin = spinner.usedCamera.WorldToScreenPoint(b.min);
		Vector2 bMax = spinner.usedCamera.WorldToScreenPoint(b.max);
		
		// fit bound values to screen coordinates
		bMin.y = Screen.height - bMin.y;
		bMax.y = Screen.height - bMax.y;
		float temp = bMin.y;
		bMin.y = bMax.y;
		bMax.y = temp;
		
		// set the emulated spinner button rect
		spinnerButton.logicRect = new Rect(bMin.x, bMin.y, bMax.x - bMin.x, bMax.y - bMin.y);

		// update the button
		spinnerButton.Update(!Dev.Cheats && state == State.waitingSpinnerInput);
		
		// do nothing more if its not waiting user input
		if (state != State.waitingSpinnerInput)
			return;
		
		// check if its touching inside the spinner circle, and move the arrow...
		bool oldDraggingSpinner = draggingSpinner;
		if (!Dev.Cheats)
		{
			draggingSpinner = false;
			
			// calculate the clicked spinner angle...
			if (spinnerButton.isPressed)
			{
				// avoid panning while dragging the spinner
				if (DualCamera.instance.Active && momentarySpinnerHideTimeLeft != 0f)
					momentarySpinnerHideTimeLeft = 0f;
				
				// only if the spinner its close to the camera
				if (spinner.position == Spinner.PositionType.center && spinner.HasArrived)
				{
					Vector3 inputPosition = spinnerButton.touchPosition;
					inputPosition.y = Screen.height - inputPosition.y;
					Ray ray = spinner.usedCamera.ScreenPointToRay(inputPosition);
					Plane plane = new Plane(spinnerT.forward, spinnerT.position);
					float enter;
					if (plane.Raycast(ray, out enter))
					{
						Vector3 planeMousePosition = ray.GetPoint(enter);
						Vector3 planeMouseRelPos = spinnerT.InverseTransformPoint(planeMousePosition);
						planeMouseRelPos.z = 0;
						if ((spinnerButton.isPressed && oldDraggingSpinner) || planeMouseRelPos.magnitude < spinner.inputRadius)
						{
							draggingSpinner = true;
							if (spinnerButton.down)
								dragSpinnerStartTime = Time.realtimeSinceStartup;
							float angle = Vector3.Angle(Vector3.left, planeMouseRelPos);
							if (Vector3.Dot(Vector3.up, planeMouseRelPos) < 0)
								angle = 360 - angle;
							spinner.arrowTargetAngle += Mathf.DeltaAngle(spinner.arrowTargetAngle, angle);
							if (spinnerButton.down)
								spinner.arrowCurrentAngle = spinner.arrowTargetAngle;
						}
					}
				}
				// its at the corner of the screen... on first click cancel the hidding
				else if (spinnerButton.down && momentarySpinnerHideTimeLeft != 0f)
				{
					momentarySpinnerHideTimeLeft = 0f;
					momentaryHideAllowed = false;
					spinnerButton.Reset();
				}
			}
			// on release, if the arrow has enought speed, start spinning!
			if (spinnerButton.released && spinner.position == Spinner.PositionType.center && spinner.HasArrived)
			{
				float diff = spinner.arrowTargetAngle - spinner.arrowCurrentAngle;
				int spins = (int)(diff / spinner.angleDragDiffToStartSpinning);
				if (spins != 0)
					Spin(spins);
				// single tap, spins randomly
				else if (dragSpinnerStartTime != 0f && Time.realtimeSinceStartup - dragSpinnerStartTime < 0.2f) 
					Spin();
			}
		}
		
		// if you are touching anything
		if (Input.touchCount != 0 || Input.GetMouseButtonDown(0))
		{
			// and no touch is dragging the spinner
			if (!draggingSpinner && !oldDraggingSpinner && momentaryHideAllowed && !spinnerButton.isPressed && !spinnerButton.released)
			{
				// find out if all touches are outside the ignore rects (this is to avoid hidding the spinner when you click on the pause button)
				bool doHide = false;
				if (!TouchButton.ignoreMouseInput && Input.GetMouseButtonDown(0) && !IsInsideIgnoreRect(TouchButton.Flip(Input.mousePosition)))
				{
					doHide = true;
				}
				if (!doHide)
				{
					for (int i = 0; i < Input.touchCount; i++)
					{
						Touch t = Input.GetTouch(i);
						if (t.phase == TouchPhase.Began && !IsInsideIgnoreRect(TouchButton.Flip(t.position)))
						{
							doHide = true;
							break;
						}
					}
				}
				
				// then momentary hide the spinner (it seems you want to pan)
				if (doHide)
					momentarySpinnerHideTimeLeft = momentarySpinnerHideTime;
			}
		}
		// there is no touch present
		else
		{
			momentaryHideAllowed = true;
		}
	}

	void Spin()
	{
		spinner.Spin();
		state = State.waitingSpinnerStop;
		showSkateAdvice = 0;
	}
	
	void Spin(int spins)
	{
		spinner.Spin(spins);
		state = State.waitingSpinnerStop;
		showSkateAdvice = 0;
	}
	
	void Spin(Spinner.Result r)
	{
		spinner.Spin(r);
		state = State.waitingSpinnerStop;
		showSkateAdvice = 0;
	}
	
	bool Move(bool single)
	{
		if (queuedMoves <= 0)
			return false;
		bool ret = false;
		if (playerTurn.character.QueueMove(single))
		{
			queuedMoves--;
			ret = true;
		}
		ResetMovePlayHintTime();
		ResetHelloTime();
		FocusIfNecesary(playerTurn.character);
		return ret;
	}
	
	void FocusIfNecesary(Character character)
	{
		Vector3 posToFocus = character.transform.position;
		Cell cell = character.TargetCell;
		if (!cell)
			cell = character.CurrentCell;
		for (int i = 0; i < character.QueuedMoves && cell; i++)
		{
			cell = cell.next;
			if (cell)
			{
				posToFocus = cell.transform.position;
				posToFocus.y = 1f;
			}
		}
		FocusIfNecesary(posToFocus);
	}
	
	void FocusIfNecesary(Vector3 pos)
	{
		if (!boardCamera.IsInsideViewport(pos))
			boardCamera.SetTarget(pos, 0f, false);
	}
	
	Character lastMovingCharacter;
	float lastMovingCharacterTime;
	
	public bool AllowConnectionResolveMove(Character c)
	{
		if (Time.timeScale > 1f)
			return true;
		return state == State.waitingCharactersStop && lastMovingCharacter == c && Time.realtimeSinceStartup > lastMovingCharacterTime + 1f;
	}
	
	void UpdateCharacterCameraPosition()
	{
		if (characterCamera.enabled)
		{
			characterCamera.cullingMask = (1 << LayerMask.NameToLayer("player0")) + (1 << playerTurn.gameObject.layer);
			Vector3 pos = characterCamera.transform.position;
			pos.y = characterCameraStartY + (1f - (avatarTime / avatarDuration)) * characterCameraAddY;
			if (DualCamera.instance.Active)
				pos.y += characterCameraTVAddY;
			characterCamera.transform.position = pos;
			StaticAssets.instance.avatarText.text = avatarPlayer.usedName;
			characterCamera.ResetAspect();
		}		
	}
	
	void Update()
	{
		float time = Time.time;
		
		if (skipUpdateFramesLeft > 0)
		{
			Time.timeScale = 0f;
			skipUpdateFramesLeft--;
			return;
		}
		
		// Do nothing if its not at ingame state.
		if (GameGUI.State != GUIState.ingame)
		{
			Time.timeScale = GameGUI.State == GUIState.pause ? 0f : 1f;
			characterCamera.enabled = GameGUI.AtIngame;
			spinner.position = Spinner.PositionType.hidden;
			momentarySpinnerHideTimeLeft = 0f;
			UpdateCharacterCameraPosition();
			return;
		}

		Time.timeScale = (Dev.FastForwardTime && state != State.waitingSpinnerInput) || fastForwardSkipTurn ? 10 : 1;
		
		// Update avatar transition.
		if (avatarTime == 0f)
			avatarPlayer = playerTurn;
		float dt = Time.deltaTime;
		avatarTime = Mathf.Clamp(avatarTime + (avatarShow ? dt : -dt), 0, avatarDuration);
		bool lastAvatarShow = avatarShow;
		avatarShow = queuedMoves != 0 || moveButton.isPressed || moveButton.released;
		avatarShow &= state == State.waitingCharactersStop;
		if (!adviceEnabled)
			avatarShow |= state == State.waitingSpinnerInput || state == State.waitingSpinnerStop || state == State.spinnerResultAdvice;
		avatarShow &= avatarPlayer == playerTurn;
		Sound.PlayMenuInOuOnBoolChange(lastAvatarShow, avatarShow);
		characterCamera.enabled = avatarTime > 0;
		UpdateCharacterCameraPosition();
		
		// Update move button transition.
		moveButtonTime = Mathf.Clamp(moveButtonTime + (moveButtonShow ? dt : -dt), 0, moveButtonDuration);
		if (DualCamera.instance.Active)
			moveButtonTime = moveButtonDuration;
		bool lastButtonShow = moveButtonShow;
		moveButtonShow = state == State.waitingPlayerMoveInput && playerTurn.isHuman && (queuedMoves != 0 || moveButton.isPressed);
		if (DualCamera.instance.Active)
			Sound.PlayMenuInOuOnBoolChange(lastButtonShow, moveButtonShow);
		
		// The spinner is close to the camera if its on a spinner state...
		if (momentarySpinnerHideTimeLeft > 0f && state == State.waitingSpinnerInput && !DualCamera.instance.Active)
		{
			spinner.position = Spinner.PositionType.side;
		}
		else if (state == State.waitingSpinnerInput ||
			state == State.waitingSpinnerStop ||
			state == State.spinnerResultAdvice)
		{
			spinner.position = Spinner.PositionType.center;
		}
		else
		{
			spinner.position = Spinner.PositionType.hidden;
		}
		
		// Set the camera focus target.
		Character movingCharacter = null;
		if (state == State.gameStart || playerTurn == null || playerTurn.character == null)
		{
			boardCamera.ResetView();
		}
		else if (DualCamera.instance.Active && ShowingSpinAgain && Time.time - showSkateAdvice < 2f)
		{
			// focus the avatar on the tv, so the "Spin Again" can be properly seen
			// This has to be applied for some time until the camera ends the smoothing transition because
			// the target position of the camera is clamped if it gets out of bounds.
			boardCamera.SetTarget((playerTurn.character.FocusPosition + assets.spinAgainOnTV.transform.position) * 0.5f, 1f, false);
		}
		else if (state == State.waitingPlayerMoveInput)
		{
			if (Spinner.IsSpecialResult(lastResult))
			{
				boardCamera.SetTarget(playerTurn.character.FocusPosition, 1f, true);
			}
		}
		// (avoid the first moment so the idle flag is properly updated and set after passing another character)
		else if (state == State.waitingCharactersStop && stateTime > ignoreCameraFocusDelay && Time.timeScale == 1f)
		{
			if (!playerTurn.character.IsIdle && Spinner.IsSpecialResult(lastResult))
				movingCharacter = playerTurn.character;
			else
				movingCharacter = FindMovingCharacter();
			if (movingCharacter && movingCharacter.RequiresCameraFocus)
				boardCamera.SetTarget(movingCharacter.FocusPosition, 1f, true);
		}
		// focus continuously on the winning player (ultil it finishes moving to the end position)
		else if (state == State.endScene && theWinnerPlayer)
		{
			boardCamera.SetTarget(theWinnerPlayer.character.FocusPosition + victoryFocusOffset, victoryZoom, true);
		}
		
		if (lastMovingCharacter != movingCharacter)
		{
			lastMovingCharacter = movingCharacter;
			lastMovingCharacterTime = Time.realtimeSinceStartup;
		}
		
		bool allowCameraControl = ((state == State.waitingPlayerMoveInput || state == State.waitingCharactersStop) && playerTurn.isHuman) || state == State.endScene || momentarySpinnerHideTimeLeft > 0f;
		if (state == State.endScene && !allowMovingCameraAtEnd)
			allowCameraControl = false;
		boardCamera.DoUpdateCameraInput(allowCameraControl);
		
		// update the momentary spinner hide flag
		if (allowCameraControl && Input.GetAxisRaw("Mouse ScrollWheel") != 0f)
			momentarySpinnerHideTimeLeft = momentarySpinnerHideTime;
		else if (!Input.GetMouseButton(0) && Input.touchCount == 0)
			momentarySpinnerHideTimeLeft -= Time.deltaTime;
		if (momentarySpinnerHideTimeLeft < 0f)
			momentarySpinnerHideTimeLeft = 0f;
		
		if (state == State.gameStart)
		{
			if (stateTime > gameStartDuration || Dev.FastForwardMenues)
			{
				state = State.turnAdvice;
				foreach (Player player in Player.all)
					player.OnGameStart();
			}
		}
		else if (state == State.turnAdvice)
		{
			if (stateTime > turnAdviceDuration || Dev.FastForwardMenues)
			{
				if (loseTurn)
				{
					loseTurn = false;
					state = State.turnLostWait;
					lastResult = Spinner.Result.badRain; // emulates a "bad rain" result
				}
				else
				{
					playerTurn.character.OnTurnStart();
					state = State.waitingSpinnerInput;
					LoadSpinnerTable(playerTurn, true);
				}
			}
		}
		else if (state == State.turnLostWait)
		{
			if (stateTime > turnLostWait || Dev.FastForwardMenues)
			{
				state = State.waitingCharactersStop;
			}
			else
			{
				boardCamera.SetTarget(playerTurn.character.FocusPosition, 1f, true);
			}
		}
		else if (state == State.waitingSpinnerInput)
		{
			// wait until the user presses the "Spin" button
			// or if its a computer, do an auto spin
			if (playerTurn.isHuman)
			{
				spinner.RequestHighSpeed();
				if (stateTime < 1f || Input.GetMouseButton(0)) // resets hello time
				{
					ResetHelloTime();
				}
				else if (Time.realtimeSinceStartup > characterPlayHelloTime)
				{
					playerTurn.GetDouble().PlayHello();
					ResetHelloTime();
				}
			}
			else if (stateTime > 1f || Dev.FastForwardMenues)
				Spin();
		}
		else if (state == State.waitingSpinnerStop)
		{
			if (!spinner.Moving)
			{
				queuedMoves = spinner.GetMoves();
				if (playerTurn.character.skate)
				{
					queuedMoves *= 2;
					playerTurn.character.usedSkateboard = true;
				}
				state = State.spinnerResultAdvice;
				lastResult = spinner.GetResult();
				if (Spinner.IsGoodResult(lastResult))
					Sound.Play(Sound.instance.fxGoodResult);
				else if (Spinner.IsBadResult(lastResult))
					Sound.Play(Sound.instance.fxBadResult);
				else
					Sound.Play(Sound.instance.fxNumberResult);
			}
		}
		else if (state == State.spinnerResultAdvice)
		{
			if (stateTime > spinnerAdviceWait || Dev.FastForwardMenues)
			{
				fastForwardSkipTurn = false;
				playerTurn.character.OnSpinnerResult(lastResult);
				if (lastResult == Spinner.Result.goodSkateboard)
				{
					state = State.waitingSpinnerInput;
					LoadSpinnerTable(playerTurn, false);
					showSkateAdvice = Time.time;
				}
				else
				{
					state = State.waitingPlayerMoveInput;
				}
			}
		}
		else if (state == State.waitingPlayerMoveInput)
		{
			// Auto move the character
			if ((!playerTurn.isHuman || Dev.AutoHumanMove) && (stateTime > timeToStartAutomove || Dev.FastForwardMenues))
			{
				if (queuedMoves > 0 && playerTurn.character.IsIdle)
					Move(!fastForwardSkipTurn);
			}
			// If there are no more cells to move, continue to the next state
			if (queuedMoves == 0)
			{
				if (stateTime > 1f && playerTurn.character.Ready && !moveButton.isPressed && !moveButton.released)
				{
					state = State.waitingCharactersStop;
					elapsedTimeNotMoving = 0f;
					// Consume the skateboard after being used.
					if (playerTurn.character.skate && playerTurn.character.usedSkateboard)
					{
						StaticAssets.Spawn(StaticAssets.instance.dustSkateDisappearPrefab, playerTurn.character.skate.transform.position, 1f);
						playerTurn.character.DestroySkate();
					}
				}
			}
			// Wait until the user moves all cells
			else
			{
				// Ignore inputing cells if it reached the end
				Cell cell = playerTurn.character.CurrentCell;
				if (cell && cell.next == null)
					queuedMoves = 0;
			}
			
			if (queuedMoves != 0 && Time.realtimeSinceStartup > characterPlayHelloTime && playerTurn.isHuman)
			{
				playerTurn.character.PlayHello();
				ResetHelloTime();
			}
		}
		else if (state == State.waitingCharactersStop)
		{
			if (AreAllCharactersReady() && ready)
			{
				elapsedTimeNotMoving += Time.deltaTime;
				// Wait a little after all characters stopped moving
				float timeToWait = (sixTwirlsAgain && lastResult == Spinner.Result.move6) || lastResult == Spinner.Result.goodSkateboard ? waitAfterMoveTwirlAgain : waitAfterMove;
				if (elapsedTimeNotMoving > timeToWait || Dev.FastForwardMenues)
				{
					// Check if any player has reached the last cell.
					Player thePlayer = null;
					foreach (Player p in Player.all)
					{
						if (p.isPlaying && p.character)
						{
							Cell cell = p.character.CurrentCell;
							if (cell && cell.next == null)
							{
								thePlayer = p;
								break;
							}
						}
					}
					// Is there a winner?
					if (thePlayer)
					{
						fastForwardSkipTurn = false;
						state = State.endScene;
						theWinnerPlayer = thePlayer;
						Sound.Play(Sound.instance.fxVictory);
					}
					// There is no winner... continue with the next player.
					else
					{
						fastForwardSkipTurn = false;
						if ((sixTwirlsAgain && lastResult == Spinner.Result.move6) || lastResult == Spinner.Result.goodSkateboard)
						{
							state = State.turnAdvice;
							LoadSpinnerTable(playerTurn, true);
							if (playerTurn.character.ConsumeCloundRain())
								loseTurn = true;
						}
						else
						{
							if (playerTurn)
								playerTurn.SetDoubleActive(false);
							playerTurn = NextTurn(playerTurn);
							if (playerTurn)
								playerTurn.SetDoubleActive(true);
							state = State.turnAdvice;
							FocusIfNecesary(playerTurn.character);
							if (playerTurn.character.ConsumeCloundRain())
								loseTurn = true;
						}
					}
				}
			}
			else
			{
				elapsedTimeNotMoving = 0f;
			}
		}
		else if (state == State.endScene)
		{
			fastForwardSkipTurn = false;
		}
		
		ready = true;
		lastFrameTime2 = lastFrameTime;
		lastFrameTime = time;
		
		HandleSpinnerInput();
		
		if (Time.timeScale == 1f)
			UpdateTouchBoard();
		else
			IgnoreAllTouchBoards();
	}
	
	public void IgnoreTouchBoardsInRect(Rect r)
	{
		if (Event.current.type != EventType.Repaint)
			return;
		ignoreRects.Add(r);
		
		r.y = Screen.height - r.y - r.height;
		for (int i = 0; i < queuedTouchBoardPoints.Count; i++)
			if (r.Contains(queuedTouchBoardPoints[i]))
				queuedTouchBoardPoints.RemoveAt(i--);
		
		// Ignore camera pan / zoom if you are pressing inside the rect, to avoid high-speeding the camera after pressing a button.
		if (Input.GetMouseButton(0))
		if (r.Contains(Input.mousePosition))
			BoardCamera.instance.IgnorePanZoomUntilRelease();
		for (int i = 0; i < Input.touchCount; i++)
			if (r.Contains(Input.GetTouch(i).position))
				BoardCamera.instance.IgnorePanZoomUntilRelease();
	}
	
	public bool IsInsideIgnoreRect(Vector2 point)
	{
		for (int i = 0; i < ignoreRects.Count; i++)
			if (ignoreRects[i].Contains(point))
				return true;
		return false;
	}
	
	void IgnoreAllTouchBoards()
	{
		queuedTouchBoardPoints.Clear();
		ignoreRects.Clear();
	}
	
	void UpdateTouchBoard()
	{
		for (int i = 0; i < queuedTouchBoardPoints.Count; i++)
			DoTouchBoard(queuedTouchBoardPoints[i]);
		queuedTouchBoardPoints.Clear();
		float timeForSingle = 0.15f;
		if (Input.touchCount == 0)
		{
			if (Input.GetMouseButtonDown(0))
				mouseDownTime = Time.realtimeSinceStartup;
			// handle single clicks only
			if (Time.realtimeSinceStartup - mouseDownTime < timeForSingle &&
				Input.GetMouseButtonUp(0))
				queuedTouchBoardPoints.Add(Input.mousePosition);
			// make sure there are no touch times if there are no touches active
			touchDownTime.Clear();
		}
		else
		{
			for (int i = 0; i < Input.touchCount; i++)
			{
				Touch t = Input.GetTouch(i);
				if (t.phase == TouchPhase.Began && !touchDownTime.ContainsKey(t.fingerId))
					touchDownTime.Add(t.fingerId, Time.realtimeSinceStartup);
				if (t.phase == TouchPhase.Ended && touchDownTime.ContainsKey(t.fingerId) && Time.realtimeSinceStartup - touchDownTime[t.fingerId] < timeForSingle)
					queuedTouchBoardPoints.Add(t.position);
			}
		}
		ignoreRects.Clear();
	}
	
	void DoTouchBoard(Vector2 point)
	{
		if (DualCamera.instance.Active)
			return;
		Ray r = Spinner.instance.usedCamera.ScreenPointToRay(point);
		RaycastHit hit;
		if (!Physics.Raycast(r, out hit, 1000f, spinnerMask))
		{
			r = BoardCamera.instance.Camera.ScreenPointToRay(point);
			if (Physics.Raycast(r, out hit, 1000f, boardItemsMask))
			{
				Clickanim a = hit.collider.GetComponent<Clickanim>();
				if (a)
					a.OnClicked();
				Character c = hit.collider.GetComponent<Character>();
				if (c)
					c.OnClicked();
			}
		}
	}
	
	bool AreAllCharactersReady()
	{
		foreach (Player p in Player.all)
			if (p.isPlaying && !p.character.Ready)
				return false;
		return true;
	}
	
	public void RequestNotReady()
	{
		ready = false;
	}
	
	void LoadSpinnerTable(Player p, bool allowUpdatingBonusCycling)
	{
		spinner.HideBonusAnim(allowUpdatingBonusCycling);
		Spinner.ProbabilityTable table = spinner.table;
		if (!p)
		{
			table.SetAll(0f);
			return;
		}
		table.SetAll(12.25f);
		table.Good = 12.25f;
		table.Bad = 12.25f;
		Character character = p.character;
		if (!character)
			return;
		Cell cell = character.CurrentCell;
		
		// If its on the start position, it cannot get special events.
		if (cell == null)
		{
			table.Good = 0f;
			table.Bad = 0f;
		}
		
		// Do not allow spring jump if there is no "isometric" cell found.
		if (cell)
		{
			if (cell.FindNextIsometricCell() == null)
			{
				float oldMagnitude = table.Good;
				table.Set(Spinner.Result.goodSpring, 0f);
				table.Good = oldMagnitude;
			}
		}
		
		// Only allow the trap door if i'm the winning player
		if (cell)
		{
			Player winningPlayer = FindWinningPlayer();
			if (winningPlayer != p || character.trapDoorCount > 0)
			{
				float oldMagnitude = table.Bad;
				table.Set(Spinner.Result.badTrapDoor, 0f);
				table.Bad = oldMagnitude;
			}
		}
		
		// Do not allow barking dog if i'm below cell number 10.
		if (cell == null || cell.number <= 10)
		{
			float oldMagnitude = table.Bad;
			table.Set(Spinner.Result.badBarkingDog, 0f);
			table.Bad = oldMagnitude;
		}
				
		// If i have a skateboard, i can only get numbers. No special events allowed.
		if (character.skate)
		{
			table.MultiplyGood(0f);
			table.MultiplyBad(0f);
		}
		
		// PROBABILITY TWEAK FOR GOOD EVENTS FOR WINNING PLAYERS:
		// If i am about to win, i'll get bigger chances of Bad events, and lower chances of Good events.
		if (cell && cell.number >= 85)
		{
			table.Good = 6.5f;
			table.Bad = 20f;
		}
		
		// A SPECIAL TWEAK FOR GOOD EVENTS FOR LOSING PLAYERS:
		// If i am at the beginning, and there is a player about to win, i'll get better chances of Good, and lower of Bad.
		if (cell == null || cell.number <= 15)
		{
			// Find the number of the occupied cell closest to the goal line.
			int closestNumber = 0;
			foreach (Player p2 in Player.all)
			{
				if (p2 && p2.isPlaying && p2.character)
				{
					Cell c = p2.character.CurrentCell;
					closestNumber = Mathf.Max(closestNumber, c ? c.number : 0);
				}
			}
			if (closestNumber >= 70)
			{
				table.Good = 20f;
				table.Bad = 6.5f;
			}
		}
	}
	
	private static Player FindWinningPlayer()
	{
		Player winningPlayer = null;
		foreach (Player player in Player.all)
		{
			if (player && player.isPlaying && player.character)
			{
				Cell cell = player.character.CurrentCell;
				if (cell && (winningPlayer == null || winningPlayer.character.CurrentCell.number < cell.number))
					winningPlayer = player;
			}
		}
		return winningPlayer;
	}
	
	private static Character FindMovingCharacter()
	{
		Character movingCharacter = null;
		foreach (Player player in Player.all)
		{
			if (player && player.isPlaying)
			{
				Character character = player.character;
				if (character && !character.IsIdle)
				{
					Cell cell = character.CurrentCell;
					if (cell && (movingCharacter == null || movingCharacter.CurrentCell.number < cell.number))
						movingCharacter = character;
				}
			}
		}
		return movingCharacter;
	}
	
	public void ResetMovePlayHintTime()
	{
		movePlayHintStartTime = Time.realtimeSinceStartup;
	}
	
	public void ResetHelloTime()
	{
		characterPlayHelloTime = Time.realtimeSinceStartup + Random.Range(10, 14);
	}

}
