using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
	#region Input Settings
	public enum MouseKeyCode : int
	{
		None = -1,
		Left = 0,
		Right = 1,
		Middle = 2
	}
	#endregion

	#region Camera Data Structers

	[System.Serializable]
	public class ScreenFollowSettings
	{
		public float sensivity = 50f;
		public float screenBorder = 0.02f;

		[Space]
		public bool moveMouseWhenOutOfScreen = false;
	}

	[System.Serializable]
	public class MovementSettings
	{

		[Tooltip("Note: You can off mouse input with MouseKeyCode.None")]
		[Header("Input Binding")]
		public MouseKeyCode mouseInput = MouseKeyCode.Right;

		public bool canUseKeyboard = true;
	
		[Space]
		public float keySensivity = 100f;
		
		/// <summary>
		/// "Note: when under 100, it is going to be unwanted shake"
		/// </summary>
		//[HideInInspector]
		public float speed = 1000f;

		[Space]
		public ScreenFollowSettings screenFollowSettings;
	}


	[System.Serializable]
	public class RotationSettings
	{
		[Header("Input Binding")]
		[Tooltip("Note: You can off mouse input with MouseKeyCode.None")]
		public MouseKeyCode mouseKey = MouseKeyCode.Middle;
		[Tooltip("Note: You can off mouse input with KeyCode.None")]
		public KeyCode leftKey = KeyCode.Q;
		[Tooltip("Note: You can off mouse input with KeyCode.None")]
		public KeyCode rightKey = KeyCode.E;


		[Space]
		public float mouseSensivity = 60f;
		public float keySensivity = 50f;
		public float speed = 40f;

		[Space]
		public float min = 20f;

		[HideInInspector]
		public float current = 35f;

		public float max = 80f;
	}

	[System.Serializable]
	public class DistanceSettings
	{
		[Header("Input Binding")]
		[Tooltip("Note: You can off mouse input with KeyCode.None")]
		public KeyCode unzoom = KeyCode.Z;

		[Tooltip("Note: You can off mouse input with KeyCode.None")]
		public KeyCode zoom = KeyCode.X;

		[Space]
		public bool canUseScroll = true;

		[Space]
		public float mouseSensivity = 70f;
		public float keySensivity = 10f;
		public float speed = 40f;

		[Space]
		public float min = 10f;
		public float current = 35f;
		public float max = 200f;

	}

	private struct MovementData 
	{
		public static Vector3 firstPosition, targetPosition;
	}
	private struct RotationData
	{
		public static bool isRotationTop,
											 isFirst;

		public static Vector3 firstPosition, 
													targetRotation;
	}

	#endregion

	#region Inspector Fields

	[SerializeField]
	public MovementSettings movementSettings = new MovementSettings();
	
	[Space]
	[SerializeField]
	public RotationSettings rotationSettings = new RotationSettings();

	[Space]
	[SerializeField]
	public DistanceSettings distanceSettings = new DistanceSettings();

	/// <summary>
	/// The Camera's Following Object
	/// </summary>
	[Space]
	public GameObject FollowingObject;
	#endregion

	#region Fields

	/// <summary>
	/// Just a plane
	/// </summary>
	private Plane plane;
	#endregion

	/// <summary>
	/// Unity Constructor
	/// </summary>
	public void Awake()
	{
		SetCurrentData();
		// Set Smooth Distance
		SmoothDistance();

		// Create a plane to calculate mouse paint on world location
		plane = new Plane(Vector3.up, Vector3.down);
	}

	/// <summary>
	/// Handler of Player Input
	/// </summary>
	public void Update()
	{
		{ // Need a event system/canvas 
			//bool isMouseOverUI = EventSystem.current.IsPointerOverGameObject();

			//if (isMouseOverUI) return;
		}

		{ // Distance Handler
			OnClosingWithMouse();
			OnClosingWithKeyboard();
		} 

		OnRotating();

		{ // Movement Handler
			OnCameraMovementWithMouse();
			OnCameraMovementWithKeyboard();
		}

		FollowMouseOnBorder();

		// if has following object
		if (FollowingObject)
			FollowObject();
	}

	/// <summary>
	/// Process of Player Input
	/// </summary>
	public void LateUpdate()
	{
		SmoothPosition();
		SmoothDistance();
		SmoothRotation();
	}

	/// <summary>
	/// For draw a sphere on camera's center
	/// </summary>
	public void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;

		Gizmos.DrawWireSphere(transform.localPosition, 1f);
	}

	#region Process

	/// <summary>
	/// Camera position movement
	/// </summary>
	private void SmoothPosition()
	{
		transform.localPosition = transform.localPosition.Lein(MovementData.targetPosition, movementSettings.speed);
	}

	/// <summary>
	/// Camera center distance
	/// </summary>
	private void SmoothDistance()
	{
		Camera camera = Camera.main;

		// Setting the distance of camera's center
		Vector3 cameraPosition = transform.position - camera.transform.forward * distanceSettings.current;

		camera.transform.position = camera.transform.position.Lein(cameraPosition, distanceSettings.speed);
	}

	// Camera core rotation 
	private void SmoothRotation()
	{
		transform.localRotation = transform.localRotation.Lein(RotationData.targetRotation, rotationSettings.speed);
	}

	#endregion

	#region Handle Movement Input
	private void OnCameraMovementWithMouse()
	{
		int mouseInput = (int)movementSettings.mouseInput;
		// If not clicking right click
		if (!Input.GetMouseButton(mouseInput))
			return;

		// if has following object set to null
		if (FollowingObject)
			FollowingObject = null;
		
		Vector3 mousePosition = GetMousePosition();
		Camera camera = Camera.main;

		Plane plane = new Plane(Vector3.up, Vector3.down);

		Ray ray = camera.ScreenPointToRay(mousePosition);

		float entry;

		// if raycast does not cross a plane 
		if (!plane.Raycast(ray, out entry))
			return;

		// if mouse clicked right click
		if (Input.GetMouseButtonDown(mouseInput))
		{
			MovementData.firstPosition = ray.GetPoint(entry);
			return;
		}
		
		// get the raycast point
		Vector3 mousePoint = ray.GetPoint(entry);

		// calculate distance between current position and mouse position 
		Vector3 difference = MovementData.firstPosition - mousePoint;

		TranslatePosition(difference);
	}

	/// <summary>
	/// When mouse is on border of screen move the camera to direction of mouse 
	/// </summary>
	private void FollowMouseOnBorder() 
	{
		// if the feature is not open
		if (!movementSettings.screenFollowSettings.moveMouseWhenOutOfScreen)
			return;
		
		// if mouse out of screen
		if (IsMouseOutOfScreen())
		{
			// center of screen
			Vector3 viewportCenter = new Vector3(Screen.width / 2, Screen.height / 2);

			// distance between mouse position and center of screen
			Vector3 distance = Input.mousePosition - viewportCenter;

			// to move on z axis not y 
			distance.z = distance.y;
			distance.y = 0;

			distance.Normalize();

			Vector3 movement = distance * movementSettings.screenFollowSettings.sensivity * Time.deltaTime;

			TranslatePosition(movement);
		}
	}
	
	private void OnCameraMovementWithKeyboard()
	{
		// if can not use keyboard
		if (!movementSettings.canUseKeyboard) return;

		float horizontal = Input.GetAxisRaw("Horizontal"),
					vertical = Input.GetAxisRaw("Vertical");

		// if has a player input, turn of following object
		if (horizontal != 0 || vertical != 0)
			FollowingObject = null;
		
		Vector3 forward = Camera.main.transform.forward;

		// to not change the height
		forward.y = 0f; 

		forward.Normalize();

		Vector3 right = Camera.main.transform.right;

		right.y = 0f; // yuksekligin degismemesi gerekiyor
		right.Normalize();

		// Create movement with inputs
		Vector3 verticalMovement = vertical * forward,
						horizontalMovement = horizontal * right;

		// merge
		Vector3 movement = verticalMovement + horizontalMovement;

		// adjust settings
		movement *= movementSettings.keySensivity * Time.deltaTime;

		TranslatePosition(movement);
	}
	#endregion

	#region Handle Distance Input

	/// <summary>
	/// Handle camera center's center change
	/// </summary>
	private void OnClosingWithMouse()
	{
		// if can not use scroll
		if (!distanceSettings.canUseScroll) return;

		float scrollWhell = Input.GetAxis("Mouse ScrollWheel");

		// Calculate mouse taken distance
		float takenDistance = -scrollWhell * distanceSettings.mouseSensivity;

		TranslateDistance(takenDistance);

	}
	private void OnClosingWithKeyboard()
	{
		float takenDistance = 0;
		
		if (Input.GetKey(distanceSettings.zoom))
			takenDistance = -0.1f;
		else if (Input.GetKey(distanceSettings.unzoom))
			takenDistance = 0.1f;

		takenDistance *= distanceSettings.keySensivity;

		TranslateDistance(takenDistance);
	}

	#endregion

	#region Handle Rotation Input

	private void OnRotating()
	{
		int mouseKey = (int)rotationSettings.mouseKey;

		// if clicking middle
		if (Input.GetMouseButton(mouseKey))
			OnRotatingWithMouse();

		float keyPressed = Input.GetKey(rotationSettings.leftKey) ? -1f : Input.GetKey(rotationSettings.rightKey) ? 1f : 0;

		if (keyPressed != 0)
			OnRotatingWithKeyboard(keyPressed);
	}

	

	private void OnRotatingWithMouse()
	{
		int mouseKey = (int)rotationSettings.mouseKey;

		Vector3 mousePosition = GetMousePosition();

		// if clicked middle
		if (Input.GetMouseButtonDown(mouseKey)) 
		{
			RotationData.firstPosition = Camera.main.ScreenToViewportPoint(mousePosition);
			
			return;
		}

		// Get the cucrrent mouse viewport point[UI]
		Vector3 lastPosition = Camera.main.ScreenToViewportPoint(mousePosition);
		
		// Get the difference between first point and current
		Vector3 difference = RotationData.firstPosition - lastPosition;

		// if there is not change
		if (difference == Vector3.zero)
			return;

		// change axis for rotation
		Vector3 newRotation = new Vector3(difference.y, difference.x);

		// get current rotation
		Vector3 targetRotation = RotationData.targetRotation + newRotation * rotationSettings.mouseSensivity;

		// limit
		targetRotation.x = Mathf.Clamp(targetRotation.x, rotationSettings.min, rotationSettings.max);

		RotationData.targetRotation = targetRotation;
		RotationData.firstPosition = lastPosition;
	}


	private void OnRotatingWithKeyboard(float value)
	{
		Vector3 newRotation = new Vector3(0, value, 0);

		newRotation *= rotationSettings.keySensivity * Time.deltaTime;
		
		RotationData.targetRotation += newRotation;
	}

	#endregion

	#region Object Follow
	private void FollowObject()
	{
		Vector3 newPosition = FollowingObject.transform.position;

		MovementData.targetPosition = newPosition;
	}
	#endregion

	#region General Methods
	public void TranslatePosition(Vector3 translatingPosition)
	{
		MovementData.targetPosition += translatingPosition;
	}

	public void SetPosition(Vector3 newCameraPosition)
	{
		MovementData.targetPosition = newCameraPosition;
	}

	public void TranslateDistance(float translatingDistance)
	{
		float totalDistance = distanceSettings.current + translatingDistance;

		// limit
		totalDistance = Mathf.Clamp(totalDistance, distanceSettings.min, distanceSettings.max);

		distanceSettings.current = totalDistance;
	}

	/// <summary>
	/// Get Fixed Mouse Position 
	/// </summary>
	private Vector3 GetMousePosition()
	{
		Vector3 mousePosition = Input.mousePosition;
		// For not detect the camera
		mousePosition.z = Camera.main.nearClipPlane + 0.5f;

		return mousePosition;
	}

	private bool IsMouseOutOfScreen()
	{
		// border size
		float screenBorder =  movementSettings.screenFollowSettings.screenBorder;
		// screen scale
		float screenScale = 1f - screenBorder;
	
		// Detect if mouse on border
		bool isMouseOutWidth = Input.mousePosition.x >= Screen.width * screenScale 
													|| Input.mousePosition.x <= Screen.width * screenBorder;

		bool isMouseOutHeight = Input.mousePosition.y >= Screen.height * screenScale 
													|| Input.mousePosition.y <= Screen.height * screenBorder;

		bool isMouseOutScreen = isMouseOutWidth || isMouseOutHeight;

		return isMouseOutScreen;
	}
	private void SetCurrentData()
	{
		MovementData.targetPosition = transform.localPosition;
		RotationData.targetRotation = transform.localEulerAngles;
	}
	#endregion


}