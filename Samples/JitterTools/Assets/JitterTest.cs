using System;
using System.Collections;
using System.Collections.Generic;

using Jitter;
using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.LinearMath;

using UnityEngine;
using JitterBody = Jitter.Dynamics.RigidBody;
/// <summary>
/// Hacky Jitter Test
/// </summary>
public class JitterTest : MonoBehaviour
{
	public JitterWorld World { get; set; }

	public CollisionSystem CollisionSystem { get; set; }

	public GameObject StaticObject;

	public GameObject[] DynamicObject;

	public JitterBody JitterStaticObject { get; set; }
	public List<JitterBody> JitterDynamicObject { get; set; }

	public GameObject CapsuleTest;

	public JitterBody JitterCapsuleTest { get; set; }

	public void Start()
	{
		CollisionSystem = new CollisionSystemSAP();
		World = new JitterWorld(CollisionSystem);
		JitterStaticObject = GetJitter(StaticObject.GetComponent<BoxCollider>());
		JitterStaticObject.IsStatic = true;
		World.AddBody(JitterStaticObject);

		JitterDynamicObject = new List<JitterBody>(this.DynamicObject.Length);

		for (int i = 0; i < this.DynamicObject.Length; i++)
		{
			var newJitterDynamicObject = GetJitter(DynamicObject[i].GetComponent<BoxCollider>());
			newJitterDynamicObject.IsStatic = false;
			World.AddBody(newJitterDynamicObject);
			JitterDynamicObject.Add(newJitterDynamicObject);
		}

		
	}

	private JitterBody GetJitter(BoxCollider collider)
	{
		var size = new JVector(collider.transform.localScale.x * collider.size.x, collider.transform.localScale.y * collider.size.y, collider.transform.localScale.z * collider.size.z);
		BoxShape result = new BoxShape(size);

		var body = SetPosition(collider.center, collider.transform, result);
		SetRotation(collider.transform.rotation, body);

		return body;
	}

	private JitterBody GetJitter(CapsuleCollider collider)
	{		
		//var size = new JVector(collider.transform.localScale.x * collider.size.x, collider.transform.localScale.y * collider.size.y, collider.transform.localScale.z * collider.size.z);
		CapsuleShape result = new CapsuleShape(collider.height, collider.radius);	

		var body = SetPosition(collider.center, collider.transform, result);
		SetRotation(collider.transform.rotation, body);

		return body;
	}

	private static JitterBody SetPosition(Vector3 c, Transform transform, Shape result)
	{
		var center = c + transform.position;
		JitterBody body = new JitterBody(result);
		body.Position = new JVector(center.x, center.y, center.z);
		return body;
	}

	private static void SetRotation(Quaternion rotation, JitterBody body)
	{
		body.Orientation = JMatrix.CreateFromQuaternion(new JQuaternion(rotation.x, rotation.y, rotation.z, rotation.w));
	}

	public void FixedUpdate()
	{
		World.Step(Time.fixedDeltaTime, true);
		for (int i = 0; i < this.DynamicObject.Length; i++)
		{
			DynamicObject[i].transform.position = new Vector3(JitterDynamicObject[i].Position.X, JitterDynamicObject[i].Position.Y, JitterDynamicObject[i].Position.Z);
			this.DynamicObject[i].transform.rotation = Convert(JitterDynamicObject[i].Orientation);
		}
	}

	public Quaternion Convert(JMatrix matrix)
	{
		float qx, qy, qz, qw;
		float m00 = matrix.M11;
		float m01 = matrix.M12;
		float m02 = matrix.M13;
		float m10 = matrix.M21;
		float m11 = matrix.M22;
		float m12 = matrix.M23;
		float m20 = matrix.M31;
		float m21 = matrix.M32;
		float m22 = matrix.M33;

		float tr = m00 + m11 + m22;

		if (tr > 0)
		{
			float S = (float)Math.Sqrt(tr + 1.0f) * 2.0f; // S=4*qw 
			qw = 0.25f * S;
			qx = (m21 - m12) / S;
			qy = (m02 - m20) / S;
			qz = (m10 - m01) / S;
		}
		else if ((m00 > m11) & (m00 > m22))
		{
			float S = (float)Math.Sqrt(1.0 + m00 - m11 - m22) * 2; // S=4*qx 
			qw = (m21 - m12) / S;
			qx = 0.25f * S;
			qy = (m01 + m10) / S;
			qz = (m02 + m20) / S;
		}
		else if (m11 > m22)
		{
			float S = (float)Math.Sqrt(1.0 + m11 - m00 - m22) * 2; // S=4*qy
			qw = (m02 - m20) / S;
			qx = (m01 + m10) / S;
			qy = 0.25f * S;
			qz = (m12 + m21) / S;
		}
		else
		{
			float S = (float)Math.Sqrt(1.0 + m22 - m00 - m11) * 2; // S=4*qz
			qw = (m10 - m01) / S;
			qx = (m02 + m20) / S;
			qy = (m12 + m21) / S;
			qz = 0.25f * S;
		}
		return new Quaternion(qz, qy, qx, qw);
	}

	public void Update()
	{

	}
}
