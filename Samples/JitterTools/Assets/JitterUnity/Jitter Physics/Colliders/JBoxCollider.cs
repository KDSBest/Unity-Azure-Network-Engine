using Jitter.Collision.Shapes;
using UnityEngine;

[AddComponentMenu("Component/Jitter Physics/Box Collider")]
public class JBoxCollider : JCollider
{
	[SerializeField] private Vector3 size = Vector3.one;
	public Vector3 Size
	{
		get { return size; }
		set { size = value; }
	}

	public void Reset()
	{
		var meshFilter = GetComponent<MeshFilter>();
		var mesh = meshFilter.sharedMesh;
		size = mesh.bounds.size;
		var scale = transform.localScale;

		size = new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
	}

	public override Shape CreateShape()
	{
		return new BoxShape(size.ToJVector());
	}
}