using System;
using System.Linq;
using Jitter.Collision.Shapes;
using UnityEngine;

[AddComponentMenu("Component/Jitter Physics/Convex Hull Collider")]
public class JConvexHullCollider : JCollider
{
	[SerializeField] private Mesh mesh;
	public Mesh Mesh
	{
		get { return mesh; }
		set { mesh = value; }
	}

	public void Reset()
	{
		if (mesh == null)
		{
			var meshFilter = GetComponent<MeshFilter>();
			mesh = meshFilter.sharedMesh;
		}

		UpdateShape();
	}

	public override Shape CreateShape()
	{
		var positions = mesh.vertices.Select(p => p.ToJVector()).ToList();
		return new ConvexHullShape(positions);
	}
}