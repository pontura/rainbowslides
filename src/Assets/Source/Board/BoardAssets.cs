using UnityEngine;
using System.Collections;

// This class contains art assets references used on the current scene board.
// These may change on each scene, depending on the specific aesthetic of each level.
[ExecuteInEditMode]
public class BoardAssets : MonoBehaviour
{
	// The cell world size.
	public float cellSize = 2f;
	public float cellMoveDownDistance = 0.1f;
	public float distanceToMoveDown = 0.5f;
	
	// Simplified color mode: a default normal color and a color for chutes and another for ladders
	public Material cellEmptyNormal;
	public Material cellChuteNormal;
	public Material cellLadderNormal;
	private Material cellEmptyGlow;
	private Material cellChuteGlow;
	private Material cellLadderGlow;

	// The game object with a TextMesh used to create the number.
	public GameObject cellTextPrefab;
	
	// The mesh of a default cell.
	public Mesh cellFull;
	
	// The mesh of a cell with a hole in it (for a chute start)
	public Mesh cellHole;

	// The color intensity over time used to animate the move material.
	public AnimationCurve glowCurve = AnimationCurve.EaseInOut(0f, 0.7f, 0.1f, 0.9f);

	// The overlays.
	public Transform overlayLeft;
	public Transform overlayRight;
	public float screenZ;
	public float startMove = 0f;
	public float zoomMoveK = 2f;
	private float overlayLeftAspect;
	private float overlayRightAspect;

	// The singletone reference.
	private static BoardAssets current;
	
	public static BoardAssets Current
	{
		get
		{
			return current;
		}
	}

	void OnEnable()
	{
		if (current)
			Debug.LogError("There is already a BoardAsset instance. It should be only one active.", this);
		current = this;
		
		// glowing materials
		if (cellEmptyNormal)
			cellEmptyGlow = new Material(cellEmptyNormal);
		if (cellChuteNormal)
			cellChuteGlow = new Material(cellChuteNormal);
		if (cellLadderNormal)
			cellLadderGlow = new Material(cellLadderNormal);
	}

	void OnDisable()
	{
		if (current == this)
			current = null;
		if (cellEmptyGlow)
		{
			Material.DestroyImmediate(cellEmptyGlow);
			cellEmptyGlow = null;
		}
		if (cellChuteGlow)
		{
			Material.DestroyImmediate(cellChuteGlow);
			cellChuteGlow = null;
		}
		if (cellLadderGlow)
		{
			Material.DestroyImmediate(cellLadderGlow);
			cellLadderGlow = null;
		}
	}

	void Start()
	{
		overlayLeftAspect = GetTextureAspect(overlayLeft);
		overlayRightAspect = GetTextureAspect(overlayRight);
	}
		
	public Material GetGlowMaterial(Material original)
	{
		if (original == cellEmptyNormal)
			return cellEmptyGlow;
		if (original == cellChuteNormal)
			return cellChuteGlow;
		if (original == cellLadderNormal)
			return cellLadderGlow;
		return original;
	}

	float GetTextureAspect(Transform t)
	{
		if (!t)
			return 1f;
		MeshRenderer mr = t.GetComponent<MeshRenderer>();
		if (!mr)
			return 1f;
		Material material = mr.sharedMaterial;
		if (!material)
			return 1f;
		Texture texture = material.mainTexture;
		if (!texture)
			return 1f;
		return (float)texture.width / texture.height;
	}
	
	public void Update()
	{
		UpdateGlowColor(cellEmptyGlow, cellEmptyNormal);
		UpdateGlowColor(cellChuteGlow, cellChuteNormal);
		UpdateGlowColor(cellLadderGlow, cellLadderNormal);
		UpdateOverlay();
	}
	
	private void UpdateGlowColor(Material glow, Material normal)
	{
		if (glow)
			glow.color = CalculateGlowColor(normal);
	}
	
	private Color CalculateGlowColor(Material material)
	{
		if (!material)
			return Color.white;
		Color c = material.color;
		float k = glowCurve.Evaluate(Time.time);
		c.r *= k;
		c.g *= k;
		c.b *= k;
		return c;
	}

	void UpdateOverlay()
	{
		BoardCamera bc = BoardCamera.instance;
		if (bc)
		{
			Camera cam = bc.Camera;
			if (cam)
			{
				float moveK = (Mathf.Max(BoardCamera.instance.GetZoom(), 1f) - 1f) * zoomMoveK;

				if (overlayLeft)
				{
					Vector3 scale = Vector3.one;
					scale.y = cam.orthographicSize * 2;
					scale.x = scale.y * overlayLeftAspect;
					overlayLeft.localScale = scale;
					
					Vector3 position = bc.ScreenToCameraPosition(new Vector3(0, 0, 0), screenZ);;
					position.x -= (startMove + moveK) * scale.x;
					overlayLeft.position = position;
				}
				if (overlayRight)
				{
					Vector3 scale = Vector3.one;
					scale.y = cam.orthographicSize * 2;
					scale.x = scale.y * overlayRightAspect;
					overlayRight.localScale = scale;
					
					// a little gap to avoid seeing thru the border
					Vector3 position = bc.ScreenToCameraPosition(new Vector3(DualCamera.GetWidth(1) * 1.001f, 0, 0), screenZ);;
					position.x += (startMove + moveK) * scale.x;
					overlayRight.position = position;
				}
			}
		}
	}
	
}
