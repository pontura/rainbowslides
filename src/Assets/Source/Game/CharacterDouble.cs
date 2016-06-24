using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The character double is the secondary instantiated character that is used as avatar on the corner of the screen.
// This class handles animations that can be triggered on this special duplicated character.
// It contains the logic to respond to ingame evets. For example, if the character gets the skate, it will play the skating anim.
public class CharacterDouble : MonoBehaviour
{
	public Transform modelRoot;
	[HideInInspector]
	public Player player;
	private CharacterEvents charEvents;
	private CharacterAnimNames animNames;
	private Animation anim;
	public  List<Renderer> renderers;
	private Transform _t;
    public TextMesh textMesh;
    public GameObject skate;
	
	void Awake()
	{
		_t = transform;
	}
	
	public void SetModel(GameObject model)
	{
		Transform modelT = model.transform;
		modelT.parent = modelRoot;
		modelT.localPosition = Vector3.zero;
		modelT.localRotation = Quaternion.Euler(0, 180, 0);
		anim = model.GetComponentInChildren<Animation>();
		charEvents = model.GetComponentInChildren<CharacterEvents>();
		animNames = model.GetComponentInChildren<CharacterAnimNames>();
        //renderers = model.GetComponentInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer rend in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderers.Add(rend);
        }
		FloorShadow fs = model.GetComponentInChildren<FloorShadow>();
		if (fs)
		{
			fs._renderer.enabled = false;
			fs.enabled = false;
		}
	}
	
	[ContextMenu("Play Hello")]
	public void PlayHello()
	{
		if (!UsingSkate && !anim.IsPlaying(animNames.HELLO_NAME))
		{
			Play(animNames.HELLO_NAME);
			Sound.Play(Sound.instance.fxCharacterHello);
		}
	}

	[ContextMenu("Play Idle")]
	public void PlayIdle()
	{
		if (animNames)
			CrossFade(animNames.IDLE_NAME);

        print("animNames " + animNames);

	}
	
	[ContextMenu("Play Victory")]
	public void PlayVictory()
	{
		if (animNames)
			CrossFade(animNames.VICTORY_NAME);
	}
	
	public void OnGameSetup(bool active)
	{
		if (active)
		{
			Play(animNames.IDLE_NAME);
			// instant move
            modelRoot.position = Vector3.zero;
            modelRoot.rotation = Quaternion.identity;
		}
		UpdateModelMaterial();
	}

	public void OnEnable()
	{
		StopAllCoroutines();
		if (animNames)
			Play(animNames.IDLE_NAME);
	}
	
	public void OnDisable()
	{
		StopAllCoroutines();
	}
	
	void Start()
	{
		Play(animNames.IDLE_NAME);
		    UpdateModelMaterial();

            if (textMesh != null && player != null)
                textMesh.text = player.defaultName;
	}
	
	private bool UsingSkate
	{
		get
		{
			return GameGUI.AtIngame && player.isPlaying && player.character && player.character.skate;
		}
	}
	
	void Update()
	{
		if (!anim.isPlaying)
			CrossFade(animNames.IDLE_NAME);
		Vector3 targetPos = Vector3.zero;
        Vector3 ModeltargetPos = Vector3.zero;
		Quaternion targetRotation = Quaternion.identity;
		if (GameGUI.AtIngame)
		{
			if (IsPlaying(animNames.HELLO_NAME))
			{
				// wait until the animation finishes
				targetRotation = Quaternion.Euler(0, charEvents.angleLookHelloAvatar, 0);
                skate.SetActive(false);
			}
			else if (UsingSkate)
			{
                ModeltargetPos = new Vector3(0, StaticAssets.instance.skateHeight, 0);
				targetRotation = Quaternion.Euler(0, 270, 0);
				CrossFade(animNames.SKATEBOARD_NAME);
                skate.SetActive(true);
			}
			else
			{
                skate.SetActive(false);
				CrossFade(animNames.IDLE_NAME);
			}
		}
		float dt = Time.deltaTime;
        _t.position = Vector3.Lerp(_t.position, targetPos, dt * 10f);
        modelRoot.position = Vector3.Lerp(modelRoot.position, ModeltargetPos, dt * 10f);
        modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, dt * 10f);
	}
	
	public void OnPlayerSwitch()
	{
		if (player.isPlaying && player.isHuman)
		{
			StopAllCoroutines();
			StartCoroutine(SelectedHumanRoutine());
		}
		else if (player.isPlaying && !player.isHuman)
		{
			//StopAllCoroutines();
			//StartCoroutine(SelectedRobotRoutine());
		}
		else if (!player.isPlaying)
		{
			//StopAllCoroutines();
			//StartCoroutine(SelectedEmptyRoutine());
		}
		UpdateModelMaterial();
	}
	
	public void UpdateModelMaterial()
	{
		Material usedMaterial = StaticAssets.instance.emptyCharacter;
        Material eyesMaterial = StaticAssets.instance.emptyCharacter;

        if (player.isPlaying)
        {
            usedMaterial = player.replaceMaterial;
            eyesMaterial = player.eyesMaterial;
        }
        int id = 0;
        foreach (Renderer r in renderers)
        {
            if (id == 0)
            {
                r.sharedMaterial = usedMaterial;
            }
            else if (id == 1)
            {   
                r.sharedMaterial = eyesMaterial;
            }
            id++;
        }
	}
	
	IEnumerator SelectedHumanRoutine()
	{
		CrossFade(animNames.VICTORY_NAME);
		CrossFadeQueued(animNames.IDLE_NAME);
		while (IsPlayingLastAnim())
			yield return new WaitForEndOfFrame();
		CrossFade(animNames.IDLE_NAME);
	}
	
	IEnumerator SelectedRobotRoutine()
	{
		CrossFade(animNames.GASP_NAME);
		while (IsPlayingLastAnim())
			yield return new WaitForEndOfFrame();
		CrossFade(animNames.IDLE_NAME);
	}
	
	IEnumerator SelectedEmptyRoutine()
	{
		CrossFade(animNames.ON_RAIN_START_NAME);
		while (IsPlayingLastAnim())
			yield return new WaitForEndOfFrame();
		CrossFade(animNames.ON_RAIN_LOOP_NAME);
		yield return new WaitForSeconds(0.5f);
		CrossFade(animNames.ON_RAIN_END_NAME);
	}
	
	string lastPlayedAnim = "";
	
	bool IsPlayingLastAnim()
	{
		return IsPlaying(lastPlayedAnim);
	}
	
	bool IsPlaying(string animName)
	{
		if (lastPlayedAnim != animName)
			return false;
		if (!anim.IsPlaying(animName))
			return false;
		AnimationState animState = anim[animName];
		if (!animState)
			return false;
		if (animState.wrapMode == WrapMode.ClampForever && animState.time > animState.clip.length)
			return false;
		return true;
	}
	
	void Play(string animName)
	{
		anim.Play(animName, PlayMode.StopAll);
		anim.Sample();
		lastPlayedAnim = animName;
	}

	void CrossFade(string animName)
	{
		anim.CrossFade(animName, 0.2f);
		lastPlayedAnim = animName;
	}
	
	void CrossFadeQueued(string animName)
	{
		anim.CrossFadeQueued(animName, 0.2f);
	}

}
