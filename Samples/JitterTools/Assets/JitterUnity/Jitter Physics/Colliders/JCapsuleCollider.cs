using Jitter.Collision.Shapes;
using UnityEngine;

[AddComponentMenu("Component/Jitter Physics/Capsule Collider")]
public class JCapsuleCollider : JCollider
{
	[SerializeField] private AxisAlignment axis;
	public AxisAlignment Axis
	{
		get { return axis; }
		set { axis = value; }
	}

	[SerializeField] private float radius;
	public float Radius
	{
		get { return radius; }
		set { radius = value; }
	}

	[SerializeField] private float length;
	public float Length
	{
		get { return length; }
		set { length = value; }
	}

	public void Reset()
	{
		var meshFilter = GetComponent<MeshFilter>();
		var mesh = meshFilter.sharedMesh;
		var size = mesh.bounds.size;
		var scale = transform.localScale;
		size = new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);

		radius = size.x > size.z ? size.x / 2 : size.z / 2;
		length = size.y - 2 * radius;
		UpdateShape();
	}

	public override Shape CreateShape()
	{
		return new CapsuleShape(length, radius);
	}
}