using System;
using System.Collections.Generic;
using System.Linq;
using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.LinearMath;
using UnityEngine;

[AddComponentMenu("Component/Jitter Physics/Mesh Collider")]
public class JMeshCollider : JCollider
{
	[SerializeField] private Mesh mesh;
	public Mesh Mesh
	{
		get { return mesh; }
		set
		{
			mesh = value;
			vertices = GetVertices();
			indices = GetIndices();
		}
	}

	private List<JVector> vertices;
	public List<JVector> Vertices
	{
		get
		{
			if (vertices == null)
				vertices = GetVertices();
			return vertices;
		}
	}

	private List<TriangleVertexIndices> indices;
	public List<TriangleVertexIndices> Indices
	{
		get
		{
			if (indices == null)
				indices = GetIndices();
			return indices;
		}
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
		var octree = new Octree(Vertices, Indices);
		return new TriangleMeshShape(octree);
	}

	public override CompoundShape.TransformedShape CreateTransformedShape(JRigidBody body)
	{
		throw new NotImplementedException();
	}

	private List<TriangleVertexIndices> GetIndices()
	{
		var triangles = mesh.triangles;
		var result = new List<TriangleVertexIndices>();
		for (int i = 0; i < triangles.Length; i += 3)
			result.Add(new TriangleVertexIndices(triangles[i + 2], triangles[i + 1], triangles[i + 0]));
		return result;
	}

	private List<JVector> GetVertices()
	{
		var scale = transform.localScale;
		var result = mesh.vertices.Select(p => new JVector(p.x * scale.x, p.y * scale.y, p.z * scale.z)).ToList();
		return result;
	}
}