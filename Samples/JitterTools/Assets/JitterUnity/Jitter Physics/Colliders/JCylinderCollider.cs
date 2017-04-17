using System;
using Jitter.Collision.Shapes;
using Jitter.LinearMath;
using UnityEngine;

[AddComponentMenu("Component/Component/Jitter Physics/Cylinder Collider")]
public class JCylinderCollider : JCollider
{
	[SerializeField]
	private AxisAlignment axis = AxisAlignment.PositiveY;

	public AxisAlignment Axis
	{
		get { return axis; }
		set
		{
			axis = value;
			UpdateShape();
		}
	}

	[SerializeField]
	private float radius;

	public float Radius
	{
		get { return radius; }
		set
		{
			radius = value;
			UpdateShape();
		}
	}

	[SerializeField]
	private float height;

	public float Height
	{
		get { return height; }
		set
		{
			height = value;
			UpdateShape();
		}
	}

	[SerializeField]
	private Vector3 offset = Vector3.zero;

	public Vector3 Offset
	{
		get { return offset; }
		set
		{
			offset = value;
			UpdateShape();
		}
	}

	public void Reset()
	{
		CalculateSize();
		offset = GetOffset();

		UpdateShape();
	}

	private void CalculateSize()
	{
		var meshFilter = GetComponent<MeshFilter>();
		var mesh = meshFilter.sharedMesh;
		var size = mesh.bounds.size;
		var scale = transform.localScale;
		size = new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);

		switch (Axis)
		{
			case AxisAlignment.PositiveX:
			case AxisAlignment.NegativeX:
				radius = size.y > size.z ? size.y / 2 : size.z / 2;
				height = size.x;
				break;

			case AxisAlignment.PositiveY:
			case AxisAlignment.NegativeY:
				radius = size.x > size.z ? size.x / 2 : size.z / 2;
				height = size.y;
				break;

			case AxisAlignment.PositiveZ:
			case AxisAlignment.NegativeZ:
				radius = size.x > size.y ? size.x / 2 : size.y / 2;
				height = size.z;
				break;

			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public override Shape CreateShape()
	{
		return new CylinderShape(height, radius);
	}

	public override CompoundShape.TransformedShape CreateTransformedShape(JRigidBody body)
	{
		var shape = new CylinderShape(height, radius);
		var transformedShape = new CompoundShape.TransformedShape(shape, GetOrientation(), offset.ToJVector());
		return transformedShape;
	}

	private Vector3 GetOffset()
	{
		var meshFilter = GetComponent<MeshFilter>();
		var mesh = meshFilter.sharedMesh;
		return mesh.bounds.center;
	}

	private JMatrix GetOrientation()
	{
		switch (Axis)
		{
			case AxisAlignment.PositiveX:
			case AxisAlignment.NegativeX:
				return JMatrix.CreateRotationZ(JMath.PiOver2);

			case AxisAlignment.PositiveY:
			case AxisAlignment.NegativeY:
				return JMatrix.Identity;

			case AxisAlignment.PositiveZ:
			case AxisAlignment.NegativeZ:
				return JMatrix.CreateRotationX(JMath.PiOver2);

			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}