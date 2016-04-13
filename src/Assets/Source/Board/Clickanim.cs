using UnityEngine;
using System.Collections;

// A component that handles touches by playing an animation.
public class Clickanim : MonoBehaviour
{
	private Animation _anim;
	
	public string animName;
	
	public AudioClip clip;
	private static string CLICK_ANIM_DEFAULT_NAME = "boing";
	
	void Awake()
	{
		_anim = GetComponent<Animation>();
		if (animName == string.Empty)
			animName = CLICK_ANIM_DEFAULT_NAME;
	}
	
	public void OnClicked()
	{
		if (!_anim.IsPlaying(animName))
		{
			string oldAnim = _anim.clip.name;
			_anim.CrossFade(animName, 0.1f);
			if (oldAnim != animName)
				_anim.CrossFadeQueued(oldAnim, 0.1f);
			Sound.Play(clip);
		}
	}
	
}
