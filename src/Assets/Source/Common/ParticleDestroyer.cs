using UnityEngine;
using System.Collections;

// This script destroys the gameobject when the contained particle system is longer more alive.
public class ParticleDestroyer : MonoBehaviour
{
	private ParticleSystem ps;
	
	void Start()
	{
		ps = GetComponent<ParticleSystem>();
	}
	
	void LateUpdate()
	{
		if (ps && !ps.IsAlive())
		{
			GameObject.Destroy(gameObject);
			ps = null;
		}
	}
}
