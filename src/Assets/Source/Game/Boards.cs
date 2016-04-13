using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class contains the list of supported boards.
// You should use this if you plan to add more boards to the game.
public class Boards : MonoBehaviour
{
	
	public List<string> sceneNames = new List<string>();
	
	void Start()
	{
		Object[] objects = Object.FindObjectsOfType(typeof(Transform));
		foreach (Transform t in objects)
			GameObject.DontDestroyOnLoad(t.gameObject);
		Load(0);
	}
	
	public void Load(int id)
	{
		if (sceneNames.Count == 0)
			return;
		Application.LoadLevel(sceneNames[id % sceneNames.Count]);
	}

}
