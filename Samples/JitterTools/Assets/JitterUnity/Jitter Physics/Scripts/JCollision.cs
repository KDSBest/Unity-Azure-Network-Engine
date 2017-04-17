using Jitter.Dynamics;
using UnityEngine;

public class JCollision
{
	public Vector3 Point { get; private set; }
	public Vector3 Normal { get; private set; }
	public float Penetration { get; private set; }
	public RigidBody Body1 { get; private set; }
	public RigidBody Body2 { get; private set; }

	public JCollision(RigidBody body1, RigidBody body2, Vector3 point, Vector3 normal, float penetration)
	{
		Body1 = body1;
		Body2 = body2;
		Point = point;
		Normal = normal;
		Penetration = penetration;
	}

	public override string ToString()
	{
		return "Normal: " + Normal + ", penetration: " + Penetration;
	}
}