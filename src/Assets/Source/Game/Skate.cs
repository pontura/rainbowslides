using UnityEngine;
using System.Collections;

// This script handles the skate billboard.
public class Skate : MonoBehaviour
{
	public GameObject rightModel;
	public GameObject leftModel;
	public GameObject forwardModel;
	
	private Vector3 originalScale;
	private Transform _t;
	private float scale = 0f;
	
	void Start()
	{
		_t = transform;
		originalScale = _t.localScale;
		_t.localScale = Vector3.zero;
	}
	
	public void UpdateModel(Transform target)
	{
		if (!_t)
			return;
		scale = Mathf.Lerp(scale, 1f, Time.deltaTime * 5f);
		_t.localScale = originalScale * scale;
		
		if (Vector3.Dot(Vector3.right, target.forward) > 0.5f)
		{
			rightModel.SetActive(true);
			leftModel.SetActive(false);
			forwardModel.SetActive(false);
		}
		else if (Vector3.Dot(Vector3.left, target.forward) > 0.5f)
		{
			rightModel.SetActive(false);
			leftModel.SetActive(true);
			forwardModel.SetActive(false);
		}
		else
		{
			rightModel.SetActive(false);
			leftModel.SetActive(false);
			forwardModel.SetActive(true);
		}
		_t.position = target.position;
	}
}
