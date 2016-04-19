using UnityEngine;
using System.Collections;

// This script is used to trigger sounds by animation events. For exmaple: footsteps, land...
public class SoundTrigger : MonoBehaviour
{
	public AudioClip[] randomClips;
	
	private int lastRandomIndex = -1;
	
	public void PlaySound(AudioClip clip)
	{
		Sound.Play(clip);
	}
	
	public void PlayRandomSound()
	{
		if (randomClips.Length != 0)
		{
			int tryCount = 0;
			while (true)
			{
				int index = Random.Range(0, randomClips.Length);
				if (randomClips.Length > 1 && index == lastRandomIndex && tryCount < 20)
					continue;
				Sound.Play(randomClips[index]);
				lastRandomIndex = index;
				break;
			}
		}
	}

	public void Spawn(GameObject prefab)
	{
		StaticAssets.Spawn(prefab, transform.position, 0f);
	}
	
	public void SpawnOnTop(GameObject prefab)
	{
		StaticAssets.Spawn(prefab, transform.position, 1f);
	}
}
