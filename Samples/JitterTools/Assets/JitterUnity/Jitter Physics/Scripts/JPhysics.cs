using System.Collections.Generic;
using System.Threading;
using Jitter;
using Jitter.Collision;
using Jitter.Dynamics;
using Jitter.Dynamics.Constraints;
using Jitter.LinearMath;
using UnityEngine;
using Material = Jitter.Dynamics.Material;

public class JPhysics : MonoBehaviour
{
	public static readonly Color Color = new Color(255, 210, 0);

	[SerializeField]
	private JMaterial defaultMaterial = null;

	internal static Material defaultPhysicsMaterial = new Material
												{
													KineticFriction = .3f,
													StaticFriction = .6f,
													Restitution = 0,
												};

	[SerializeField]
	private bool runInBackground = false;

	private static CollisionSystem collisionSystem;
	public static CollisionSystem CollisionSystem
	{
		get
		{
			if (collisionSystem == null)
			{
				collisionSystem = new CollisionSystemSAP();
			}
			return collisionSystem;
		}
	}

	private static JitterWorld world;
	public static JitterWorld World
	{
		get
		{
			if (world == null)
			{
				world = new JitterWorld(CollisionSystem);
				world.SetDampingFactors(.5f, .5f);
				world.Gravity = Physics.gravity.ToJVector();
			}
			return world;
		}
	}

	[SerializeField]
	private float sleepAngularVelocity = .05f;

	public float SleepAngularVelocity
	{
		get { return sleepAngularVelocity; }
		set
		{
			sleepAngularVelocity = value;
			UpdateWorld();
		}
	}

	[SerializeField]
	private float sleepVelocity = 0.05f;

	public float SleepVelocity
	{
		get { return sleepVelocity; }
		set
		{
			sleepVelocity = value;
			UpdateWorld();
		}
	}

	[SerializeField]
	private float angularDamping = .5f;
	public float AngularDamping
	{
		get { return angularDamping; }
		set
		{
			angularDamping = value;
			UpdateWorld();
		}
	}

	[SerializeField]
	private float linearDamping = .5f;
	public float LinearDamping
	{
		get { return linearDamping; }
		set
		{
			linearDamping = value;
			UpdateWorld();
		}
	}

	public void UpdateWorld()
	{
		world.SetDampingFactors(angularDamping, linearDamping);
		world.SetInactivityThreshold(sleepAngularVelocity, sleepVelocity, .5f);
		if (defaultMaterial != null)
		{
			defaultPhysicsMaterial = defaultMaterial.ToMaterial();
		}
	}

	private void Awake()
	{
		World.Clear();
		UpdateWorld();

		if (runInBackground)
		{
			InitializeThreads();
		}
	}

	private void FixedUpdate()
	{
		if (runInBackground == false)
		{
			World.Step(Time.fixedDeltaTime, false);
		}
	}

	private void OnDrawGizmos()
	{
		JBBox box;

		foreach (var island in World.Islands)
		{
			box = JBBox.SmallBox;

			foreach (RigidBody body in island.Bodies)
			{
				box = JBBox.CreateMerged(box, body.BoundingBox);
			}

			var color = Gizmos.color;
			Gizmos.color = island.IsActive() ? Color.green : Color.yellow;
			JRuntimeDrawer.Instance.DrawAabb(box.Min, box.Max);
			Gizmos.color = color;
		}
	}

	public static IEnumerable<JCollision> DetectCollisions(RigidBody body)
	{
		var position1 = body.Position;
		var orientation1 = body.Orientation;

		foreach (RigidBody body2 in World.RigidBodies)
		{
			if (body == body2)
			{
				continue;
			}
			var position2 = body2.Position;
			var orientation2 = body2.Orientation;

			JVector point;
			JVector normal;
			float penetration;
			var collisionDetected = XenoCollide.Detect(body.Shape, body2.Shape, ref orientation1, ref orientation2, ref position1, ref position2, out point, out normal, out penetration);
			if (collisionDetected)
			{
				yield return new JCollision(body, body2, point.ToVector3(), normal.ToVector3(), penetration);
			}
		}
	}

	public static JCollision DetectCollision(RigidBody body1, RigidBody body2)
	{
		System.Diagnostics.Debug.Assert(body1 != body2, "body1 == body2");

		var position1 = body1.Position;
		var position2 = body2.Position;
		var orientation1 = body1.Orientation;
		var orientation2 = body2.Orientation;

		JVector point;
		JVector normal;
		float penetration;
		var collisionDetected = XenoCollide.Detect(body1.Shape, body2.Shape, ref orientation1, ref orientation2, ref position1, ref position2, out point, out normal, out penetration);
		if (collisionDetected == false)
		{
			return null;
		}

		return new JCollision(body1, body2, point.ToVector3(), normal.ToVector3(), penetration);
	}

	public static JRaycastHit Raycast(Ray ray, float maxDistance = 10f, RaycastCallback callback = null)
	{
		RigidBody hitBody;
		JVector hitNormal;
		float hitFraction;

		var origin = ray.origin.ToJVector();
		var direction = ray.direction.ToJVector();

		if (collisionSystem.Raycast(origin, direction, callback, out hitBody, out hitNormal, out hitFraction))
		{
			if (hitFraction <= maxDistance)
			{
				return new JRaycastHit(hitBody, hitNormal.ToVector3(), ray.origin, ray.direction, hitFraction);
			}
		}
		else
		{
			direction *= maxDistance;
			if (collisionSystem.Raycast(origin, direction, callback, out hitBody, out hitNormal, out hitFraction))
			{
				return new JRaycastHit(hitBody, hitNormal.ToVector3(), ray.origin, direction.ToVector3(), hitFraction);
			}
		}
		return null;
	}

	public static void AddBody(RigidBody body)
	{
		lock (sync)
			World.AddBody(body);
	}

	public static void RemoveBody(RigidBody body)
	{
		lock (sync)
			World.RemoveBody(body);
	}

	public static void AddConstraint(Constraint constraint)
	{
		lock (sync)
		{
			World.AddConstraint(constraint);
		}
	}

	public static void RemoveConstraint(Constraint constraint)
	{
		lock (sync)
		{
			World.RemoveConstraint(constraint);
		}
	}

	public static int RigidBodyCount
	{
		get { return World.RigidBodies.Count; }
	}

	private Thread controlThread;
	private Thread stepThread;
	private volatile bool cancel;
	private float deltaTime;
	private AutoResetEvent reset;
	private static readonly object sync = new object();

	private void OnDestroy()
	{
		cancel = true;
		if (reset != null)
		{
			reset.Set();
		}
	}

	private void InitializeThreads()
	{
		controlThread = new Thread(ControlThreadProc);
		controlThread.IsBackground = true;
		deltaTime = Time.fixedDeltaTime;

		stepThread = new Thread(StepThreadProc);
		stepThread.IsBackground = true;

		reset = new AutoResetEvent(false);
		cancel = false;
		controlThread.Start();
		stepThread.Start();
	}

	private void ControlThreadProc()
	{
		while (cancel == false)
		{
			Thread.Sleep((int)(deltaTime * 1000));
			reset.Set();
		}
		Debug.Log("control thread stopped");
	}

	private void StepThreadProc()
	{
		while (cancel == false)
		{
			reset.WaitOne();
			lock (sync)
			{
				World.Step(deltaTime, false);
			}
		}
		Debug.Log("step thread stopped");
	}
}