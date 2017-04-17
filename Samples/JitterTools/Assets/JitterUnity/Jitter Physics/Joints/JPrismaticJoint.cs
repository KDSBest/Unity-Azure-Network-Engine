using Jitter.Dynamics.Joints;
using UnityEngine;

// todo Check the reason of fixed angle constraint instability
// todo Arbitrary points on rigid bodies

public class JPrismaticJoint : MonoBehaviour
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

	[SerializeField] private bool useDistances;
	public bool UseDistances
	{
		get { return useDistances; }
		set
		{
			useDistances = value;
			if (enabled)
				Joint.Deactivate();
			joint = CreateJoint();
			if (enabled)
				joint.Activate();
		}
	}

	[SerializeField] private float minDistance = 5;
	public float MinDistance
	{
		get { return minDistance; }
		set
		{
			minDistance = value;
			Refresh();
		}
	}

	[SerializeField] private float minDistanceSoftness = .5f;
	public float MinDistanceSoftness
	{
		get { return minDistanceSoftness; }
		set
		{
			minDistanceSoftness = value;
			Refresh();
		}
	}

	[SerializeField] private float maxDistance = 10;
	public float MaxDistance
	{
		get { return maxDistance; }
		set
		{
			maxDistance = value;
			Refresh();
		}
	}

	[SerializeField] private float maxDistanceSoftness = .5f;
	public float MaxDistanceSoftness
	{
		get { return maxDistanceSoftness; }
		set
		{
			if (value <= .01f)
				value = .01f;
			maxDistanceSoftness = value;
			Refresh();
		}
	}

	private PrismaticJoint joint;
	private PrismaticJoint Joint
	{
		get
		{
			if (joint == null)
				joint = CreateJoint();
			return joint;
		}
	}

	private PrismaticJoint CreateJoint()
	{
		if (useDistances)
		{
			var j = new PrismaticJoint(JPhysics.World, Body1.Body, Body2.Body, minDistance, maxDistance);
			j.MinimumDistanceConstraint.Softness = minDistanceSoftness;
			j.MaximumDistanceConstraint.Softness = maxDistanceSoftness;
			return j;
		}
		return new PrismaticJoint(JPhysics.World, Body1.Body, Body2.Body);
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

		minDistanceSoftness = Mathf.Clamp(minDistanceSoftness, 0, 1f);
		maxDistanceSoftness = Mathf.Clamp(maxDistanceSoftness, 0, 1f);
		if (minDistance < 0)
			minDistance = 0;
		if (maxDistance < 0)
			maxDistance = 0;

		Joint.MinimumDistanceConstraint.Distance = minDistance;
		Joint.MinimumDistanceConstraint.Softness = minDistanceSoftness;
		Joint.MaximumDistanceConstraint.Distance = maxDistance;
		Joint.MaximumDistanceConstraint.Softness = maxDistanceSoftness;

		if (enabled)
			Joint.Activate();
	}

	private void OnDrawGizmos()
	{
		var color = Gizmos.color;

		if (UseDistances)
		{
			var center = (Body1.transform.position + Body2.transform.position) * .5f;
			var dir = (Body1.transform.position - Body2.transform.position).normalized;
			var minExtent = dir * minDistance / 2;
			var maxExtent = dir * maxDistance / 2;

			Gizmos.color = Color.red;
			Gizmos.DrawLine(center - maxExtent, center + maxExtent);
			Gizmos.color = Color.green;
			Gizmos.DrawLine(center - minExtent, center + minExtent);
		}
		else
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(Body1.transform.position, Body2.transform.position);
		}

		Gizmos.color = color;
	}
}