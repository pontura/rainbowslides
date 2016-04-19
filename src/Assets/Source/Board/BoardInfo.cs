using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class handles data and logic related to general board variables.
public class BoardInfo : MonoBehaviour
{
	// The min and max visible positions that can be reached by the camera.
	public Vector3 minCameraPosition;
	public Vector3 maxCameraPosition;
	
	// The cells currently involved in the update process.
	private List<Cell> involvedCells = new List<Cell>();
	
	// A temporary list of cells that needs to glow.
	private List<Cell> moveCells = new List<Cell>();
	
	// The timer used to animate the glowing material.
	private float timeMove;

	// The last loaded level. This is used for debug.
	public LevelInfo lastLoadedLevel;
	
	// The difficulty levels references. Set these on the inspector.
	public List<LevelInfo> easyLevels = new List<LevelInfo>();
	public List<LevelInfo> normalLevels = new List<LevelInfo>();
	public List<LevelInfo> hardLevels = new List<LevelInfo>();
	
	// The singletone instance.
	public static BoardInfo instance;
	
	void OnEnable()
	{
		if (instance)
			Debug.LogError("There should be only one Board active!");
		instance = this;
	}

	void OnDisable()
	{
		if (instance == this)
			instance = null;
	}
	
	public void LoadRandomLevel()
	{
		if (Dev.UseHardcodedLevel && Dev.instance.hardcodedStart)
		{
			LoadLevel(Dev.instance.hardcodedStart);
		}
		else
		{
			LoadRandomLevel(GetUsedLevels());
		}
	}
	
	public List<LevelInfo> GetUsedLevels()
	{
		if (Ingame.instance.difficulty == Ingame.Difficulty.hard)
			return hardLevels;
		else if (Ingame.instance.difficulty == Ingame.Difficulty.easy)
			return easyLevels;
		return normalLevels;
	}
	
	public void LoadNextLevel(int increment)
	{
		List<LevelInfo> usedLevels = GetUsedLevels();
		int index = usedLevels.IndexOf(lastLoadedLevel);
		if (index == -1 || usedLevels.Count == 0)
		{
			LoadRandomLevel(usedLevels);
		}
		else
		{
			LoadLevel(usedLevels[(index + usedLevels.Count + increment) % usedLevels.Count]);
		}
	}
	
	public void LoadRandomLevel(List<LevelInfo> levels)
	{
		LoadLevel(levels[Random.Range(0, levels.Count)]);
	}
	
	public void LoadLevel(LevelInfo level)
	{
		if (!LoadLevelInternal(level))
			Debug.LogError("Level could not be loaded: " + level.name);
	}
	
	private bool LoadLevelInternal(LevelInfo level)
	{
		lastLoadedLevel = level;
		
		// First, destroy previous connections, and cache the cells by numbers to speed up next searches.
		Dictionary<int, Cell> numberToCell = new Dictionary<int, Cell>();
		foreach (Cell c in Cell.all)
		{
			c.DestroyConnection();
			if (!numberToCell.ContainsKey(c.number))
				numberToCell.Add(c.number, c);
		}
		
		if (!level)
		{
			Debug.LogError("Null level");
			return false;
		}
		
		foreach (ConnectionInfo cInfo in level.connections)
		{
			Cell cellStart = numberToCell.ContainsKey(cInfo.start) ? numberToCell[cInfo.start] : null;
			if (cellStart == null)
			{
				Debug.LogError("start cell not found: " + cInfo.start);
				return false;
			}
			Cell cellEnd = numberToCell.ContainsKey(cInfo.end) ? numberToCell[cInfo.end] : null;
			if (cellEnd == null)
			{
				Debug.LogError("end cell not found: " + cInfo.end);
				return false;
			}
			if (!cInfo.model)
			{
				Debug.LogError("model not set");
				return false;
			}
			cellStart.LoadConnectionModel(cellEnd, cInfo.model);
		}
		
		// Now, refresh the positions and materials
		foreach (Cell cell in Cell.all)
		{
			cell.RefreshPosition(0f);
			cell.RefreshMaterial(false);
		}
		
		return true;
	}
	
	void Update()
	{
		// Gather cells that needs update.
		// Those where the characters are standing are lowered, so they need to be updated when the character leaves.
		foreach (Player p in Player.all)
		{
			if (p.isPlaying && p.character)
			{
				Cell c1 = p.character.CurrentCell;
				if (c1 && !involvedCells.Contains(c1))
					involvedCells.Add(c1);
				Cell c2 = p.character.TargetCell;
				if (c2 && !involvedCells.Contains(c2))
					involvedCells.Add(c2);
			}
		}
		
		// Refresh the position of the involved cells.
		BoardAssets assets = BoardAssets.Current;
		if (assets)
		{
			foreach (Cell c in involvedCells)
			{
				if (c != null)
				{
					float minDistance = Mathf.Infinity;
					foreach (BoardEntity e in c.entities)
						minDistance = Mathf.Min(minDistance, Vector3.Distance(c.transform.position, e.transform.position));
					float down = minDistance <= Cell.SIZE * assets.distanceToMoveDown ? assets.cellMoveDownDistance : 0f;
					c.RefreshPosition(down);
					c.RefreshMaterial(down != 0f);
				}
			}
		}
		
		// Stop updating the involved cells when no entity is standing on them.
		for (int i = 0; i < involvedCells.Count; i++)
		{
			Cell c = involvedCells[i];
			if (c == null || c.entities.Count == 0)
				involvedCells.RemoveAt(i--);
		}
		
		// Gather the move cells: those that needs the glowing effect.
		moveCells.Clear();
		Ingame ingame = Ingame.instance;
		if (ingame && ingame.state == Ingame.State.waitingPlayerMoveInput)
		{
			Player player = ingame.PlayerTurn;
			if (player && player.isPlaying)
			{
				Character character = player.character;
				if (character)
				{
					Cell cell = character.TargetCell;
					if (cell)
						moveCells.Add(cell);
					else
						cell = character.CurrentCell;
					int count = character.QueuedMoves + ingame.QueuedMoves;
					for (int i = 0; i < count; i++)
					{
						if (cell)
							cell = cell.next;
						else
							cell = Cell.firstCell;
						moveCells.Add(cell);
						if (cell)
							moveCells.Add(cell);
						else
							break;
					}
				}
			}
			timeMove += Time.deltaTime * 20;
		}
		else
		{
			timeMove = 0f;
		}
		
		// Update the material of the moving cells.
		for (int i = 0; i < moveCells.Count; i++)
		{
			Cell c = moveCells[i];
			if (Dev.HighlightMoveMode)
			{
				if (i != moveCells.Count - 1)
					continue;
			}
			if (c)
			{
				c.SetMoveMaterial();
				if (!involvedCells.Contains(c))
					involvedCells.Add(c);
			}
		}
	}
	
	public void RequestCellUpdate(Cell cell)
	{
		if (!involvedCells.Contains(cell))
		{
			involvedCells.Add(cell);
		}
	}

}
