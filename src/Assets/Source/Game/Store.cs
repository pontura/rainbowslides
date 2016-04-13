using UnityEngine;
using System.Collections;

// This class handles the persistent data that can be stored on the device.
public class Store : MonoBehaviour
{
	public static void Load()
	{
		Sound sound = Sound.instance;
		sound.fxEnabled = PlayerPrefs.GetInt("sound", 1) != 0;
		sound.musicEnabled = PlayerPrefs.GetInt("music", 1) != 0;
		Ingame.instance.difficulty = (Ingame.Difficulty)PlayerPrefs.GetInt("difficulty", (int)Ingame.Difficulty.medium);
	}

	public static void Save()
	{
		Sound sound = Sound.instance;
		PlayerPrefs.SetInt("sound", sound.fxEnabled ? 1 : 0);
		PlayerPrefs.SetInt("music", sound.musicEnabled ? 1 : 0);
		PlayerPrefs.SetInt("difficulty", (int)Ingame.instance.difficulty);
	}
}
