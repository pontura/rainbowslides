using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class contains the definition of chutes and ladders on a level configuration.
public class LevelInfo : MonoBehaviour
{
	public List<ConnectionInfo> connections = new List<ConnectionInfo>();
}

[System.Serializable]
public class ConnectionInfo
{
	// The number of cell where the connection starts.
	public int start;
	
	// The number of cell where the connection ends.
	public int end;
	
	// The model that is will be used to represent this connection.
	public GameObject model;
	
	public static int SortComparison(ConnectionInfo a, ConnectionInfo b)
	{
		return a.start.CompareTo(b.start);
	}
}
