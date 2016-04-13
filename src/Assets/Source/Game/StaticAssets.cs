using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class contains art and logic assets references used along the whole game.
public class StaticAssets : MonoBehaviour
{
	// The prefab that contains the character logic.
	public GameObject characterPrefab;
	
	// The prefab that contains the character double logic (a reduced character that is used at player selection)
	public GameObject characterDoublePrefab;
	
	// The material applied to the character when its not playing.
	public Material emptyCharacter;
	
	// Special events prefabs.
	public GameObject bubblePrefab;
	public GameObject cloudRainPrefab;
	public GameObject skatePrefab;
	public GameObject dogPrefab;
	public GameObject trapDoorPrefab;
	
	// Background
	public Texture2D backgroundPattern;
	public Texture2D backgroundPattern2;
	public Color backgroundPatternColor = Color.white;
	public Color backgroundPatternColor2 = Color.white;
	public float backgroundPatternSize = 4;
	public float backgroundPatternSize2 = 4;
	
	// Dusts triggered by code
	public GameObject dustSkateAppearPrefab;
	public GameObject dustSkateDisappearPrefab;
	public GameObject dustSkateAppearSmallPrefab;
	public GameObject dustDogAppearPrefab;
	public GameObject dustDogDisappearPrefab;
	
	// Game start
	// GUI stuff
	public Texture2D iconAI;
	public Texture2D iconAdd;
	public Texture2D textFieldBackground;
	public Texture2D pauseButtonBack;
	public float pauseButtonBackSize = 0.25f;
	public Vector2 pauseButtonPosition;
	public float pauseButtonSize = 0.1f;
	public Texture2D pausedBackground;
	public float pausedBackgroundSize = 0.25f;
	public float pausedItemSize = 0.1f;
	public Vector2 pausedRestartPosition;
	public Vector2 pausedMusicPosition;
	public Vector2 pausedSoundPosition;
	public float pausedItemStartAngle = 10;
	public float pausedItemEndAngle = 80;
	public float pausedItemRadius = 1;
	public Vector2 pausedItemOffset = Vector2.zero;
	public bool pausedBlack = false;
	public float confirmationWindowSize = 0.4f;
	public Vector2 difficultySize = new Vector2(0.3f, 0.1f);
	public Vector2 difficultyPosition = new Vector2(0.5f, 0.98f);
	public Vector2 skateTextBubblePosition;
	public Vector2 skateTextBubbleSize;
	public TextAnchor skateTextAnchor = TextAnchor.UpperLeft;

	// Move button
	public Texture2D moveButtonBack;
	public Vector2 moveButtonPosition = new Vector2(0.8f, 0.8f);
	public float moveButtonBackSize = 0.28f;
	public AnimationCurve moveButtonScaling = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public Texture2D moveButtonPlay;
	public float moveButtonPlayFrequency = 0.8f;
	public Rect moveButtonPlayPosition;
	public Texture2D[] moveButtonNumbers;
	public Rect moveButtonNumberPosition;
	
	// Ingame animation parameters
	public AnimationClip cellSpringClip;
	public float chuteCharacterMoveTimeK = 1f;
	public AnimationCurve chuteCharacterTimeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public AnimationCurve chuteCharacterMoveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public AnimationCurve horizontalTransition = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	
	// Game Start transition: "Ready, set, go!"
	public AnimationCurve gameStartTransition0 = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public AnimationCurve gameStartTransition1 = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public AnimationCurve gameStartTransition2 = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public AnimationCurve gameStartTransition3 = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public Texture2D gameStart0;
	public Texture2D gameStart1;
	public Texture2D gameStart2;
	public Texture2D gameStart3;
	public Vector2 gameStartPosition = new Vector2(0.5f, 0.5f);
	public Vector2 gameStartSize = new Vector2(1.0f, 0.3f);
	public float gameStartMenuInTime = 0.1f;
	public float gameStartMelodyTime = 0.25f;
	public float gameStartMenuOutTime = 0.8f;
	
	// The text used to display the player's name on the avatar base.
	public TextMesh avatarText;
	
	// The congratulations billboard used when a player wins.
	public GameObject congratulations;
	public TextMesh congratulationsText;
	public Vector2 tapToExitPosition;
	public Vector2 tapToExitSize;
	
	// The skate used with the double character.
	public Renderer skateBase;
	public float skateHeight = 0.1f;
	public static StaticAssets instance;

	// TV related assets
	public Transform gameSetupBackOnTV;
	public Transform gameSetupLogoOnTV;
	public float gameSetupLogoSize = 1.5f;
	public Transform readySetGoBackOnTV;
	public Transform readyOnTV;
	public Transform setOnTV;
	public Transform goOnTV;
	public Renderer grayedTV;
	public Renderer pausedTV;
	public Renderer tapToExitTV;
	public RectEx.Position ingameHintPosition = new RectEx.Position();
	public float ingameHintFrequency = 6f;
	public float ingameHint1T = 0.3f;
	public float ingameHint2T = 0.6f;
	public RectEx.Position victoryNamePosition = new RectEx.Position();
	public Color disabledColor = Color.gray;
	public GameObject spinAgainOnTV;
	public Vector3 spinAgainOnTVOffset = Vector3.up;
	
	public float superDPIscale = 1.5f;
	
	void OnEnable()
	{
		if (instance)
			Debug.LogError("LogicAssets already exists. It must be a singletone!", this);
		instance = this;
	}
		
	public static GameObject SpawnCharacter()
	{
		if (!instance.characterPrefab)
		{
			Debug.LogError("Cannot spawn a new character. The characterPrefab is not set.");
			return null;
		}
		return (GameObject)GameObject.Instantiate(instance.characterPrefab);
	}
	
	public static GameObject SpawnDouble()
	{
		if (!instance.characterDoublePrefab)
		{
			Debug.LogError("Cannot spawn a double character. The characterDoublePrefab is not set.");
			return null;
		}
		return (GameObject)GameObject.Instantiate(instance.characterDoublePrefab);
	}

	public static GameObject Spawn(GameObject prefab, Vector3 position, float distanceToCamera)
	{
		if (!prefab)
			return null;
		Camera cam = BoardCamera.instance.Camera;
		if (!cam)
			return null;
		if (distanceToCamera != 0f)
		{
			Vector3 screenPoint = cam.WorldToScreenPoint(position);
			Ray ray = cam.ScreenPointToRay(screenPoint);
			position += ray.direction * (-distanceToCamera);
		}
		return (GameObject)GameObject.Instantiate(prefab, position, prefab.transform.rotation);
	}
	
	void Update()
	{
		// show or hide the skateboard, spawning particles when it appears
		if (skateBase)
		{
			bool showSkate = false;
			if (GameGUI.AtIngame)
			{
				Player pt = Ingame.instance.PlayerTurn;
				if (pt && pt.isPlaying && pt.character && pt.character.skate)
				{
					showSkate = true;
				}
			}
			if (skateBase.enabled != showSkate)
			{
				if (showSkate)
				{
					Vector3 pos = skateBase.transform.position;
					pos -= Vector3.forward * 0.2f;
					GameObject go = (GameObject)GameObject.Instantiate(StaticAssets.instance.dustSkateAppearSmallPrefab, pos, Quaternion.identity);
					foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
						r.gameObject.layer = skateBase.gameObject.layer;
				}
			}
			skateBase.enabled = showSkate;
		}
		
		// show / hide the congratulations object
		if (congratulations)
		{
			bool show = false;
			if (GameGUI.AtIngame && Ingame.instance.state == Ingame.State.endScene)
			{
				Player theWinner = Ingame.instance.TheWinner;
				if (theWinner && theWinner.character)
				{
					congratulations.transform.position = theWinner.character.transform.position;
					show = true;
					if (congratulationsText)
						congratulationsText.text = theWinner.usedName;
				}
			}
			else
			{
				congratulations.SetActive(false);
			}
			congratulations.SetActive(show);
		}
	}
	

}
