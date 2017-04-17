using Jitter.Dynamics;
using UnityEngine;

public class JRaycastHit
{
	public RigidBody Rigidbody { get; private set; }
	public Vector3 Point { get; private set; }
	public Vector3 Normal { get; private set; }
	public float Distance { get; private set; }

	public JRaycastHit(RigidBody rigidbody, Vector3 normal, Vector3 origin, Vector3 direction, float fraction)
	{
		Rigidbody = rigidbody;
		Normal = normal;
		Point = origin + direction * fraction;
		Distance = fraction * direction.magnitude;
	}
}