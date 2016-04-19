using UnityEngine;
using System.Collections;

// This script handles the clouds behind the castle.
// The clouds loop when they move out of bounds.
public class BackgroundCloud : MonoBehaviour
{
	public Vector3 speed;
	
	public Transform max;
	public Transform min;
	
	private Transform t;
	
	public bool randomY = true;
	
	void Start()
	{
		t = transform;
		Vector3 minPosition = min.position;
		Vector3 maxPosition = max.position;
		Vector3 position = Vector3.zero;
		position.x = Random.Range(minPosition.x, maxPosition.x);
		if (randomY)
			position.y = Random.Range(minPosition.y, maxPosition.y);
		//position.z = Random.Range(minPosition.z, maxPosition.z);
		t.position = position;
	}
	
	void Update()
	{
		Vector3 position = t.position;
		position += speed * Time.deltaTime;
		Vector3 minPosition = min.position;
		Vector3 maxPosition = max.position;
		Vector3 delta = maxPosition - minPosition;
		while (position.x > maxPosition.x)
		{
			position.x -= delta.x;
			if (randomY)
				position.y = Random.Range(minPosition.y, maxPosition.y);
		}
		while (position.x < minPosition.x)
		{
			position.x += delta.x;
			if (randomY)
				position.y = Random.Range(minPosition.y, maxPosition.y);
		}
		position.y = Mathf.Clamp(position.y, minPosition.y, maxPosition.y);
		position.z = Mathf.Clamp(position.z, minPosition.z, maxPosition.z);
		t.position = position;
	}
	
}
