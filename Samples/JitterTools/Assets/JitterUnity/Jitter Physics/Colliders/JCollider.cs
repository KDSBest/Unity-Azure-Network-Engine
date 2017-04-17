using System;
using Jitter.Collision.Shapes;
using UnityEngine;

[Serializable]
public abstract class JCollider : MonoBehaviour
{
	private Shape shape;
	public Shape Shape
	{
		get
		{
			if (shape == null)
				shape = CreateShape();
			return shape;
		}
		protected set { shape = value; }
	}

	public abstract Shape CreateShape();

	public virtual CompoundShape.TransformedShape CreateTransformedShape(JRigidBody body)
	{
		var position = transform.position - body.transform.position;
		var rotation = Quaternion.RotateTowards(body.transform.rotation, transform.rotation, 360);
		
		var invRotation = Quaternion.Inverse(body.transform.rotation);
		rotation = invRotation * rotation;
		position = invRotation * position;

		return new CompoundShape.TransformedShape(Shape, rotation.ToJMatrix(), position.ToJVector());
	}

	public void UpdateShape()
	{
		shape = null;

		if (ShapeChanged != null)
			ShapeChanged.Invoke();
	}

	public event Action ShapeChanged;
}