using UnityEngine;
using System.Collections;

public class CameraCallbacks : MonoBehaviour
{
	public MonoBehaviour target;
	public string methodName;
	
	public void OnPostRender()
	{
		if (target)
		{
			target.BroadcastMessage(methodName);
		}
	}
	
}
