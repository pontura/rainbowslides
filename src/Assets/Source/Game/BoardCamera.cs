using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class handles the board camera behaviour.
// It can focus ("center on") a specific target (usually set by Ingame).
public class BoardCamera : MonoBehaviour
{
	// Allow zooming in / out?
	public bool allowFocusing = true;
	
	// Min and Max orthographic size relative to rest size.
	public float minZoom = 0.5f;
	public float maxZoom = 1.0f;
	
	// The orthographic size used when focusing a character.
	public float focusSize = 5f;

	// 0 to 1: 0 means 100% on rest position, 1 means 100% on focus position.
	public float focusIntensity = 0.5f;
	
	// The speed at which the camera tries to catch the target position.
	public float positionCatchSpeed = 10f;
	
	// The speed at which the camera size is adjusted.
	public float sizeCatchTime = 1f;
	private float sizeVelocity;

	// Sensitivity used to change the orthographic size value with the mouse scrollwheel.
	public float mouseScrollWheelSpeed = 5f;
	
	// The sensitivity used to calculate the amount of orthographic size change when using pinch zoom.
	public float touchPinchSensitivity = 5f;
	
	// The last mouse position used to calculate the mouse delta for panning.
	private Vector3 lastMousePosition;
	
	// The last redius vector of the smallest circle that contains all touches. This is used to calculate the pinch zoom.
	private Vector2 lastTouchPinchDelta;

	// Time left using hight catch speed. This is used while manually panning and zooming.
	private float highSpeedTimeLeft;
	private float highSpeedTimeMax = 0.3f;
	
	// The high speed velocity multiplier.
	public float highSpeedMultiplier = 10f;
	
	// The current end values that will be smoothly reached. 
	public Vector3 targetPosition;
	public float targetSize;

	// Rest values.
	public float restSize;
	
	// The touch positions of the previous frame. This is used to calculate the delta position between frames.
	// This is a workaround for the issue: Touch.deltaPosition returns an incorrect value when using multiple touches.
	private Dictionary<int, Vector2> lastFingerPosition = new Dictionary<int, Vector2>();

	// This flag is used to ignore the user input for camera pan / zoom.
	// Its used to avoid high-speeding the camera after pressing a button.
	private bool ignorePanZoomUntilRelease;
	
	// The target position that will be set when the game starts.
	public Transform RestPosition
	{
		get
		{
			return BoardCenter.instance ? BoardCenter.instance.transform : null;
		}
	}
	
	// Cached references.
	private Transform _t;
	private Camera _camera;
	private CameraSkew _skew;
	public static BoardCamera instance;
	
	public Camera Camera
	{
		get { return _camera; }
	}

	public CameraSkew Skew
	{
		get { return _skew; }
	}

	void OnEnable()
	{
		instance = this;
	}
	
	void OnDisable()
	{
		if (instance == this)
			instance = null;
	}
	
	void Start()
	{
		_t = transform;
		_camera = GetComponent<Camera>();
		_skew = _camera.GetComponent<CameraSkew>();
		if (restSize == 0)
			restSize = _camera.orthographicSize;
	}
	
	void Update()
	{
		if (GameGUI.State != GUIState.ingame || !_camera.enabled)
			return;
		
		float deltaTime = Time.deltaTime;
		if (highSpeedTimeLeft > 0)
		{
			highSpeedTimeLeft = Mathf.Max(highSpeedTimeLeft - deltaTime, 0);
			deltaTime = deltaTime + highSpeedMultiplier * deltaTime * highSpeedTimeLeft / highSpeedTimeMax;
		}
		
		// Update size
		if (deltaTime != 0f)
			_camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, targetSize, ref sizeVelocity, sizeCatchTime, Mathf.Infinity, deltaTime);
		_skew.Update();
		
		// Update position
		Vector3 pos = _t.position;
		pos += (targetPosition - pos) * Mathf.Clamp01(positionCatchSpeed * deltaTime);
		_t.position = pos;
		_skew.Update();
		
		// clamp position
		BoardInfo info = BoardInfo.instance;
		if (info)
		{
			Vector3 minPos = WorldToCameraPosition(info.minCameraPosition);
			Vector3 maxPos = WorldToCameraPosition(info.maxCameraPosition);
			Vector3 currentScreenMinPos = ScreenToCameraPosition(Vector3.zero);
			Vector3 currentScreenMaxPos = ScreenToCameraPosition(new Vector3(DualCamera.GetWidth(1), DualCamera.GetHeight(1), 0));
			if (currentScreenMaxPos.x - currentScreenMinPos.x > maxPos.x - minPos.x)
			{
				pos.x += ((minPos.x - currentScreenMinPos.x) + (maxPos.x - currentScreenMaxPos.x)) / 2;
				targetPosition.x = pos.x;
			}
			else if (currentScreenMinPos.x < minPos.x)
			{
				pos.x += minPos.x - currentScreenMinPos.x;
				targetPosition.x = Mathf.Max(targetPosition.x, pos.x);
			}
			else if (currentScreenMaxPos.x > maxPos.x)
			{
				pos.x += maxPos.x - currentScreenMaxPos.x;
				targetPosition.x = Mathf.Min(targetPosition.x, pos.x);
			}
			if (currentScreenMaxPos.y - currentScreenMinPos.y > maxPos.y - minPos.y)
			{
				pos.y += ((minPos.y - currentScreenMinPos.y) + (maxPos.y - currentScreenMaxPos.y)) / 2;
				targetPosition.y = pos.y;
			}
			else if (currentScreenMinPos.y < minPos.y)
			{
				pos.y += minPos.y - currentScreenMinPos.y;
				targetPosition.y = Mathf.Max(targetPosition.y, pos.y);
			}
			else if (currentScreenMaxPos.y > maxPos.y)
			{
				pos.y += maxPos.y - currentScreenMaxPos.y;
				targetPosition.y = Mathf.Min(targetPosition.y, pos.y);
			}
		}
		_t.position = pos;
		_skew.Update();
	}
	
	private Vector3 WorldToCameraPosition(Vector3 worldPosition)
	{
		return ScreenToCameraPosition(_camera.WorldToScreenPoint(worldPosition));
	}
	
	public Vector3 ScreenToCameraPosition(Vector3 screenPosition)
	{
		return ScreenToCameraPosition(screenPosition, 0f);
	}
	
	public Vector3 ScreenToCameraPosition(Vector3 screenPosition, float extraDistance)
	{
		Ray r = _camera.ScreenPointToRay(screenPosition);
		Plane p = new Plane(Vector3.forward, _t.position);
		float e;
		if (p.Raycast(r, out e))
			return r.GetPoint(e + extraDistance);
		else
			return Vector3.zero;
	}
	
	public void ResetView()
	{
		// go back to rest position and rest size
		if (RestPosition)
			targetPosition = RestPosition.position;
		targetSize = restSize;
	}
	
	public void SetTarget(Vector3 worldPosition, float zoom, bool onlyZoomIn)
	{
		float fk = Mathf.Clamp01(1 - targetSize / restSize);
		worldPosition = Vector3.Lerp(RestPosition.position, worldPosition, Mathf.Clamp01(focusIntensity * fk));
		targetPosition = WorldToCameraPosition(worldPosition);
		if (zoom != 0f)
		{
			float newTargetSize = focusSize * (1f / zoom);
			if (!onlyZoomIn || targetSize > newTargetSize)
				targetSize = newTargetSize;
		}
		
		// ignore manual camera move while setting target
		IgnorePanZoomUntilRelease();
	}
	
	public bool IsInsideViewport(Vector3 position)
	{
		Vector3 vp = _camera.WorldToViewportPoint(position);
		return vp.x > 0.1f && vp.x < 0.9f && vp.y > 0.1f && vp.y < 0.9f;
	}
	
	public void Restore()
	{
		_t.position = RestPosition.position;
		_camera.orthographicSize = restSize;
	}
	
	public void IgnorePanZoomUntilRelease()
	{
		ignorePanZoomUntilRelease = true;
	}

	public void DoUpdateCameraInput(bool canPanZoom)
	{
		Vector2 touchPinchDelta = Vector2.zero;
		if (canPanZoom && !ignorePanZoomUntilRelease)
		{
			// no touches... try with mouse
			if (Input.touchCount == 0)
			{
				// left click and drag to pan
				if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0))
				{
					Vector3 a = ScreenToCameraPosition(lastMousePosition);
					Vector3 b = ScreenToCameraPosition(Input.mousePosition);
					targetPosition -= b - a;
					HighSpeedRequest();
				}
				
				float delta = Input.GetAxisRaw("Mouse ScrollWheel") * mouseScrollWheelSpeed;
				ClampTargetSize(-delta, (Vector2)Input.mousePosition, !DualCamera.instance.Active);
				if (delta != 0)
					HighSpeedRequest();
			}
			// touches found... ignore mouse
			else
			{
				// zoom
				if (Input.touchCount > 1)
				{
					// calculate the center
					Vector2 center = Vector2.zero;
					for (int i = 0; i < Input.touchCount; i++)
					{
						Touch t = Input.GetTouch(i);
						center += t.position;
					}
					center /= Input.touchCount;
					
					// calculate the radius
					for (int i = 0; i < Input.touchCount; i++)
					{
						Touch t = Input.GetTouch(i);
						Vector2 delta = t.position - center;
						delta.x /= Screen.width;
						delta.y /= Screen.height;
						if (delta.sqrMagnitude > touchPinchDelta.sqrMagnitude)
							touchPinchDelta = delta;
					}
					
					if (lastTouchPinchDelta.sqrMagnitude != 0f)
					{
						float delta = touchPinchDelta.magnitude - lastTouchPinchDelta.magnitude;
						if (delta != 0f)
						{
							ClampTargetSize(-delta * touchPinchSensitivity * _camera.orthographicSize, center, !DualCamera.instance.Active);
							HighSpeedRequest();
						}
					}
				}
				
				// pan
				if (Input.touchCount > 0)
				{
					Vector3 delta = Vector2.zero;
					int deltaCount = 0;
					for (int i = 0; i < Input.touchCount; i++)
					{
						Touch t = Input.GetTouch(i);
						if (lastFingerPosition.ContainsKey(t.fingerId))
						{
							Vector2 lastPos = lastFingerPosition[t.fingerId];
							Vector3 a = ScreenToCameraPosition(lastPos);
							Vector3 b = ScreenToCameraPosition(t.position);
							delta += b - a;
							deltaCount++;
						}
					}
					if (deltaCount != 0)
						delta /= deltaCount;
					targetPosition -= delta;
					HighSpeedRequest();					
				}
			}
		}
		
		// save the finger id touch positions... (deltaPosition doesn't seems to return the correct value)
		lastFingerPosition.Clear();
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch t = Input.GetTouch(i);
			if (!lastFingerPosition.ContainsKey(t.fingerId))
				lastFingerPosition.Add(t.fingerId, t.position);
		}
		
		lastMousePosition = Input.mousePosition;
		lastTouchPinchDelta = touchPinchDelta;
		
		// update the ignore input flag
		if (ignorePanZoomUntilRelease && !Input.GetMouseButton(0) && Input.touchCount == 0)
			ignorePanZoomUntilRelease = false;
	}
	
	void ClampTargetSize(float sizeChange, Vector2 screenPosition, bool followPan)
	{
		float oldSize = targetSize;
		targetSize = Mathf.Clamp(targetSize + sizeChange, restSize * minZoom, restSize * maxZoom);
		if (followPan)
		{
			Vector3 screenWorldPosition = ScreenToCameraPosition(screenPosition);
			Vector3 middleScreenWorldPosition = ScreenToCameraPosition(new Vector3(DualCamera.GetWidth(1) / 2, DualCamera.GetHeight(1) / 2, 0));
			Vector3 screenDelta = screenWorldPosition - middleScreenWorldPosition;
			float deltaSize = targetSize - oldSize;
			targetPosition -= deltaSize * screenDelta / _camera.orthographicSize;
		}
	}
	
	void HighSpeedRequest()
	{
		highSpeedTimeLeft = highSpeedTimeMax;
	}
	
	public float GetZoom()
	{
		return restSize / _camera.orthographicSize;
	}
	
}
