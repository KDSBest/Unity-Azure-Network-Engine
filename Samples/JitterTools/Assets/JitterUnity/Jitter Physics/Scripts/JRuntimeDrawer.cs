using System;
using Jitter;
using Jitter.LinearMath;
using UnityEngine;

public class JRuntimeDrawer : IDebugDrawer
{
	public static readonly JRuntimeDrawer Instance = new JRuntimeDrawer();

	public void DrawLine(JVector start, JVector end)
	{
		Gizmos.DrawLine(start.ToVector3(), end.ToVector3());
	}

	public void DrawPoint(JVector pos)
	{
		float f = .05f;
		var dx = f * JVector.Right;
		var dy = f * JVector.Up;
		var dz = f * JVector.Forward;

		Gizmos.DrawLine((pos - dx).ToVector3(), (pos + dx).ToVector3());
		Gizmos.DrawLine((pos - dy).ToVector3(), (pos + dy).ToVector3());
		Gizmos.DrawLine((pos - dz).ToVector3(), (pos + dz).ToVector3());
	}

	public void DrawTriangle(JVector pos1, JVector pos2, JVector pos3)
	{
		if ((Camera.current.transform.position - pos1.ToVector3()).sqrMagnitude > 625)
			return;

		Gizmos.DrawLine(pos1.ToVector3(), pos2.ToVector3());
		Gizmos.DrawLine(pos2.ToVector3(), pos3.ToVector3());
		Gizmos.DrawLine(pos3.ToVector3(), pos1.ToVector3());
	}

	private void SetElement(ref JVector v, int index, float value)
	{
		if (index == 0)
			v.X = value;
		else if (index == 1)
			v.Y = value;
		else if (index == 2)
			v.Z = value;
		else
			throw new ArgumentOutOfRangeException("index");
	}

	private float GetElement(JVector v, int index)
	{
		if (index == 0)
			return v.X;
		if (index == 1)
			return v.Y;
		if (index == 2)
			return v.Z;

		throw new ArgumentOutOfRangeException("index");
	}

	public void DrawAabb(JVector from, JVector to)
	{
		var halfExtents = (to - from) * 0.5f;
		var center = (to + from) * 0.5f;

		var edgecoord = new JVector(1f, 1f, 1f);
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				var pa = new JVector(edgecoord.X * halfExtents.X, edgecoord.Y * halfExtents.Y, edgecoord.Z * halfExtents.Z);
				pa += center;

				int othercoord = j % 3;
				SetElement(ref edgecoord, othercoord, GetElement(edgecoord, othercoord) * -1f);
				var pb = new JVector(edgecoord.X * halfExtents.X, edgecoord.Y * halfExtents.Y, edgecoord.Z * halfExtents.Z);
				pb += center;

				DrawLine(pa, pb);
			}
			edgecoord = new JVector(-1f, -1f, -1f);
			if (i < 3)
				SetElement(ref edgecoord, i, GetElement(edgecoord, i) * -1f);
		}
	}
}