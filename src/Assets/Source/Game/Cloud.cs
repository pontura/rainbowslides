using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class contains the animation code of a rain cloud.
public class Cloud : MonoBehaviour
{
	[System.Serializable]
	public class CloudData
	{
		public Transform t;
		private float time;
		private Vector3 originalScale;

		public void Update(Vector3 speed, Vector3 amplitude)
		{
			if (time == 0f)
			{
				originalScale = t.localScale;
				time = Random.Range(0, 1000f);
			}
			time += Time.deltaTime;
			Vector3 scale = originalScale;
			scale.x += Mathf.Cos(time * speed.x) * amplitude.x * originalScale.x;
			scale.y += Mathf.Cos(time * speed.y) * amplitude.y * originalScale.y;
			scale.z += Mathf.Cos(time * speed.z) * amplitude.z * originalScale.z;
			t.localScale = scale;
		}
	}
	
	// The clound is made of many small cloulds.
	public List<CloudData> clouds = new List<CloudData>();
	public Vector3 cloudSpeed = Vector3.one;
	public Vector3 cloudAmplitude = Vector3.one;
	
	// Drops variables...
	public GameObject drop;
	public int dropCount;
	public float dropSpeed = 10f;
	public Vector3 dropSpawnRadius = Vector3.one;
	public float dropMaxDistance = 1f;
	public float dropSpawnFrequency = 0.1f;
	public float dropSpawnFrequencyRnd = 0.1f;
	public AnimationCurve dropScaleCurve = new AnimationCurve();
	private float dropNextSpawnTime;
	private List<Transform> drops = new List<Transform>();
	private static float nextThunderSoundTime;
	private Vector3 dropOriginalScale;
	
	// What it should follow?
	public Transform target;
	// the height at which the could should follow the target
	public Vector3 targetOffset = new Vector3(0, 3, 0);
	// The movement speed.
	public float catchMoveSpeed = 5f;

	// Used to animate the transform scale.
	private float scale = 0f;
	
	// Should this cloud destroy itself? A smooth fade out scaling will be performed, and then it will be destroyed.
	private bool destroyRequested = false;
	
	// Cahced transform reference.
	[HideInInspector]
	public Transform _t;
	
	// After the character loses its turn, the cloud will be "consumed".
	// This flag must be reset if the cloud is stolen.
	public bool consumed;
	
	void Awake()
	{
		_t = transform;
	}
	
	void Start()
	{
		if (drop)
		{
			dropOriginalScale = drop.transform.localScale;
			for (int i = 0; i < dropCount; i++)
			{
				GameObject go = (GameObject)GameObject.Instantiate(drop);
				go.transform.parent = _t;
				go.transform.localPosition = Vector3.zero;
				drops.Add(go.transform);
			}
			drop.SetActive(false);
		}
		
		Sound.Play(Sound.instance.fxCloudAppear);
		nextThunderSoundTime = Time.realtimeSinceStartup + Random.Range(10, 20);
	}
	
	void Update()
	{
		if (destroyRequested)
		{
			scale = Mathf.Lerp(scale, 0f, Time.deltaTime * 10f);
			if (scale < 0.1f)
			{
				GameObject.Destroy(gameObject);
				return;
			}
		}
		else
		{
			scale = Mathf.Lerp(scale, 1f, Time.deltaTime * 10f);
			
			// Move towards the target.
			if (target)
			{
				Vector3 position = _t.position;
				position = Vector3.Lerp(position, target.position + targetOffset, Time.deltaTime * catchMoveSpeed);
				_t.position = position;
			}
		}
		
		_t.localScale = Vector3.one * scale;
		
		foreach (CloudData c in clouds)
			c.Update(cloudSpeed, cloudAmplitude);
		
		Vector3 delta = Vector3.down * Time.deltaTime * dropSpeed;
		foreach (Transform t in drops)
		{
			Vector3 pos = t.localPosition;
			if (pos.y < -dropMaxDistance || !t.gameObject.activeSelf)
			{
				t.gameObject.SetActive(false);
				if (Time.time > dropNextSpawnTime)
				{
					dropNextSpawnTime = Time.time + dropSpawnFrequency + Random.value * dropSpawnFrequencyRnd;
					pos = Random.insideUnitSphere;
					pos.x *= dropSpawnRadius.x;
					pos.y *= dropSpawnRadius.y;
					pos.z *= dropSpawnRadius.z;
					t.localPosition = pos;
					t.localScale = dropOriginalScale;
					t.gameObject.SetActive(true);
				}
			}
			// its alive
			else
			{
				// move the drop down
				pos += delta;
				t.localPosition = pos;
				
				// scale the drop
				t.localScale = dropOriginalScale * dropScaleCurve.Evaluate(-pos.y / dropMaxDistance);
			}
		}
		
		if (Time.realtimeSinceStartup > nextThunderSoundTime)
		{
			nextThunderSoundTime = Time.realtimeSinceStartup + Random.Range(10, 20);
			Sound.Play(Sound.instance.fxCloudThunder);
		}
		Sound.RequestPlayRain();
	}
	
	public void InstantMoveToTarget()
	{
		if (target)
			_t.position = target.position + targetOffset;
	}
	
	public void RequestDestroy()
	{
		destroyRequested = true;
	}
	
}
