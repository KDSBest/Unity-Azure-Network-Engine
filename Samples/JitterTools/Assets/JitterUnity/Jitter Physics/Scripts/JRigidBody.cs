using System.Collections.Generic;
using System.Linq;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using UnityEngine;
using Material = Jitter.Dynamics.Material;

[AddComponentMenu("Jitter Physics/Rigid Body")]
[ExecuteInEditMode]
public class JRigidBody : MonoBehaviour
{
	private RigidBody body;
	internal RigidBody Body
	{
		get
		{
			if (body == null)
			{
				body = CreateBody();
			}
			return body;
		}
	}

	private Shape shape;
	private Shape Shape
	{
		get
		{
			if (shape == null)
			{
				shape = CreateShape();
			}
			return shape;
		}
	}

	[SerializeField] private bool affectedByGravity = true;
	public bool AffectedByGravity
	{
		get { return affectedByGravity; }
		set
		{
			affectedByGravity = value;
			Body.AffectedByGravity = value;
		}
	}

	[SerializeField] private bool isStatic;
	public bool IsStatic
	{
		get { return isStatic; }
		set
		{
			isStatic = value;
			Body.IsStatic = value;
		}
	}

	[SerializeField] private bool isKinematic;
	public bool IsKinematic
	{
		get { return isKinematic; }
		set { isKinematic = value; }
	}

	public bool IsActive
	{
		get { return Body.IsActive; }
		set { Body.IsActive = value; }
	}

	public bool IsStaticOrInactive
	{
		get { return Body.IsStaticOrInactive; }
	}

	[SerializeField] private bool allowDeactivation = true;
	public bool AllowDeactivation
	{
		get { return allowDeactivation; }
		set
		{
			allowDeactivation = value;
			Body.AllowDeactivation = allowDeactivation;
		}
	}

	[SerializeField] private bool enableDebugDraw;
	public bool EnableDebugDraw
	{
		get { return enableDebugDraw; }
		set
		{
			enableDebugDraw = value;
			Body.EnableDebugDraw = value;
		}
	}

	[SerializeField] private bool speculativeContacts;
	public bool SpeculativeContacts
	{
		get { return speculativeContacts; }
		set
		{
			speculativeContacts = value;
			Body.EnableSpeculativeContacts = value;
		}
	}

	[SerializeField] private bool linearDamping = true;
	public bool LinearDamping
	{
		get { return linearDamping; }
		set
		{
			linearDamping = value;
			if (linearDamping)
			{
				Body.Damping |= RigidBody.DampingType.Linear;
			}
			else
			{
				Body.Damping &= ~RigidBody.DampingType.Linear;
			}
		}
	}

	[SerializeField] private bool angularDamping = true;
	public bool AngularDamping
	{
		get { return angularDamping; }
		set
		{
			angularDamping = value;
			if (angularDamping)
			{
				Body.Damping |= RigidBody.DampingType.Angular;
			}
			else
			{
				Body.Damping &= ~RigidBody.DampingType.Angular;
			}
		}
	}

	[SerializeField] private JMaterial jMaterial;
	public JMaterial JMaterial
	{
		get { return jMaterial; }
		set
		{
			jMaterial = value;
			if (jMaterial != null)
			{
				Body.Material = jMaterial.ToMaterial();
			}
			else
			{
				Body.Material = null;
			}
		}
	}

	[SerializeField]
	private float mass;
	public float Mass
	{
		get { return mass; }
		set
		{
			mass = value;
			Body.Mass = mass;
		}
	}

	public Vector3 Torque
	{
		get { return Body.Torque.ToVector3(); }
	}

	public Vector3 Force
	{
		get { return Body.Force.ToVector3(); }
		set { Body.Force = value.ToJVector(); }
	}

	public Quaternion Inertia
	{
		get { return Body.Inertia.ToQuaternion(); }
	}

	public Quaternion InverseInertia
	{
		get { return Body.InverseInertia.ToQuaternion(); }
	}

	public Quaternion InverseInertiaWorld
	{
		get { return Body.InverseInertiaWorld.ToQuaternion(); }
	}

	public Vector3 LinearVelocity
	{
		get { return Body.LinearVelocity.ToVector3(); }
		set { Body.LinearVelocity = value.ToJVector(); }
	}

	public Vector3 AngularVelocity
	{
		get { return Body.AngularVelocity.ToVector3(); }
		set { Body.AngularVelocity = value.ToJVector(); }
	}

	public void AddForce(Vector3 force)
	{
		Body.AddForce(force.ToJVector());
	}

	public void AddForceAtPosition(Vector3 force, Vector3 position)
	{
		Body.AddForce(force.ToJVector(), position.ToJVector());
	}

	public void AddImpulse(Vector3 impulse)
	{
		Body.ApplyImpulse(impulse.ToJVector());
	}

	public void AddImpulseAtPosition(Vector3 impulse, Vector3 position)
	{
		Body.ApplyImpulse(impulse.ToJVector(), position.ToJVector());
	}

	public Vector3 GetPointVelocity(Vector3 point)
	{
		var velocity = Body.LinearVelocity + JVector.Cross(Body.AngularVelocity, point.ToJVector() - Body.Position);
		return velocity.ToVector3();
	}

	public void SetTransform(Transform t)
	{
		SetPosition(t.position);
		SetRotation(t.rotation);
	}

	public void SetTransform(Vector3 position, Quaternion rotation)
	{
		SetPosition(position);
		SetRotation(rotation);
	}

	public void SetPosition(Vector3 position)
	{
		Body.Position = position.ToJVector();
	}

	public void SetRotation(Quaternion rotation)
	{
		body.Orientation = rotation.ToJMatrix();
	}

	private RigidBody CreateBody()
	{
		Material material;
		if (JMaterial != null)
		{
			material = JMaterial.ToMaterial();
		}
		else if (JPhysics.defaultPhysicsMaterial != null)
		{
			material = JPhysics.defaultPhysicsMaterial;
		}
		else
		{
			material = new Material();
		}

		var result = new RigidBody(Shape, material);
		result.AffectedByGravity = AffectedByGravity;
		result.IsStatic = IsStatic;
		result.AllowDeactivation = AllowDeactivation;
		result.Damping = (LinearDamping ? RigidBody.DampingType.Linear : 0) | (AngularDamping ? RigidBody.DampingType.Angular : 0);
		result.EnableDebugDraw = EnableDebugDraw;

		if (isKinematic)
		{
			result.AffectedByGravity = false;
			result.Mass = 1e6f;
		}
		else if (Mass > 0)
		{
			result.Mass = Mass;
		}
		else
		{
			result.SetMassProperties();
		}

		return result;
	}

	public Bounds Bounds
	{
		get
		{
			var bb = Shape.BoundingBox;
			return new Bounds(bb.Center.ToVector3(), (bb.Max - bb.Min).ToVector3());
		}
	}

	public virtual void Refresh()
	{
		JMaterial = jMaterial;
		Mass = isKinematic ? 1e6f : mass;
		IsStatic = isStatic;
		AffectedByGravity = isKinematic ? false : affectedByGravity;
		AllowDeactivation = allowDeactivation;
		SpeculativeContacts = speculativeContacts;
		LinearDamping = linearDamping;
		AngularDamping = angularDamping;
		EnableDebugDraw = enableDebugDraw;
		shape = CreateShape();

		TransformToBody();
	}

	private Shape CreateShape()
	{
		var jterrain = GetComponent<JTerrainCollider>();
		if (jterrain != null)
		{
			return jterrain.Shape;
		}

		var colliders = GetComponentsInChildren<JCollider>();
		if (colliders.Length == 1 && colliders[0].gameObject == gameObject)
		{
			return colliders[0].Shape;
		}
		
		var shapes = colliders.Select(collider => collider.CreateTransformedShape(this)).ToList();
		return new CompoundShape(shapes);
	}

	private void OnEnable()
	{
		Body.Shape = Shape;
		Body.Tag = this;
		TransformToBody();

		JPhysics.AddBody(Body);
	}

	private void OnDisable()
	{
		JPhysics.RemoveBody(Body);
	}

	private void Reset()
	{
		Mass = Body.Shape.Mass * 100;
	}

	private void LateUpdate()
	{
		if (transform.hasChanged)
		{
			TransformToBody();
		}
		else
		{
			BodyToTransform();
		}

		transform.hasChanged = false;
	}

	public void TransformToBody()
	{
		var position = transform.position.ToJVector();
		var orientation = transform.rotation.ToJMatrix();
		var compound = Shape as CompoundShape;
		if (compound != null)
		{
			position -= JVector.Transform(compound.Shift, orientation);
		}

		Body.Position = position;
		Body.Orientation = orientation;
		//if (Body.IsActive == false && Body.IsStatic == false)
		//{
		//	var island = JPhysics.World.Islands.First(i => i.Bodies.Contains(Body));
		//	foreach (RigidBody b in island.Bodies)
		//		b.IsActive = true;
		//}
	}

	private void BodyToTransform()
	{
		var position = Body.Position.ToVector3();
		var quaternion = Body.Orientation.ToQuaternion();

		var compound = Shape as CompoundShape;
		if (compound != null)
		{
			position += quaternion * compound.Shift.ToVector3();
		}

		transform.position = position;
		transform.rotation = quaternion;
	}

	private void OnDrawGizmos()
	{
		var color = Gizmos.color;
		if (IsStatic)
		{
			Gizmos.color = Color.cyan;
		}
		else if (IsActive)
		{
			Gizmos.color = Color.green;
		}
		else
		{
			Gizmos.color = Color.yellow;
		}

		Body.DebugDraw(JRuntimeDrawer.Instance);

		Gizmos.color = color;
	}
}