using System;
using UnityEngine;

public struct JControllerColliderHit : IComparable<JControllerColliderHit>
{
	public readonly JRigidBody body;
	public readonly Vector3 point;
	public readonly Vector3 normal;
	public readonly Vector3 moveDirection;
	public readonly float penetration;

	public JControllerColliderHit(JRigidBody body, Vector3 point, Vector3 normal, Vector3 moveDirection, float penetration)
	{
		this.body = body;
		this.point = point;
		this.normal = normal;
		this.moveDirection = moveDirection;
		this.penetration = penetration;
	}

	public int CompareTo(JControllerColliderHit other)
	{
		return penetration.CompareTo(other.penetration);
		//return moveDirection.sqrMagnitude.CompareTo(other.moveDirection.sqrMagnitude);
	}
}