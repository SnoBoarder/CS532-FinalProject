using UnityEngine;
using System.Collections;

public class Data : MonoBehaviour
{
	public static string fileName = "";
	public static int voxelResolution = 10;

	void Awake()
	{
		// keep this around for data transfering to scenes
		DontDestroyOnLoad(transform.gameObject);
	}
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
