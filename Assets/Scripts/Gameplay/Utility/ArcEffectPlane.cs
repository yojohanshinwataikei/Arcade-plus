using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcEffectPlane : MonoBehaviour
{
	public Camera ReferenceCamera;
	// Start is called before the first frame update
	public Vector3 GetPositionOnPlane(Vector3 worldPos)
	{
		Vector3 normal = -ReferenceCamera.transform.worldToLocalMatrix.inverse.MultiplyVector(Vector3.forward);
		Plane plane = new Plane(normal, transform.position);
		Ray ray = new Ray(ReferenceCamera.transform.position, (worldPos - ReferenceCamera.transform.position).normalized);
		float distance = 0.0f;
		if (plane.Raycast(ray, out distance))
		{
			return ray.GetPoint(distance);
		}
		return worldPos;
	}

	void OnDrawGizmosSelected()
	{
		if (ReferenceCamera != null)
		{
			Vector3 normal = -ReferenceCamera.transform.worldToLocalMatrix.inverse.MultiplyVector(Vector3.forward);
			Gizmos.color = Color.white;
			Gizmos.DrawLine(transform.position, transform.position + normal * 4);
		}
	}
}
