using UnityEngine;
using System.Collections;

public class MiniGameBase : MonoBehaviour
{
	public Camera mainCamera;

	protected bool _started = false;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (_started)
		{
			UpdateGame();
        }
	}

	public void startGame()
	{
		_started = true;
    }

	protected virtual void UpdateGame()
	{

	}
}
