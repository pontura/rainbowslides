using UnityEngine;
using System.Collections;

// This class contains the mayority of the sounds used in the game.
// Some other can be triggered by animation events. Those are handled SoundTrigger.PlaySound()
public class Sound : MonoBehaviour
{
	public bool fxEnabled;
	public bool musicEnabled;
	public AudioSource fxSource;
	public AudioSource mainThemeMusicSource;
	public AudioSource ingameMusicSource;
	public AudioSource rainSource;
	public AudioSource dogSource;
	public AudioSource bubblesSource;
	public AudioSource skateboardSource;
	public float fxVolume = 1f;
	public float musicVolume = 1f;
	public float masterVolume = 1f;
	public AudioClip fxMenuIn;
	public AudioClip fxMenuOut;
	public AudioClip fxButton;
	public AudioClip fxButton2;
	public AudioClip fxMoveButton;
	public AudioClip fxArrowChangeSlot;
	public AudioClip fxBonusCycle;
	public AudioClip fxNumberResult;
	public AudioClip fxGoodResult;
	public AudioClip fxBadResult;
	public AudioClip fxCloudAppear;
	public AudioClip fxCloudThunder;
	public AudioClip fxBubbleAppear;
	public AudioClip fxChuteFall;
	public AudioClip fxLadderFirst;
	public AudioClip[] fxLadderNotes;
	public AudioClip fxSpring;
	public AudioClip fxGameStart;
	public AudioClip fxCharacterPass;
	public AudioClip fxCharacterPush;
	public AudioClip fxCharacterFalling;
	public AudioClip fxVictory;
	public AudioClip[] fxCharacterHello;
	public AudioClip fxTreeTouch;
	public static Sound instance;
	
	private int playRain;
	private int playDog;
	private int playBubbles;
	private int playSkateboard;
	
	void OnEnable()
	{
		if (instance)
			Debug.LogError("Sound must be a singletone.", this);
		instance = this;
	}
	
	void Update()
	{
		bool isIngame = GameGUI.AtIngame;
		ingameMusicSource.enabled = musicEnabled && isIngame && Ingame.instance.state != Ingame.State.endScene;
		mainThemeMusicSource.enabled = musicEnabled && !ingameMusicSource.enabled && !isIngame;
		
		// Start playing if its not already playing...
		if (ingameMusicSource.enabled && !ingameMusicSource.isPlaying)
			ingameMusicSource.Play();
		if (mainThemeMusicSource.enabled && !mainThemeMusicSource.isPlaying)
			mainThemeMusicSource.Play();
		
		if (fxSource && fxSource.enabled != fxEnabled)
			fxSource.enabled = fxEnabled;
		
		if (playRain > 0)
			playRain--;
		rainSource.enabled = playRain > 0 && fxSource.enabled && isIngame;

		if (playDog > 0)
			playDog--;
		dogSource.enabled = playDog > 0 && fxSource.enabled && isIngame;
		
		if (playBubbles > 0)
			playBubbles--;
		bubblesSource.enabled = playBubbles > 0 && fxSource.enabled && isIngame;

		if (playSkateboard > 0)
			playSkateboard--;
		skateboardSource.enabled = playSkateboard > 0 && fxSource.enabled && isIngame;
	}
	
	public static void Play(AudioClip clip)
	{
		if (!instance || !instance.fxEnabled || !clip)
			return;
		instance.fxSource.enabled = instance.fxEnabled; // make sure its enabled (required after turrning on from options)
		instance.fxSource.PlayOneShot(clip, instance.fxVolume * instance.masterVolume);
	}
		
	public static void Play(AudioClip[] clips)
	{
		if (clips == null || clips.Length == 0)
			return;
		Play(clips[Random.Range(0, clips.Length)]);
	}
	
	public static void PlayButton()
	{
		Play(instance.fxButton);
	}
	
	public static void PlayButton2()
	{
		Play(instance.fxButton2);
	}
	
	public static void PlayMenuIn()
	{
		Play(instance.fxMenuIn);
	}

	public static void PlayMenuOut()
	{
		Play(instance.fxMenuOut);
	}
	
	public static void RequestPlayRain()
	{
		instance.playRain = 10;
	}

	public static void RequestPlayDog()
	{
		instance.playDog = 10;
	}
	
	public static void RequestPlayBubbles()
	{
		instance.playBubbles = 10;
	}

	public static void RequestPlaySkateboard()
	{
		instance.playSkateboard = 10;
	}
	
	public static void PlayMenuInOuOnBoolChange(bool prevValue, bool newValue)
	{
		if (prevValue != newValue)
		{
			if (newValue)
				Sound.PlayMenuIn();
			else
				Sound.PlayMenuOut();
		}
	}
	
}
