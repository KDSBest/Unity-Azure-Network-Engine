using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using UnityEngine;
using Material = Jitter.Dynamics.Material;

public class JFpsController : MonoBehaviour
{
	public float stepOffset = .4f;
	public float height = 2f;
	public float radius = .4f;
	public float jumpVelocity = 2f;
	public float walkVelocity = 4;
	public float runVelocity = 6;
	public float sprintVelocity = 10;
	public Vector3 contactPoint;
	public Vector3 localContactPoint;

	public GameObject marker;

	private CapsuleShape capsule;
	private RigidBody body;
	private JCharacterController controller;

	public RigidBody Body
	{
		get { return body; }
	}

	public void TransformToBody()
	{
		body.Position = transform.position.ToJVector();
		body.Orientation = transform.rotation.ToJMatrix();
	}

	private void BodyToTransform()
	{
		transform.position = body.Position.ToVector3();
	}

	private void Awake()
	{
		capsule = new CapsuleShape(height - 2 * radius, radius);

		body = new RigidBody(capsule);
		body.AllowDeactivation = false;
		body.SetMassProperties(JMatrix.Zero, 1.0f, true);
		body.Material = new Material
							{
								KineticFriction = 1,
								StaticFriction = 1,
								Restitution = 0,
							};
		body.Damping = RigidBody.DampingType.None;
		body.Tag = this;

		controller = new JCharacterController(body);
	}

	private void OnEnable()
	{
		TransformToBody();

		JPhysics.AddBody(body);
		JPhysics.AddConstraint(controller);
	}

	private void OnDisable()
	{
		JPhysics.RemoveBody(body);
		JPhysics.RemoveConstraint(controller);
	}

	private void FixedUpdate()
	{
		float vertical = Input.GetAxis("Vertical");
		float horizontal = Input.GetAxis("Horizontal");

		float velocity = runVelocity;
		if (Input.GetKey(KeyCode.LeftShift))
			velocity = sprintVelocity;
		if (Input.GetKey(KeyCode.LeftControl))
			velocity = walkVelocity;
		var move = new Vector3(horizontal, 0, vertical).normalized * velocity;

		controller.TargetVelocity = transform.TransformDirection(move).ToJVector();
		if (controller.BodyWalkingOn != null)
			controller.TargetVelocity += controller.BodyWalkingOn.LinearVelocity;

		controller.TryJump = Input.GetAxis("Jump") > 0;
		controller.JumpVelocity = jumpVelocity;

		contactPoint = controller.contactPoint.ToVector3();
		localContactPoint = controller.localContactPoint.ToVector3();
	}

	private void LateUpdate()
	{
		BodyToTransform();
	}
}