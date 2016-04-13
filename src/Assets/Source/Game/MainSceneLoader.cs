using UnityEngine;
using System.Collections;

// A helper script to load the main scene. This is useful when you are editing the board and you start the simulation.
public class MainSceneLoader : MonoBehaviour
{
	public string mainSceneName = "";
	
	void Start()
	{
		if (Ingame.instance == null)
		{
			Debug.LogWarning("Ingame instance not found. This may happen if you are not running the main scene. Loading it now.");
			Application.LoadLevel(mainSceneName);
		}
	}
}
