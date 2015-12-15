using UnityEngine;
using System.Collections;

public class Target : MonoBehaviour
{

	public static event TargetHitHandler TargetHit;
	public delegate void TargetHitHandler();

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider other)
	{
		Debug.Log("OMG");

		gameObject.SetActive(false);
		other.gameObject.SetActive(false);

		if (TargetHit != null)
		{
			TargetHit();
        }
	}
}
