using Jitter.Collision.Shapes;
using UnityEngine;

[AddComponentMenu("Component/Jitter Physics/Cone Collider")]
public class JConeCollider : JCollider
{
	[SerializeField] private float radius;
	public float Radius
	{
		get { return radius; }
		set { radius = value; }
	}

	[SerializeField] private float height;
	public float Height
	{
		get { return height; }
		set { height = value; }
	}

	public void Reset()
	{
		var meshFilter = GetComponent<MeshFilter>();
		var mesh = meshFilter.sharedMesh;
		var size = mesh.bounds.size;
		var scale = transform.localScale;
		size = new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);

		radius = size.x > size.z ? size.x / 2 : size.z / 2;
		height = size.y;
		UpdateShape();
	}

	public override Shape CreateShape()
	{
		return new ConeShape(height, radius);
	}
}