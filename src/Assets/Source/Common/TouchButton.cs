using UnityEngine;
using System.Collections;

// This class emulates the behaviour of a button, handling the Input touches insted of mouse clicks.
// Handling mouse clicks with the traditional GUI.Button has the issue of interpreting clicks as the average of all touches.
// If you touch the screen with multiple fingers, the GUI.Button will not respond properly.
public class TouchButton
{
	// The screen rect that will handle touches.
	public Rect logicRect;
	
	// The screen rect used to draw the box.
	public Rect guiRect;
	
	// Is it being pressed? (any touch on it)
	public bool isPressed;
	
	// Is the first frame is was pressed?
	public bool down;
	
	// Was a single press done on this frame?
	public bool singlePress;
	
	// Is it being holded? (it must be pressed and holded for some time until this returns true)
	public bool holded;
	
	// Was released on this frame?
	public bool released;
	
	// The touched position. The value has meaning only while isPressed.
	public Vector2 touchPosition;
	
	// The current touch that started pressing the button.
	private int touchPressId = NONE_ID;
	
	// The real time when the button started to be pressed.
	private float touchStartTime = 0f;
	
	// Used to detect mouse down inside GUI.
	private bool lastMouseButton;
	
	// After a touch has been detected, the mouse input will be ignored.
	// If you use 2 touches, the center of the touches will be reported as an Input mouse click.
	// This behaviour can lead to usability issues.
	// There is no way to distinguish touches from mouse input.
	// That's why Input is ignored after a touch has been detected.
	public static bool ignoreMouseInput;
	
	// An unique ID to emulate the mouse as a touch.
	private static int MOUSE_ID = -999;
	private static int NONE_ID = -1;
	
	// If you press and release the button inside this gap of time, a single press will be reported.
	public static float SINGLE_PRESS_TIME = 0.2f;
	
	public void DoGUI(Rect _rect, GUIContent content, GUIStyle style)
	{
		DoGUI(_rect, _rect, content, style, true);
	}
	
	public void DoGUI(Rect _guiRect, Rect _logicRect, GUIContent content, GUIStyle style, bool canPress)
	{
		guiRect = _guiRect;
		logicRect = _logicRect;
		if (content != null)
			GUI.Box(guiRect, content, style);
		if (Event.current.type == EventType.Repaint)
			Update(canPress);
	}
	
	public void DoGUI(Rect _guiRect, Rect _logicRect, Texture texture, ScaleMode scaleMode, bool canPress)
	{
		guiRect = _guiRect;
		logicRect = _logicRect;
		if (texture != null)
			GUI.DrawTexture(guiRect, texture, scaleMode, true);
		if (Event.current.type == EventType.Repaint)
			Update(canPress);
	}
	
	public void Update(bool canPress)
	{
		bool wasPressed = isPressed;
		bool mouseButton = Input.GetMouseButton(0);
		
		// reset the single press flag
		singlePress = false;
		touchPosition = Vector2.zero;
		
		// If any touch has been detected, ignore mouse input.
		ignoreMouseInput |= Input.touchCount != 0;
		
		// No touch has started pressing the button yet... search for touches inside.
		if (touchPressId == NONE_ID)
		{
			if (canPress)
			{
				// Check mouse input.
				if (!ignoreMouseInput)
				{
					if (mouseButton && !lastMouseButton) // GetMouseButtonDown always reports false inside OnGUI
					{
						BeginTouch(MOUSE_ID, Flip(Input.mousePosition));
					}
				}
				// Check touches.
				for (int i = 0; i < Input.touchCount; i++)
				{
					Touch t = Input.GetTouch(i);
					if (t.phase != TouchPhase.Began)
						continue;
					BeginTouch(t.fingerId, Flip(t.position));
				}
			}
		}
		// The button is being touched. Wait until it releases.
		else
		{
			// Is it a mouse click?
			if (touchPressId == MOUSE_ID)
			{
				if (mouseButton)
				{
					touchPosition = Flip(Input.mousePosition);
				}
				else
				{
					EndTouch(MOUSE_ID, Flip(Input.mousePosition));
				}
			}
			// It's a touch.
			else
			{
				bool currentTouchFound = false;
				// Check touches.
				for (int i = 0; i < Input.touchCount; i++)
				{
					Touch t = Input.GetTouch(i);
					if (t.fingerId == touchPressId)
					{
						currentTouchFound = true;
						touchPosition = Flip(t.position);
					}
					if (t.phase != TouchPhase.Ended)
						continue;
					EndTouch(t.fingerId, Flip(t.position));
				}
				// In case the touch has disappeared (may happen if you lose focus of the app and then you stop touching))
				if (!currentTouchFound)
					touchPressId = NONE_ID; // end touch without checking for release
			}
		}
		
		// update flags
		isPressed = touchPressId != NONE_ID;
		holded = isPressed && !singlePress && Time.realtimeSinceStartup > touchStartTime + SINGLE_PRESS_TIME;
		released = wasPressed && !isPressed;
		down = !wasPressed && isPressed;
		lastMouseButton = mouseButton;
	}
	
	public static Vector2 Flip(Vector2 position)
	{
		position.y = Screen.height - position.y;
		return position;
	}
	
	bool BeginTouch(int id, Vector2 position)
	{
		if (touchPressId != NONE_ID)
			return false;
		if (!logicRect.Contains(position))
			return false;
		touchPressId = id;
		touchStartTime = Time.realtimeSinceStartup;
		touchPosition = position;
		return true;
	}
	
	void EndTouch(int id, Vector2 position)
	{
		if (touchPressId != id)
			return;
		if (logicRect.Contains(position))
			singlePress = Time.realtimeSinceStartup <= touchStartTime + SINGLE_PRESS_TIME;
		touchPressId = NONE_ID;
		touchPosition = position;
	}
	
	public void Reset()
	{
		touchPressId = NONE_ID;
		isPressed = false;
		holded = false;
		released = false;
		down = false;
		singlePress = false;
	}
	
}
