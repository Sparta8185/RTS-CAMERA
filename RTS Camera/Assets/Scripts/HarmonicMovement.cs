using UnityEngine;

public class HarmonicMovement : MonoBehaviour
{
	public float scaleLine = 1f;
	public float speed = 10f;

	private Vector3 startPoint;
	private Vector3[] points;

	private int currentPoint = 0;

	public void OnValidate()
	{
		SetPoints();
	}

	public void SetPoints()
	{
		if (!Application.isPlaying)
			startPoint = transform.position;


		points = new Vector3[2]
		{
			// from
			startPoint - new Vector3(scaleLine, 0, 0),
			// to
			startPoint + new Vector3(scaleLine, 0, 0)
		};
	}

	public void Start()
	{
		SetPoints();
	}

	public void Update()
	{
		int currentIndex = currentPoint % 2; // 0 or 1

		Vector3 target = points[currentIndex];

		// if target reached
		if (DetectToReach(target))
		{
			currentPoint++;
			return;
		}
		
		// get distance between target and object
		float distance = target.x - transform.position.x;

		float movementX = distance * Time.deltaTime * speed;

		transform.Translate(movementX, 0, 0);
	}

	private bool DetectToReach(Vector3 target)
	{
		float distance = target.x - transform.position.x;

		bool isReached = ((distance < 0.1f && distance >= 0) || (distance > -0.1f && distance <= 0));

		return isReached; 
	}

	public void OnDrawGizmosSelected()
	{
		SetPoints();

		Gizmos.color = Color.red;

		Gizmos.DrawLine(points[0], points[1]);
	}
}
