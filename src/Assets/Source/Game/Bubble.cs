using UnityEngine;
using System.Collections;

// This class handles the bubble animation effect.
public class Bubble : MonoBehaviour
{
	public Vector3 speed = Vector3.one;
	public Vector3 amplitude = Vector3.one;
	public Transform modelRoot;
	public ParticleSystem particles;
	
	private float time;
	private Transform t;
	private Vector3 originalScale;
	private float scaleK;
	private bool destroyRequested;
	
	void Start()
	{
		originalScale = modelRoot.localScale;
		Sound.Play(Sound.instance.fxBubbleAppear);
	}
	
	void Update()
	{
		if (destroyRequested)
		{
			if (!particles.IsAlive())
				GameObject.Destroy(gameObject);
			return;
		}
		else
		{
			scaleK = Mathf.Lerp(scaleK, 1f, Time.deltaTime * 10f);
		}		
		
		time += Time.deltaTime;
		Vector3 scale = originalScale;
		scale.x += Mathf.Cos(time * speed.x) * amplitude.x * originalScale.x;
		scale.y += Mathf.Cos(time * speed.y) * amplitude.y * originalScale.y;
		scale.z += Mathf.Cos(time * speed.z) * amplitude.z * originalScale.z;
		scale *= scaleK;
		modelRoot.localScale = scale;
		Sound.RequestPlayBubbles();
	}
	
	public void RequestDestroy()
	{
		destroyRequested = true;
		particles.gameObject.SetActive(true);
		modelRoot.gameObject.SetActive(false);
	}
}
