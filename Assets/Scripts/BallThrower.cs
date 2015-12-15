using System.Collections;
using UnityEngine;

public class BallThrower : MonoBehaviour
{
    public GameObject ballPrefab;
    public Camera mainCamera;
    private float forwardVelocity = 10.0f;
    
    private GameObject[] ballArray = new GameObject[10];
    private int currentBallID = 0;
    
    private void Start()
    {
        for (int i = 0; i < ballArray.Length; i++)
        { 
            ballArray[i] = (GameObject)Instantiate(ballPrefab);
            ballArray[i].SetActive(false);
            ballArray[i].transform.parent = transform;
        }
        currentBallID = 0;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
			ShootBall();
		}

        for (var i = 0; i < Input.touchCount; ++i)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
				ShootBall();
            }
        }
    }

	private void ShootBall()
	{
		ballArray[currentBallID].transform.position = mainCamera.transform.position - (mainCamera.transform.up * ballPrefab.transform.localScale.y);
		ballArray[currentBallID].GetComponent<Rigidbody>().velocity = (mainCamera.transform.forward * forwardVelocity);
		ballArray[currentBallID].SetActive(true);
		currentBallID = (currentBallID + 1) % ballArray.Length;
	}
}
