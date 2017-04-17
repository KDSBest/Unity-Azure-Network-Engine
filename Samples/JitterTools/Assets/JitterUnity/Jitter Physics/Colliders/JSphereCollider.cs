using Jitter.Collision.Shapes;
using UnityEngine;

[AddComponentMenu("Component/Jitter Physics/Sphere Collider")]
public class JSphereCollider : JCollider
{
	[SerializeField] private float radius;
	public float Radius
	{
		get { return radius; }
		set { radius = value; }
	}

	public void Reset()
	{
		var meshFilter = GetComponent<MeshFilter>();
		var mesh = meshFilter.sharedMesh;
		var size = mesh.bounds.size;
		var scale = transform.localScale;
		size = new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
		radius = size.x / 2;
		if (size.y / 2 > radius)
			radius = size.y / 2;
		if (size.z / 2 > radius)
			radius = size.z / 2;
		UpdateShape();
	}

	public override Shape CreateShape()
	{
		return new SphereShape(radius);
	}
}