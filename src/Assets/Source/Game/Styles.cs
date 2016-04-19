using UnityEngine;
using System.Collections;

// This class contains the styles used on the whole game.
public class Styles : AutoStyles
{
	public GUIStyle optionTitle;
	public GUIStyle optionContent;
	public GUIStyle optionLeft;
	public GUIStyle optionRight;
	public GUIStyle player;
	public GUIStyle playerTextField;
	public GUIStyle playerLabel;
	public GUIStyle setupButton;
	public GUIStyle ready;
	public GUIStyle moveButtonText;
	public GUIStyle pause;
	public GUIStyle unpause;
	public GUIStyle menuButton;
	
	public GUIStyle confirmationWindow;
	public GUIStyle confirmationOk;
	public GUIStyle confirmationCancel;
	public GUIStyle confirmationText;
	
	public GUIStyle airplayTitle;
	public GUIStyle airplayText;
	
	public GUIStyle pausedRestart;
	public GUIStyle pausedMusicOn;
	public GUIStyle pausedMusicOff;
	public GUIStyle pausedSoundOn;
	public GUIStyle pausedSoundOff;
	public GUIStyle pausedAirPlay;

	public GUIStyle skateTextBubble;
	
	public GUIStyle tapToExit;

	public GUIStyle exitButtonStyle;
	public RectEx.Position exitButtonPosition;
	public GUIStyle rematchButtonStyle;
	public RectEx.Position rematchButtonPosition;
	
	public GUIStyle devicePausedText;
	public GUIStyle devicePinchPanText;
	public GUIStyle deviceVictoryNameText;

	public static Styles instance;

	void OnEnable()
	{
		if (instance)
			Debug.LogError("Styles must be a singletone!", this);
		instance = this;
	}
	
}
