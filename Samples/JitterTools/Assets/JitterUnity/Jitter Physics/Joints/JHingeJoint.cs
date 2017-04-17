using UnityEngine;
using HingeJoint = Jitter.Dynamics.Joints.HingeJoint;

public class JHingeJoint : MonoBehaviour
{
	[SerializeField] private JRigidBody body1;
	public JRigidBody Body1
	{
		get { return body1; }
		set { body1 = value; }
	}

	[SerializeField] private JRigidBody body2;
	public JRigidBody Body2
	{
		get { return body2; }
		set { body2 = value; }
	}

	private HingeJoint joint;
	private HingeJoint Joint
	{
		get
		{
			if (joint == null)
				joint = CreateJoint();
			return joint;
		}
	}

	private HingeJoint CreateJoint()
	{
		return new HingeJoint(JPhysics.World, body1.Body, body2.Body, transform.position.ToJVector(), transform.forward.ToJVector());
	}

	private void OnEnable()
	{
		Joint.Activate();
	}

	private void OnDisable()
	{
		Joint.Deactivate();
	}

	public void Refresh()
	{
		if (enabled)
			Joint.Deactivate();

		joint = CreateJoint();

		if (enabled)
			Joint.Activate();
	}

	private void OnDrawGizmos()
	{
		var color = Gizmos.color;
		Gizmos.color = JPhysics.Color;
		Gizmos.DrawLine(Body1.transform.position, Body2.transform.position);
		Gizmos.color = color;
	}
}