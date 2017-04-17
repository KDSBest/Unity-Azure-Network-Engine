using UnityEngine;

public static class GeometryUtilities
{
	public static Vector3 GetArbitraryPerpendicular(Vector3 v)
	{
		Vector3 result;
		result.x = v.y - v.z + v.y * v.z;
		result.y = v.z - v.x + v.z * v.x;
		result.z = v.x - v.y - 2 * v.x * v.y;
		result.Normalize();

		return result;
	}
}