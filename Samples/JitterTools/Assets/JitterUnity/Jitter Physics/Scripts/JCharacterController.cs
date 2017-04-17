using Jitter;
using Jitter.Dynamics;
using Jitter.Dynamics.Constraints;
using Jitter.LinearMath;

public class JCharacterController : Constraint
{
	public float JumpVelocity = 2f;
	private readonly float feetPosition;

	public JCharacterController(RigidBody body)
		: base(body, null)
	{
		// determine the position of the feets of the character
		// this can be done by supportmapping in the down direction.
		// (furthest point away in the down direction)
		var vec = JVector.Down;
		JVector result;

		// Note: the following works just for normal shapes, for multishapes (compound for example)
		// you have to loop through all sub-shapes -> easy.
		body.Shape.SupportMapping(ref vec, out result);

		// feet position is now the distance between body.Position and the feets
		// Note: the following '*' is the dot product.
		feetPosition = result * JVector.Down;
	}

	public JVector TargetVelocity { get; set; }
	public bool TryJump { get; set; }
	public RigidBody BodyWalkingOn { get; set; }
	public JVector contactPoint;
	public JVector localContactPoint;

	private JVector deltaVelocity = JVector.Zero;
	private bool shouldIJump;

	public override void PrepareForIteration(float timestep)
	{
		// send a ray from our feet position down.
		// if we collide with something which is 0.05f units below our feets remember this!

		RigidBody resultingBody = null;
		JVector normal;
		float frac;

		var rayOrigin = Body1.Position + JVector.Down * (feetPosition - 0.1f);
		bool result = JPhysics.World.CollisionSystem.Raycast(
			rayOrigin,
			JVector.Down,
			(body, hitNormal, fraction) => body != Body1,
			out resultingBody,
			out normal,
			out frac);

		if (BodyWalkingOn != null)
		{
			contactPoint = rayOrigin + JVector.Down * frac;
			localContactPoint = JVector.Transform(contactPoint - BodyWalkingOn.Position, JMatrix.Inverse(BodyWalkingOn.Orientation));
		}

		BodyWalkingOn = (result && frac <= 0.2f) ? resultingBody : null;
		shouldIJump = TryJump && result && (frac <= 0.2f) && (Body1.LinearVelocity.Y < JumpVelocity);
	}

	public override void Iterate()
	{
		deltaVelocity = TargetVelocity - Body1.LinearVelocity;
		deltaVelocity.Y = 0.0f;

		deltaVelocity *= 0.02f;
		if (BodyWalkingOn == null)
			deltaVelocity *= .1f;

		if (deltaVelocity.LengthSquared() != 0.0f)
		{
			// activate it, in case it fall asleep :)
			Body1.IsActive = true;
			Body1.ApplyImpulse(deltaVelocity * Body1.Mass);
		}

		if (shouldIJump)
		{
			Body1.IsActive = true;
			Body1.ApplyImpulse(JumpVelocity * JVector.Up * Body1.Mass);

			if (!BodyWalkingOn.IsStatic)
			{
				BodyWalkingOn.IsActive = true;
				// apply the negative impulse to the other body
				BodyWalkingOn.ApplyImpulse(JumpVelocity * JVector.Up * Body1.Mass, contactPoint);
			}
		}
	}
}