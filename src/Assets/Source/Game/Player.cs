using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class represents a player.
public class Player : MonoBehaviour
{
	// A number to define the start order.
	public int startOrder;
	
	// The name of the player (can't use the gameobject name, because it cannot be set empty while editting on a text field)
	public string usedName = "Player";
	
	// The name that will have by default. This name is also used when the AI is playing.
	public string defaultName = "Player";
	
	// Its a human controlled player?
	public bool isHuman = false;
	
	// Its playing the game?
	public bool isPlaying = false;
	
	// The current character under control. If null, it must be spawned.
	public Character character;
	
	// The 3D model used to represent the character.
	public GameObject model;
	
	// The material used to replace the model default material;
	public Material replaceMaterial;
    // The material used to replace the model default material;
    public Material eyesMaterial;

	// The texture used on the move button. It should have the player color.
	public Texture2D coloredButton;
	
	// The background used at game setup. This is disabled at ingame.
	public Renderer background;
	
	// The bar used on top of the background at game setup. This is disabled at ingame.
	public Renderer bar;
	
	// The GUI color that represents this player.
	public Color color = Color.white;
	public StartPlace startPlace;
	public static List<Player> all = new List<Player>();
	
	// Used to debug moving cases;
	public int debugStartCellNumber;
	
	// A double of the character, used to represent it on the player selection screen.
	private CharacterDouble theDouble;
	
	// The camera used to render the double at the character selection screen.
	public Camera doubleCamera;
	
	[ContextMenu("CopyColorToCamera")]
	public void CopyColorToCamera()
	{
		doubleCamera.backgroundColor = color;
	}
	
	void OnEnable()
	{
		all.Add(this);
		all.Sort(PlayerComparison);
	}

	void OnDisable()
	{
		all.Remove(this);
	}
	
	
	void Awake()
	{
		this.usedName = defaultName;
	}
	
	void Start()
	{
		// instantiate the double
		GetDouble();
		
		// make sure the background and bar are properly enabled
		if (background)
			background.enabled = true;
		if (bar)
			bar.enabled = false;
	}
	
	private static int PlayerComparison(Player a, Player b)
	{
		return a.startOrder.CompareTo(b.startOrder);
	}
	
	public void Restore()
	{
		if (character)
		{
			character.Restore();
			character.gameObject.SetActive(false);
		}
		startPlace = null;
	}
		
	public void Switch()
	{
		if (!isPlaying)
		{
			isPlaying = true;
			isHuman = true;
		}
		else
		{
			if (isHuman)
				isHuman = false;
			else
				isPlaying = false;
		}
		if (theDouble)
			theDouble.OnPlayerSwitch();
		if (bar)
			bar.enabled = isPlaying;
	}
	
	private GameObject InstantiateModel(GameObject modelPrefab, Material replaceMaterial)
	{
		GameObject go = (GameObject)GameObject.Instantiate(modelPrefab, Vector3.zero, Quaternion.identity);
		if (replaceMaterial)
		{
            foreach (SkinnedMeshRenderer r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.material.name != "EYES Cheer"  && r.material.name != "EYES Cheer (Instance)")
                    r.sharedMaterial = replaceMaterial;
            }
		}
		return go;
	}
	
	public Character GetCharacter()
	{
		if (character == null)
		{
			GameObject goCharacter = (GameObject)StaticAssets.SpawnCharacter();
			goCharacter.name = "characterLogic " + name;
			character = goCharacter.GetComponent<Character>();
			GameObject modelGO = InstantiateModel(model, replaceMaterial);
			modelGO.name = "characterModel " + name;
			character.SetModel(modelGO);
			character.player = this;
			GameObject.DontDestroyOnLoad(character.gameObject);
		}
		character.gameObject.SetActive(true);
		return character;
	}
	
	public CharacterDouble GetDouble()
	{
		if (theDouble == null)
		{
			GameObject goCharacter = (GameObject)StaticAssets.SpawnDouble();
			goCharacter.name = "characterDoubleLogic " + name;
			theDouble = goCharacter.GetComponent<CharacterDouble>();
			GameObject modelGO = InstantiateModel(model, replaceMaterial);
			modelGO.name = "characterDoubleModel " + name;
			theDouble.SetModel(modelGO);
			theDouble.player = this;
			SetLayerRecursive(goCharacter.transform, gameObject.layer);
			GameObject.DontDestroyOnLoad(theDouble.gameObject);
		}
		return theDouble;
	}
	
	private static void SetLayerRecursive(Transform t, int layer)
	{
		t.gameObject.layer = layer;
		foreach (Transform t2 in t)
			SetLayerRecursive(t2, layer);
	}
	
	public void Spawn()
	{
		Kill();
		if (startPlace == null)
			startPlace = FindStartPlace();
		if (!startPlace)
		{
			Debug.LogError("Cant spawn a new character. Start place not found");
			return;
		}
		character = GetCharacter(); // make sure the character is instantiated and active
		character.TargetPlace = startPlace.transform;
		character.InstantMove();
	}
	
	public static StartPlace FindStartPlace()
	{
		foreach (StartPlace a in StartPlace.instances)
		{
			if (!HasOwner(a))
				return a;
		}
		return null;
	}
	
	private static bool HasOwner(StartPlace sp)
	{
		foreach (Player p in all)
			if (p.startPlace == sp)
				return true;
		return false;
	}
	
	public void Kill()
	{
		if (character && character.gameObject.activeSelf)
			character.gameObject.SetActive(false);
	}
	
	public void OnGameStart()
	{
		if (Dev.CheatedStart && debugStartCellNumber != 0 && character)
		{
			Cell cell = Cell.firstCell;
			while (cell && cell.number != debugStartCellNumber)
				cell = cell.next;
			if (cell && cell.number == debugStartCellNumber)
				character.SetMoveTarget(cell.transform, Character.MoveMode.jump);
		}
	}
	
	public void OnGameSetup(bool active)
	{
		SetGameSetupObjects(active);
		theDouble.OnGameSetup(active);
		//if (background)
		//	background.enabled = active;
		if (bar)
			bar.enabled = active && isPlaying;
		doubleCamera.farClipPlane = active ? 5 : 2.39f;
	}
		
	private void SetGameSetupObjects(bool active)
	{
		doubleCamera.enabled = active;
		theDouble.gameObject.SetActive(active);
	}
	
	public void SetDoubleActive(bool active)
	{
		if (theDouble)
			theDouble.gameObject.SetActive(active);
	}

	public static void ResetPlayerNames()
	{
		foreach (Player p in all)
			p.usedName = p.defaultName;
	}
	
}
