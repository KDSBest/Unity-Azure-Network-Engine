using Jitter.LinearMath;
using UnityEngine;

public static class JitterExtensions
{
	public static JVector ToJVector(this Vector3 vector)
	{
		return new JVector(vector.x, vector.y, vector.z);
	}

	public static Vector3 ToVector3(this JVector vector)
	{
		return new Vector3(vector.X, vector.Y, vector.Z);
	}

	public static Quaternion ToQuaternion(this JQuaternion rot)
	{
		return new Quaternion(rot.X, rot.Y, rot.Z, rot.W);
	}

	public static JQuaternion ToJQuaternion(this Quaternion rot)
	{
		return new JQuaternion(rot.x, rot.y, rot.z, rot.w);
	}

	public static JMatrix ToJMatrix(this Quaternion rot)
	{
		return JMatrix.CreateFromQuaternion(rot.ToJQuaternion());
	}

	public static Quaternion ToQuaternion(this JMatrix matrix)
	{
		return JQuaternion.CreateFromMatrix(matrix).ToQuaternion();
	}
}