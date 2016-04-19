using UnityEngine;
using System.Collections;

// This class handles the character's rounded floor shadow.
public class FloorShadow : MonoBehaviour
{
	public float targetScale = 1f;
	
	// Cached reference to the shadow.
	public Renderer _renderer;
	
	private float currentScale = 1f;
	private Vector3 originalScale;
	private Transform _t;
	
	void Awake()
	{
		_t = transform;
		originalScale = _t.localScale;
	}
	
	void LateUpdate()
	{
		currentScale = Mathf.Lerp(currentScale, targetScale, 10f * Time.deltaTime);
		_t.localScale = originalScale * currentScale;
		
		// make sure the head its pointing up
		_t.rotation = Quaternion.FromToRotation(_t.up, Vector3.up) * _t.rotation;
	}
}
