using UnityEngine;
using System.Collections;

public class GameMain : MonoBehaviour
{
	public MiniGameBase[] miniGames;

	// Use this for initialization
	void Start ()
	{
		GameMeshListener.MeshBuildingComplete += startMiniGame;
    }
	
	// Update is called once per frame
	void Update () {
	
	}

	private void startMiniGame()
	{
		miniGames[0].startGame();
	}

	void OnGUI()
	{
		if (GUI.Button(new Rect(Screen.width - 160, 120, 140, 80), "Back to Start"))
		{
			Application.LoadLevel("StartScene");
		}
	}
}
