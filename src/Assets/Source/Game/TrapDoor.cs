using UnityEngine;
using System.Collections;

// This class handles the trap door object effect.
public class TrapDoor : MonoBehaviour
{
	public float destroyTime = 5f;
	private float time;
	
	void Update()
	{
		time += Time.deltaTime;
		Ingame.instance.RequestNotReady();
		if (time > destroyTime)
		{
			GameObject.Destroy(gameObject);
		}
	}

}
