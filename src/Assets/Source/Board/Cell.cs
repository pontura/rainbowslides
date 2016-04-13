using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Cell : MonoBehaviour
{
	public enum ConnectionType
	{
		none,
		ladder,
		chute,
	}
	
	// The cell number 1 (there should be only one).
	public static Cell firstCell;
	
	// The cell's number.
	public int number;
	
	// If set, this position will be used when a characters moves appart to let the other pass.
	public Vector3 customSidePosition;
	
	// The next cell.
	public Cell next;
	
	// The previous cell.
	public Cell previous;
	
	// If its a chute or ladder, the cell where its connected to (the end).
	public Cell connection;

	// The root where the connection model is located. The renderers will be enabled only if the connection is active.
	public GameObject connectionModel;
	
	// The transform where the cell model is located.
	public Transform modelRoot;
	
	// The transform where the number renderer is located.
	public Transform numberRoot;

	// If set, this cell is an end point of a connection. A cell cannot be the end of more than one cell.
	private Cell _connectedTo;
	
	// The initial position (required to restore values after an animation).
	private Vector3 startPosition;
	private Quaternion startRotation;
	private Vector3 startScale;
	
	// Cached references.
	private Renderer _renderer;
	[HideInInspector]
	public Transform _t;
	
	// Routines safety flags.
	private bool hideMeshesRoutineRunning;
	private bool springEffectRoutineRunning;
	
	// The characters that are currently stepping this cell.
	// This must be handled by the characters.
	[HideInInspector]
	public List<BoardEntity> entities = new List<BoardEntity>();
	
	// The list of cells that currently exists on the scene.
	public static List<Cell> all = new List<Cell>();
	
	// The cell size.
	public static float SIZE
	{
		get
		{
			BoardAssets board = BoardAssets.Current;
			if (board)
				return board.cellSize;
			Debug.LogError("Cell size needs an active board art.");
			return 1f;
		}
	}
	
	public static Cell FindByNumber(int number)
	{
		foreach (Cell c in all)
			if (c.number == number)
				return c;
		return null;
	}
	
	public Renderer GetModelRenderer()
	{
		if (_renderer == null && modelRoot)
			_renderer = modelRoot.GetComponent<Renderer>();
		return _renderer;
	}

	// The position where the character must move to let another character pass.
	public Vector3 SidePosition
	{
		get
		{
			if (customSidePosition != Vector3.zero)
				return _t.position + customSidePosition * SIZE * 0.5f;
			Vector3 nextDirection = NextDirection;
			Vector3 sideDir = Vector3.forward;
			if (Mathf.Abs(nextDirection.z) < 0.5f)
				sideDir = Vector3.forward;
			else
			{
				Vector3 prevDir = PreviousDirection;
				if (Mathf.Abs(nextDirection.z) < 0.5f)
					sideDir = -prevDir;
				else
					sideDir = number % 2 == 0 ? Vector3.right : Vector3.left;
			}
			return _t.position + sideDir * SIZE * 0.5f;
		}
	}
	
	// The direction pointing to the previous cell.
	public Vector3 PreviousDirection
	{
		get
		{
			if (previous)
				return (previous._t.position - _t.position).normalized;
			else
				return (StartPlace.Center - _t.position).normalized;
		}
	}
	
	// The direction pointing to the next cell.
	public Vector3 NextDirection
	{
		get
		{
			if (next)
				return (next._t.position - _t.position).normalized;
			else
				return -PreviousDirection;
		}
	}
	
	// The current cell's push position. Where the character must be located to perform the push animation.
	public Vector3 ToPushPosition
	{
		get
		{
			return _t.position - PreviousDirection * SIZE * 0.5f;
		}
	}
	
	// The current cell's pushed start position. Where the character must be located to start playing the pushed animation.
	public Vector3 ToBePushedPosition
	{
		get
		{
			return _t.position;
		}
	}
	
	// The current cell's pushed end position. Where the character will move while playing the pushed animation.
	public Vector3 ToPushedPosition
	{
		get
		{
			return _t.position + NextDirection * SIZE * 0.5f;
		}
	}
	
	// The cell where this cell connects to. This is used by chutes and ladders.
	private Cell ConnectedTo
	{
		get
		{
			return _connectedTo;
		}
		set
		{
			if (value != _connectedTo)
			{
				if (_connectedTo != null && value != null)
				{
					Debug.LogError("The cell is already a connection end point of " + _connectedTo.name, this);
				}
				else
				{
					_connectedTo = value;
				}
			}
		}
	}
	
	// Its a chute or ladder?
	public ConnectionType GetConnectionType()
	{
		if (connection == null)
			return ConnectionType.none;
		return connection.number > number ? ConnectionType.ladder : ConnectionType.chute;
	}
	
	void Awake()
	{
		_t = transform;
	}

	void OnEnable()
	{
		if (firstCell == null || this.number < firstCell.number)
			firstCell = this;
		all.Add(this);
		startPosition = _t.localPosition;
		startRotation = _t.localRotation;
		startScale = _t.localScale;
		_renderer = GetModelRenderer();
	}
	
	void OnDisable()
	{
		all.Remove(this);
	}
	
	public void LoadConnectionModel(Cell connectedCell, GameObject modelPrefab)
	{
		DestroyConnection();
		connection = connectedCell;
		connectionModel = (GameObject)GameObject.Instantiate(modelPrefab, _t.position, _t.rotation);
		connectionModel.transform.position += modelPrefab.transform.position;
	}
	
	public void DestroyConnection()
	{
		connection = null;
		if (connectionModel)
		{
			GameObject.Destroy(connectionModel);
			connectionModel = null;
		}
	}
	
	// Use this to fix some common errors after editting cells on the scene.
	[ContextMenu("Fix")]
	void Fix()
	{
		BoardAssets board = BoardAssets.Current;
		
		// fix the name
		name = "cell " + number;
		
		// fix position
		if (previous != null)
		{
			float cellSize = SIZE;
			
			// snap into the correct position
			Vector3 direction = transform.position - previous.transform.position;
			direction.y = 0;
			if (direction.sqrMagnitude < Mathf.Epsilon)
			{
				Debug.LogError("cell at the same position", this);
			}
			else
			{
				direction = MakeNormalizedPlanar(direction);
				transform.position = previous.transform.position + direction * cellSize;
			}
			// fix the number
			number = previous.number + 1;
		}
		
		// destroy all children
		List<Transform> childs = new List<Transform>();
		foreach (Transform t2 in transform)
			childs.Add(t2);
		foreach (Transform t2 in childs)
			GameObject.DestroyImmediate(t2.gameObject, false);
		
		// create the model root
		GameObject goModel = new GameObject("model");
		modelRoot = goModel.transform;
		modelRoot.parent = transform;
		modelRoot.localPosition = Vector3.zero;
		modelRoot.localRotation = Quaternion.identity;
		modelRoot.localScale = Vector3.one;
		goModel.AddComponent<MeshFilter>().sharedMesh = board.cellFull;
		goModel.AddComponent<MeshRenderer>();

		if (board.cellTextPrefab)
		{
			GameObject textGO = board.cellTextPrefab;
			GameObject go = (GameObject)GameObject.Instantiate(textGO, transform.position, transform.rotation);
			if (go)
			{
				go.name = "number";
				numberRoot = go.transform;
				numberRoot.parent = modelRoot;
				go.transform.localPosition = textGO.transform.localPosition;
				go.transform.localRotation = textGO.transform.localRotation;
				go.transform.localScale = textGO.transform.localScale;
				go.transform.position += Vector3.up * 0.01f;
				TextMesh tm = go.GetComponent<TextMesh>();
				if (tm)
					tm.text = number.ToString();
			}
		}
	}
	
	// An easy way to create many cells.
	[ContextMenu("Create Cell x10")]
	void CreateCellxTen()
	{
		Cell cell = this;
		for (int i = 0; i < 10; i++)
		{
			cell = cell.CreateCell();
			if (cell == null)
				return;
		}
	}

	[ContextMenu("Create Cell")]
	Cell CreateCell()
	{
		Transform t = transform;
		GameObject go = (GameObject)GameObject.Instantiate(gameObject, t.position, t.rotation);
		
		Vector3 direction = Random.onUnitSphere;
		if (previous)
		{
			direction = t.position - previous.transform.position;
			direction.Normalize();
		}
		direction.y = 0;
		if (direction.sqrMagnitude < Mathf.Epsilon)
			direction = Random.onUnitSphere;

		direction = MakeNormalizedPlanar(direction);
		
		go.transform.position = go.transform.position + direction * SIZE;
		Cell copy = go.GetComponent<Cell>();
		if (copy)
		{
			next = copy;
			copy.previous = this;
			copy.next = null;
			copy.connection = null;
			copy.number = number + 1;
			copy.transform.parent = t.parent;
			copy.Fix();
			return copy;
		}
		return null;
	}
	
	private static Vector3 MakeNormalizedPlanar(Vector3 v)
	{
		v.y = 0;
		if (v.sqrMagnitude < Mathf.Epsilon)
			return Vector3.zero;
		if (Mathf.Abs(v.x) > Mathf.Abs(v.z))
		{
			v.x = Mathf.Sign(v.x);
			v.z = 0;
		}
		else
		{
			v.x = 0;
			v.z = Mathf.Sign(v.z);
		}
		return v;
	}
	
	// Fixes all cells. This is usefull when you are editing the cells and you want to make sure they are aligned.
	[ContextMenu("Fix All")]
	void FixAll()
	{
		Cell cell = this;
		Cell previous = null;
		List<Cell> cells = new List<Cell>();
		
		int safetyCount = 0;
		bool startFound = false;
		while (true)
		{
			// avoid infinite loop hang
			if (safetyCount++ > 999)
			{
				Debug.LogError("safety loop check!");
				break;
			}
			
			// stop iterating at the end of the path
			if (cell == null)
				break;

			// iterate backwards until finding the first cell
			if (!startFound)
			{
				if (cell.previous == null)
				{
					startFound = true;
				}
				else
				{
					cell = cell.previous;
					continue;
				}
			}
			
			// detect looping paths and abort
			if (cells.Contains(cell))
			{
				Debug.LogWarning("loop found!", cell);
				break;
			}
			cells.Add(cell);
			
			// fix the previous reference
			cell.previous = previous;
			
			// fix it
			cell.Fix();
			
			// advance to the next cell
			previous = cell;
			cell = cell.next;
		}
	}
	
	void OnDrawGizmos()
	{
		if (connection)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawCube(connection.transform.position, new Vector3(0.2f, 3f, 0.2f));
			Gizmos.color = Color.green;
			Gizmos.DrawCube(transform.position, new Vector3(0.5f, 1f, 0.5f));
			Gizmos.DrawLine(transform.position, connection.transform.position);
		}
	}
	
	public void RefreshPosition(float down)
	{
		Vector3 position = startPosition;
		if (down > 0f)
			position += Vector3.down * down;
		_t.localPosition = position;
		_t.localRotation = startRotation;
		_t.localScale = startScale;
	}
	
	[ContextMenu("Refresh Material")]
	public void RefreshMaterial()
	{
		RefreshMaterialInternal(GetModelRenderer(), false);
	}

	public void RefreshMaterial(bool bright)
	{
		RefreshMaterialInternal(_renderer, bright);
	}
	
	private void RefreshMaterialInternal(Renderer r, bool bright)
	{
		BoardAssets assets = BoardAssets.Current;
		
		// one color for all cells... except for chutes and ladders start positions.
		if (r)
		{
			ConnectionType ct = GetConnectionType(); 
			Material usedMaterial = assets.cellEmptyNormal;
			if (ct == ConnectionType.ladder)
				usedMaterial = assets.cellLadderNormal;
			else if (ct == ConnectionType.chute)
				usedMaterial = assets.cellChuteNormal;
			r.sharedMaterial = usedMaterial;
		}
	}
	
	public void SetMoveMaterial()
	{
		BoardAssets assets = BoardAssets.Current;
		if (_renderer)
			_renderer.sharedMaterial = assets.GetGlowMaterial(_renderer.sharedMaterial);
	}
	
	public Cell FindNextIsometricCell()
	{
		Cell found = this.next;
		float maxDistanceX = SIZE * 0.5f;
		Vector3 position = _t.position;
		while (found)
		{
			Vector3 relPos = found._t.position - position;
			if (Mathf.Abs(relPos.x) < maxDistanceX && found.number > number + 2)
				return found;
			found = found.next;
		}
		return null;
	}
	
	public void HideMeshes()
	{
		StartCoroutine(HideMeshesForSomeTimeRoutine());
	}
	
	private IEnumerator HideMeshesForSomeTimeRoutine()
	{
		if (hideMeshesRoutineRunning)
			yield break;
		hideMeshesRoutineRunning = true;
		if (numberRoot)
			numberRoot.gameObject.SetActive(false);
		yield return new WaitForSeconds(2.5f);
		if (numberRoot)
			numberRoot.gameObject.SetActive(true);
		hideMeshesRoutineRunning = false;
	}
	
	public void StartSpringEffect(float delay)
	{
		StartCoroutine(SpringEffectRoutine(delay));
	}
	
	private IEnumerator SpringEffectRoutine(float delay)
	{
		AnimationClip clip = StaticAssets.instance.cellSpringClip;
		if (springEffectRoutineRunning || !clip)
			yield break;
		springEffectRoutineRunning = true;
		Animation anim = modelRoot.gameObject.AddComponent<Animation>();
		anim.AddClip(clip, "springJump");
		yield return new WaitForSeconds(delay);
		anim.Play("springJump");
		Sound.Play(Sound.instance.fxSpring);
		yield return new WaitForEndOfFrame();
		while (anim.isPlaying)
			yield return new WaitForEndOfFrame();
		Animation.Destroy(anim);
		if (modelRoot)
		{
			modelRoot.localPosition = Vector3.zero;
			modelRoot.localRotation = Quaternion.identity;
			modelRoot.localScale = Vector3.one;
		}
		springEffectRoutineRunning = false;
	}
}
