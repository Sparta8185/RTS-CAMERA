using UnityEngine;


internal static class CameraExtensions
{
	internal static Vector3 Lein(this Vector3 first, Vector3 target, float step = 30)
	{
		Vector3 current = Vector3.Lerp(first, target, step * Time.deltaTime);
		return current;
	}

	internal static Quaternion Lein(this Quaternion first, Vector3 target, float step = 30)
	{
		Quaternion targetRotation = Quaternion.Euler(target);
		Quaternion current = Quaternion.Lerp(first, targetRotation, step * Time.deltaTime);

		return current;
	}
}	
