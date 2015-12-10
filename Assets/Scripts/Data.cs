using UnityEngine;
using System.Collections;

public class Data : MonoBehaviour
{
	public string fileName = "";

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
