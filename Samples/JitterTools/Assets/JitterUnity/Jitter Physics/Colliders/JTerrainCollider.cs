using System;
using Jitter.Collision.Shapes;
using UnityEngine;

[AddComponentMenu("Component/Jitter Physics/Terrain Collider")]
[RequireComponent(typeof(Terrain))]
public class JTerrainCollider : JCollider
{
	public int Resolution
	{
		get
		{
			var terrain = GetComponent<Terrain>();
			var data = terrain.terrainData;
			int resolusion = data.heightmapResolution;
			return resolusion;
		}
	}

	public Vector3 Size
	{
		get
		{
			var terrain = GetComponent<Terrain>();
			var data = terrain.terrainData;
			return data.size;
		}
	}

	private void Reset()
	{
		UpdateShape();
	}

	public override Shape CreateShape()
	{
		var terrain = GetComponent<Terrain>();
		var data = terrain.terrainData;
		int resolusion = data.heightmapResolution;
		var heights = data.GetHeights(0, 0, resolusion, resolusion);
		float verticalScale = data.size.y;
		for (int x = 0; x < resolusion; x++)
		{
			for (int z = 0; z < resolusion; z++)
				heights[x, z] *= verticalScale;
		}
		for (int x = 0; x < resolusion - 1; x++)
		{
			for (int z = x; z < resolusion; z++)
			{
				float h1 = heights[x, z];
				float h2 = heights[z, x];
				heights[x, z] = h2;
				heights[z, x] = h1;
			}
		}

		var result = new TerrainShape(heights, data.size.x / (resolusion - 1), data.size.z / (resolusion - 1));
		return result;
	}

	public override CompoundShape.TransformedShape CreateTransformedShape(JRigidBody body)
	{
		throw new NotImplementedException();
	}
}