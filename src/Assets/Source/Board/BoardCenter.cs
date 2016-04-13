using UnityEngine;
using System.Collections;

// This class is used to identify the center of the board. The camera will target it when the game starts.
public class BoardCenter : MonoBehaviour
{
	public static BoardCenter instance;
	
	void OnEnable()
	{
		instance = this;
	}
	
	void OnDisable()
	{
		if (instance == this)
			instance = null;
	}
}
