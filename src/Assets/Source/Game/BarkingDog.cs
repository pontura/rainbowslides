using UnityEngine;
using System.Collections;

// This class handles the barking dog effect.
public class BarkingDog : BoardEntity
{
	public int movesLeft;
	public float fadeOutTime = 1f;
	public float tauntSpeed = 1f;
	public float runSpeed = 1f;
	public bool look = true;
	public bool flip = false;
	
	private Animation anim;
	private Transform t;
	private Vector3 originalScale;
	private float scaleK;
	private bool dustSpawned;
	
	void Start()
	{
		t = transform;
		originalScale = t.localScale;
		anim = GetComponentInChildren<Animation>();
		if (anim["taunt"])
		{
			anim["taunt"].speed = tauntSpeed;
			anim.Play("taunt");
		}
		if (anim["run"])
			anim["run"].speed = runSpeed;
	}
	
	void Update()
	{
		// ingame should not change state until the barking dog dissapears
		Ingame.instance.RequestNotReady();
		
		float targetScale = 1f;
		if (anim.IsPlaying("taunt"))
		{
			// wait until the animation finish
			if (look)
				LookToPrevious(false);
		}
		else if (TargetPlace != null)
		{
			t.localScale = originalScale;
			if (anim["run"])
				anim.CrossFade("run");
			Vector3 targetPosition = TargetPlace.position;
			if (MoveTo(targetPosition, look))
			{
				CurrentPlace = TargetPlace;
				TargetPlace = null;
				Cell cc = CurrentCell;
				if (cc && cc.previous && movesLeft > 0)
				{
					movesLeft--;
					TargetPlace = cc.previous.transform;
				}
			}
			Sound.RequestPlayDog();
		}
		else
		{
			if (!dustSpawned)
			{
				dustSpawned = true;
				StaticAssets.Spawn(StaticAssets.instance.dustDogDisappearPrefab, transform.position, 1f);
			}
			targetScale = 0f;
			if (anim["idle"])
				anim.CrossFade("idle");
			if (t.localScale.sqrMagnitude < 0.01f)
				GameObject.Destroy(gameObject);
		}
		
		Vector3 newScale = originalScale * scaleK;
		if (flip)
		{
			Cell cell = CurrentCell;
			if (cell)
			{
				Vector3 dir = cell.PreviousDirection;
				if (dir.x < 0 || dir.z < -0.1f)
					newScale.x *= -1;
			}
		}
		t.localScale = newScale;
		scaleK = Mathf.Lerp(scaleK, targetScale, 10f * Time.deltaTime);
	}
}
