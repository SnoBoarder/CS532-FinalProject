using UnityEngine;
using System.Collections;

public class MiniGame1 : MiniGameBase
{
	private const float TARGET_SPAWN_DELAY = 1.0f;

	public GameObject targetPrefab;

	private int _totalScore = 0;

	private GameObject[] targets = new GameObject[10];
	private int currentTargetID = 0;

	private float _spawnTime = TARGET_SPAWN_DELAY;

	private RaycastHit hit;

	private Vector3 _origin = new Vector3(0.0f, 1.4f, 0.0f);
	private Vector3 _direction = Vector3.zero;

	void Start()
	{
		Target.TargetHit += OnTargetHit;

		for (int i = 0; i < targets.Length; i++)
		{
			targets[i] = (GameObject)Instantiate(targetPrefab);
			targets[i].SetActive(false);
			targets[i].transform.parent = transform;
		}
		currentTargetID = 0;
    }

	void OnGUI()
	{
		GUI.contentColor = Color.red;

		GUI.Label(new Rect(10, 180, 1000, 30), "Score: " + _totalScore);
	}

	protected override void UpdateGame()
	{
		_spawnTime -= Time.deltaTime;

		if (_spawnTime <= 0)
		{
			_direction.x = Random.Range(-30, 30);
			_direction.y = Random.Range(-30, 30);
			_direction.z = Random.Range(-30, 30);

			bool raycastSuccessful = Physics.Raycast(_origin, _direction, out hit);

			if (raycastSuccessful)
			{
				_spawnTime = TARGET_SPAWN_DELAY;

				GameObject target = targets[currentTargetID];
				target.transform.position = hit.collider.transform.position;//Vector3.zero;//mainCamera.transform.position - (mainCamera.transform.up * ballPrefab.transform.localScale.y);
				target.SetActive(true);
				currentTargetID = (currentTargetID + 1) % targets.Length;
			}
		}
    }

	private void OnTargetHit()
	{
		_totalScore += 500;
    }
}
