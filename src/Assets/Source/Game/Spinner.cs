using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class handles the spinner behaviour and the probabilities table.
public class Spinner : MonoBehaviour
{
	public enum Result
	{
		unknown = -1,
		move1 = 0,
		move2 = 1,
		move3 = 2,
		move4 = 3,
		move5 = 4,
		move6 = 5,
		goodSpring = 6,
		goodBubbles = 7,
		goodSkateboard = 8,
		badRain = 9,
		badBarkingDog = 10,
		badTrapDoor = 11,
	}
		
	public static bool IsGoodResult(Result r)
	{
		int i = (int)r;
		return i >= 6 && i <= 8;
	}

	public static bool IsBadResult(Result r)
	{
		int i = (int)r;
		return i >= 9 && i <= 11;
	}
	
	public static bool IsSpecialResult(Result r)
	{
		return IsGoodResult(r) || IsBadResult(r);
	}
			
	public static int RESULT_NUMBER_MIN = 0;
	public static int RESULT_NUMBER_MAX = 5;
	public static int RESULT_GOOD_MIN = 6;
	public static int RESULT_GOOD_MAX = 8;
	public static int RESULT_BAD_MIN = 9;
	public static int RESULT_BAD_MAX = 11;
	public static int RESULT_COUNT = 12;
	
	[System.Serializable]
	public class ProbabilityTable
	{
		public float[] points = new float[RESULT_COUNT];

		public float Good
		{
			get
			{
				float total = 0f;
				for (int i = RESULT_GOOD_MIN; i <= RESULT_GOOD_MAX; i++)
					total += points[i];
				return total;
			}
			set
			{
				float t = Good;
				for (int i = RESULT_GOOD_MIN; i <= RESULT_GOOD_MAX; i++)
					points[i] = t == 0 ? 0 : value * points[i] / t;
			}
		}
		
		public float Bad
		{
			get
			{
				float total = 0f;
				for (int i = RESULT_BAD_MIN; i <= RESULT_BAD_MAX; i++)
					total += points[i];
				return total;
			}
			set
			{
				float t = Bad;
				for (int i = RESULT_BAD_MIN; i <= RESULT_BAD_MAX; i++)
					points[i] = t == 0 ? 0 : value * points[i] / t;
			}
		}

		public float Total
		{
			get
			{
				float total = 0f;
				for (int i = 0; i < RESULT_COUNT; i++)
					total += points[i];
				return total;
			}
			set
			{
				float t = Total;
				for (int i = 0; i < RESULT_COUNT; i++)
					points[i] = t == 0 ? 0 : value * points[i] / t;
			}
		}
		
		public float Get(Result r)
		{
			return points[(int)r];
		}
		
		public float Get(int r)
		{
			return points[r];
		}
		
		public void Set(Result r, float p)
		{
			points[(int)r] = p;
		}
				
		public void SetAll(float p)
		{
			for (int i = 0; i < RESULT_COUNT; i++)
				points[i] = p;
		}
				
		public void MultiplyGood(float p)
		{
			for (int i = RESULT_GOOD_MIN; i <= RESULT_GOOD_MAX; i++)
				points[i] *= p;
		}
		
		public void MultiplyBad(float p)
		{
			for (int i = RESULT_BAD_MIN; i <= RESULT_BAD_MAX; i++)
				points[i] *= p;
		}
		
		public void Normalize()
		{
			Total = 1f;
		}
		
		public Result GetResultForPoints(float p)
		{
			float accum = 0f;
			for (int i = 0; i < RESULT_COUNT; i++)
			{
				accum += points[i];
				if (p <= accum)
					return (Result)i;
			}
			return Result.unknown;
		}
		
	}
	
	public enum PositionType
	{
		center,
		hidden,
		side,
	}

	// The camera used to render the spinner.
	public Camera usedCamera;
	
	// The renderer used to draw the base of the spinner.
	// This is used to determine the bounds of the spinner.
	public Renderer usedRenderer;
	
	// Renderers that will be faded when the spinner is unavailable when using dual cameras.
	public List<Renderer> fadeRenderers = new List<Renderer>();
	
	// Transforms
	public Transform modelRoot;
	public Transform arrowRoot;
	public Transform tClose;
	public Transform tHidden;
	public Transform tSide;

	// good and bad cycling bonuses
	public GameObject goGoodDefault;
	public GameObject goGoodSpring;
	public GameObject goGoodBubbles;
	public GameObject goGoodSkateboard;
	public GameObject goBadDefault;
	public GameObject goBadRain;
	public GameObject goBadBarkingDog;
	public GameObject goBadTrapDoor;
	public AnimationCurve bonusCurve;
	public float bonusAnimDuration = 3;
	private float bonusAnimTimeLeft;
	public float bonusAnimSpeed = 1;
	private bool bonusAnimEnabled;
	public GameObject sparksStart;
	public GameObject sparksEnd;
	public bool bonusUpdate = true;

	// The probability table that will be used on next Spin.
	public ProbabilityTable table = new ProbabilityTable();
	public float usedTablePoints;
	public Result usedTableResult;
	
	// Spinner position
	public PositionType position = PositionType.center;
	public float timeToMove = 0.5f;
	public float catchSpeed = 10f;
	private int highSpeedFramesLeft;
	private Vector3 velocity;
	public Vector2 sidePosition = new Vector2(0.8f, 0.8f);
	
	// Arrow variables
	public float arrowCurrentAngle;
	public float arrowTargetAngle;
	public float arrowCatchSpeed = 1;
	public float arrowDragCatchSpeed = 10;
	public float arrowMaxSpeed = 2000;
	public float arrowMinSpeed = 40;
	public int numbers = 6;
	public float startAngle;
	public float angleDragDiffToStartSpinning = 15;
	public int maxSpins = 6;
	public float inputRadius = 1f;
	
	// Used to calculate the real delta time
	private float lastTime;
	
	// Chached references of all renderer's materials copies.
	private List<Material> materials = new List<Material>();

	// The singletone
	public static Spinner instance;
	
	void OnEnable()
	{
		instance = this;
	}
	
	public Bounds GetBounds()
	{
		return usedRenderer.bounds;
	}
	
	public bool HasArrived
	{
		get
		{
			// update position, scale and rotation
			Transform target = null;
			if (position == PositionType.center)
				target = tClose;
			else if (position == PositionType.side)
				target = tSide;
			else if (position == PositionType.hidden)
				target = tHidden;
			return target == null || Vector3.Distance(modelRoot.position, target.position) < 0.1f;
		}
	}
	
	void Start()
	{
		// make a copy of the materials
		foreach (Renderer r in fadeRenderers)
		{
			Material m = new Material(r.sharedMaterial);
			r.sharedMaterial = m;
			materials.Add(m);
		}
	}
	
	void OnDestroy()
	{
		foreach (Material m in materials)
			Material.DestroyImmediate(m);
		materials.Clear();
	}
	
	void Update()
	{
		// Update and calculate the real elapsed time
		float now = Time.realtimeSinceStartup;
		float dt = lastTime == 0f ? 0f : now - lastTime;
		if (Dev.FastForwardMenues || Dev.Cheats)
			dt *= 10000f;
		if (Time.timeScale > 1f)
			dt *= Time.timeScale;
		lastTime = now;

		// define the side position
		tSide.position = ScreenToWorldPosition(Screen.width * sidePosition.x, Screen.height * sidePosition.y, tSide.position);
		tHidden.position = ScreenToWorldPosition(Screen.width * 1.5f, Screen.height * 1.5f, tHidden.position);
		
		int oldSlot = GetSlotAtArrow();

		// update color
		Ingame.State ingameState = Ingame.instance.state;
		bool spinnerEnabled = Ingame.instance.PlayerTurn && Ingame.instance.PlayerTurn.isHuman &&
			(ingameState == Ingame.State.waitingSpinnerInput ||
			ingameState == Ingame.State.waitingSpinnerStop ||
			ingameState == Ingame.State.spinnerResultAdvice);
		if (!DualCamera.instance.Active)
			spinnerEnabled = true;
		foreach (Material m in materials)
			m.color = spinnerEnabled ? Color.white : StaticAssets.instance.disabledColor;
		
		// update position, scale and rotation
		Transform target = null;
		if (DualCamera.instance.Active)
			target = tClose;
		else if (position == PositionType.center)
			target = tClose;
		else if (position == PositionType.side)
			target = tSide;
		else if (position == PositionType.hidden)
			target = tHidden;
		if (dt != 0f && target)
		{
			modelRoot.position = Vector3.SmoothDamp(modelRoot.position, target.position, ref velocity, timeToMove, 100f, dt);
			modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, target.localScale, dt * catchSpeed);
			modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, target.rotation, dt * catchSpeed);
		}
		
		// instant move when usind dual camera to avoid visual glitches when switching airplay
		if (DualCamera.instance.Active && target)
		{
			modelRoot.position = target.position;
			modelRoot.localScale = target.localScale;
			modelRoot.rotation = target.rotation;
		}
		
		// update arrow angle
		float angleDiff = arrowTargetAngle - arrowCurrentAngle;
		float angleDeltaAbs = Mathf.Clamp(Mathf.Abs(angleDiff * arrowCatchSpeed), arrowMinSpeed, arrowMaxSpeed) * dt;
		if (highSpeedFramesLeft > 0)
			angleDeltaAbs = Mathf.Abs(angleDiff * arrowDragCatchSpeed) * dt;
		if (angleDeltaAbs >= Mathf.Abs(angleDiff))
			arrowCurrentAngle = arrowTargetAngle;
		else
			arrowCurrentAngle += angleDeltaAbs * Mathf.Sign(angleDiff);
		arrowRoot.localRotation = Quaternion.Euler(0, 0, -arrowCurrentAngle);
		
		// play a sound when the arrow changes slot
		if (GetSlotAtArrow() != oldSlot)
			Sound.Play(Sound.instance.fxArrowChangeSlot);
		
		// arrow high speed request update
		if (highSpeedFramesLeft > 0)
			highSpeedFramesLeft--;

		if (bonusUpdate)
			UpdateBonusCycling(dt);
	}

	int lastShowItem = 0;
	void UpdateBonusCycling(float dt)
	{
		if (bonusAnimEnabled && arrowTargetAngle == arrowCurrentAngle)
		{
			float lastTimeLeft = bonusAnimTimeLeft;
			float tk = Mathf.Clamp01(1 - bonusAnimTimeLeft / bonusAnimDuration);
			bonusAnimTimeLeft = Mathf.Max(bonusAnimTimeLeft - dt, 0);
			int itemOffset = 0;
			switch (usedTableResult)
			{
			case Result.goodBubbles:
				itemOffset = 0;
				break;
			case Result.goodSkateboard:
				itemOffset = 1;
				break;
			case Result.goodSpring:
				itemOffset = 2;
				break;
			case Result.badBarkingDog:
				itemOffset = 0;
				break;
			case Result.badRain:
				itemOffset = 1;
				break;
			case Result.badTrapDoor:
				itemOffset = 2;
				break;
			}
			
			int showItem = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(bonusCurve.Evaluate(tk) * bonusAnimSpeed + itemOffset - 1, 3)), 0, 2);
			if (lastTimeLeft == 0f) // exactly at time 0, show the target value
				showItem = itemOffset;

			// play sound on item change
			if (showItem != lastShowItem)
				Sound.Play(Sound.instance.fxBonusCycle);
			lastShowItem = showItem;
			
			GameObject sparksOrigin = null;
			if (IsGoodResult(usedTableResult))
			{
				sparksOrigin = goGoodDefault;
				goBadDefault.SetActive(true);
				goBadBarkingDog.SetActive(false);
				goBadRain.SetActive(false);
				goBadTrapDoor.SetActive(false);
				goGoodDefault.SetActive(false);
				goGoodBubbles.SetActive(showItem == 0);
				goGoodSkateboard.SetActive(showItem == 1);
				goGoodSpring.SetActive(showItem == 2);
			}
			else if (IsBadResult(usedTableResult))
			{
				sparksOrigin = goBadDefault;
				goBadDefault.SetActive(false);
				goBadBarkingDog.SetActive(showItem == 0);
				goBadRain.SetActive(showItem == 1);
				goBadTrapDoor.SetActive(showItem == 2);
				goGoodDefault.SetActive(true);
				goGoodBubbles.SetActive(false);
				goGoodSkateboard.SetActive(false);
				goGoodSpring.SetActive(false);
			}
			else
			{
				goBadDefault.SetActive(true);
				goBadBarkingDog.SetActive(false);
				goBadRain.SetActive(false);
				goBadTrapDoor.SetActive(false);
				goGoodDefault.SetActive(true);
				goGoodBubbles.SetActive(false);
				goGoodSkateboard.SetActive(false);
				goGoodSpring.SetActive(false);
			}
			
			// spawn sparks
			if (lastTimeLeft >= bonusAnimDuration && bonusAnimTimeLeft < bonusAnimDuration && sparksOrigin && sparksStart)
			{
				GameObject go = (GameObject)GameObject.Instantiate(sparksStart, sparksOrigin.transform.position, Quaternion.identity);
				go.transform.parent = sparksOrigin.transform.parent;
				go.transform.localPosition = sparksStart.transform.localPosition;
				go.transform.localRotation = sparksStart.transform.localRotation;
				go.transform.localScale = sparksStart.transform.localScale;
			}
			if (lastTimeLeft > 0 && bonusAnimTimeLeft == 0 && sparksOrigin && sparksEnd)
			{
				GameObject go = (GameObject)GameObject.Instantiate(sparksEnd, sparksOrigin.transform.position, Quaternion.identity);
				go.transform.parent = sparksOrigin.transform.parent;
				go.transform.localPosition = sparksStart.transform.localPosition;
				go.transform.localRotation = sparksStart.transform.localRotation;
				go.transform.localScale = sparksStart.transform.localScale;
			}
		}
		else
		{
			goBadDefault.SetActive(true);
			goBadBarkingDog.SetActive(false);
			goBadRain.SetActive(false);
			goBadTrapDoor.SetActive(false);
			goGoodDefault.SetActive(true);
			goGoodBubbles.SetActive(false);
			goGoodSkateboard.SetActive(false);
			goGoodSpring.SetActive(false);
		}
	}
	
	Vector3 ScreenToWorldPosition(float x, float y, Vector3 center)
	{
		Ray ray = usedCamera.ScreenPointToRay(new Vector3(x, y, 0));
		Plane plane = new Plane(Vector3.forward, center);
		float enter = 0f;
		if (plane.Raycast(ray, out enter))
			return ray.GetPoint(enter);
		return Vector3.zero;
	}
	
	public bool Moving
	{
		get
		{
			return arrowTargetAngle != arrowCurrentAngle || (bonusUpdate && bonusAnimEnabled && bonusAnimTimeLeft != 0);
		}
	}
	
	private int GetSlotForResult(Result r)
	{
		int i = (int)r;
		if (i >= RESULT_NUMBER_MIN && i <= RESULT_NUMBER_MAX)
			return (int)r;
		if (i >= RESULT_GOOD_MIN && i <= RESULT_GOOD_MAX)
			return 6;
		if (i >= RESULT_BAD_MIN && i <= RESULT_BAD_MAX)
			return 7;
		return 0;
	}
	
	public void Spin()
	{
		Spin(Random.Range(2, 4));
	}
	
	public void Spin(int spins)
	{
		spins = Mathf.Clamp(spins, -maxSpins, maxSpins);
		usedTablePoints = Random.Range(0f, table.Total);
		usedTableResult = table.GetResultForPoints(usedTablePoints);
		int targetSlot = GetSlotForResult(usedTableResult);
		arrowCurrentAngle = Mathf.Repeat(arrowCurrentAngle, 360);
		float randomSpin = 360 * spins;
		if (spins < 0)
			randomSpin -= 360;
		arrowTargetAngle = randomSpin + startAngle + AnglePerSlot * (targetSlot + 0.5f);
		highSpeedFramesLeft = 0;
		bonusAnimEnabled = IsSpecialResult(usedTableResult);
		bonusAnimTimeLeft = bonusAnimEnabled ? bonusAnimDuration : 0f;
	}
	
	public void Spin(Result target)
	{
		table.SetAll(0f);
		table.Set(target, 1f);
		table.Normalize();
		Spin(1);
		highSpeedFramesLeft = 0;
		bonusAnimEnabled = false;
		bonusAnimTimeLeft = 0f;
	}
	
	public float AnglePerSlot
	{
		get
		{
			return 360 / numbers;
		}
	}
	
	public int GetSlotAtArrow()
	{
		return (int)((Mathf.Repeat(arrowCurrentAngle - startAngle + 360, 360)) / AnglePerSlot);
	}
	
	public Result GetResult()
	{
		return usedTableResult;
	}

	public int GetMoves()
	{
		Result r = usedTableResult;
		int i = (int)r;
		if (i >= RESULT_NUMBER_MIN && i <= RESULT_NUMBER_MAX)
			return (int)r + 1;
		return 0;
	}
	
	public void RequestHighSpeed()
	{
		highSpeedFramesLeft = 2;
	}
	
	public void Restore()
	{
		position = PositionType.hidden;
		arrowCurrentAngle = 90;
		arrowTargetAngle = 90;
		bonusAnimEnabled = false;
	}

	public void HideBonusAnim(bool updateBonusCycling)
	{
		bonusUpdate = updateBonusCycling;
		bonusAnimEnabled = false;
	}

}
