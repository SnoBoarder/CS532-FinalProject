using UnityEngine;
using System.Collections;
using System.IO;

public class StartMain : MonoBehaviour
{
	public const int PADDING = 20;

	public const int BUTTON_WIDTH = 300;
	public const int BUTTON_HEIGHT = 150;

	public const int SELECTION_WIDTH = 300;

	private string[] _files;

	private string[] _fileNames;

	public int selectionGrid = -1;

	// Use this for initialization
	void Start ()
	{
		refreshList();
    }
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	private void refreshList()
	{
		_files = Directory.GetFiles(Application.persistentDataPath);
		_fileNames = new string[_files.Length];

		for (int i = 0; i < _fileNames.Length; ++i)
		{
			_fileNames[i] = Path.GetFileName(_files[i]);
		}
	}

	void OnGUI ()
	{
		GUI.Label(new Rect((Screen.width - SELECTION_WIDTH) / 2, PADDING, SELECTION_WIDTH, BUTTON_HEIGHT), "CS532 - Final Project\nBy Brian Tran");

		if (GUI.Button(new Rect(PADDING, (Screen.height - BUTTON_HEIGHT) / 2, BUTTON_WIDTH, BUTTON_HEIGHT), "Create Map"))
		{
			Application.LoadLevel("CreateMeshScene");
		}

		selectionGrid = GUI.SelectionGrid(new Rect(Screen.width - SELECTION_WIDTH, 0, SELECTION_WIDTH, Screen.height), selectionGrid, _fileNames, 1);

		if (GUI.Button(new Rect(Screen.width - SELECTION_WIDTH - BUTTON_WIDTH - PADDING, (Screen.height - BUTTON_HEIGHT) / 2 - BUTTON_HEIGHT, BUTTON_WIDTH, BUTTON_HEIGHT), "Play Selected Map"))
		{
			Debug.Log("Playing " + selectionGrid  + ": " + _files[selectionGrid]);

			Data.fileName = _files[selectionGrid];

			Application.LoadLevel("GameScene");
		}

		if (GUI.Button(new Rect(Screen.width - SELECTION_WIDTH - BUTTON_WIDTH - PADDING, (Screen.height - BUTTON_HEIGHT) / 2 + BUTTON_HEIGHT, BUTTON_WIDTH, BUTTON_HEIGHT), "Delete Selected Map"))
		{
			File.Delete(_files[selectionGrid]);

			refreshList();
		}
	}
}
