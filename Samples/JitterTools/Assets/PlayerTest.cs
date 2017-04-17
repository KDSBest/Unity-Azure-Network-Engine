using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Jitter.Collision;
using Jitter.Dynamics;
using Jitter.LinearMath;

using Protocol;

using ReliableUdp.Enums;

using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerTest : MonoBehaviour
{
	public float speed = 10f;
	public float sensitivity = 2f;

	public GameObject Camera;

	float moveFB;
	float moveLR;

	float rotX;
	float rotY;

	private CollisionSystem colSys = new CollisionSystemSAP();
	private CollisionSystem colSysOnlyEnemy = new CollisionSystemSAP();

	private List<JRigidBody> enemies = new List<JRigidBody>();

	public Guid Id = Guid.NewGuid();

	// Use this for initialization
	public void Start()
	{
		foreach (var go in GameObject.FindGameObjectsWithTag("Static"))
		{
			var body = go.GetComponent<JRigidBody>();
			if (body != null)
				this.colSys.AddEntity(body.Body);
		}

		foreach (var go in GameObject.FindGameObjectsWithTag("Enemy"))
		{
			var body = go.GetComponent<JRigidBody>();
			if (body != null)
			{
				enemies.Add(body);
				this.colSys.AddEntity(body.Body);
				this.colSysOnlyEnemy.AddEntity(body.Body);
			}
		}
	}

	public List<Shot> Shots = new List<Shot>();

	public void FixedUpdate()
	{
		ManageStuff.Udp.SendToAll(new Protocol.ClientUpdate()
		{
			Id = new MGuid()
			{
				Id = this.Id.ToByteArray().ToList()
			},
			Position = new MVector3()
			{
				X = this.transform.position.x,
				Y = this.transform.position.y,
				Z = this.transform.position.z
			},
			Rotation = new MQuaternion()
			{
				X = this.transform.rotation.x,
				Y = this.transform.rotation.y,
				Z = this.transform.rotation.z,
				w = this.transform.rotation.w
			},
			Shots = new List<Shot>(this.Shots)
		}, ChannelType.UnreliableOrdered);
		this.Shots.Clear();
	}

	// Update is called once per frame
	public void Update()
	{
		JVector normal = new JVector();
		float fraction = 0;
		Jitter.Dynamics.RigidBody body = null;

		if (Input.GetMouseButtonUp(0))
		{
			if (colSysOnlyEnemy.Raycast(this.transform.position.ToJVector(), this.Camera.transform.forward.ToJVector(), null, out body, out normal, out fraction))
			{
				foreach (var b in this.enemies)
				{
					if (b.Body == body)
					{
						GameObject.Destroy(b.gameObject);
						break;
					}
				}
			}
		}

		bool onFloor = false;

		if (colSys.Raycast((this.transform.position + Vector3.down * 1.25f).ToJVector(), JVector.Down, null, out body, out normal, out fraction))
		{
			if (fraction < 0.1f)
			{
				onFloor = true;
			}
		}

		if (!onFloor)
		{
			this.transform.position += new Vector3(0, -10, 0) * Time.deltaTime;
		}

		moveFB = Input.GetAxis("Vertical") * speed;
		moveLR = Input.GetAxis("Horizontal") * speed;

		rotX = Input.GetAxis("Mouse X") * sensitivity;
		rotY -= Input.GetAxis("Mouse Y") * sensitivity;

		rotY = Mathf.Clamp(rotY, -60f, 60f);

		Vector3 movement = new Vector3(moveLR, 0, moveFB);
		transform.Rotate(0, rotX, 0);
		this.Camera.transform.localRotation = Quaternion.Euler(rotY, 0, 0);

		movement = transform.rotation * movement;

		bool hitWall = false;
		if (colSys.Raycast(this.transform.position.ToJVector(), movement.ToJVector(), null, out body, out normal, out fraction))
		{
			if (fraction < 0.1f)
			{
				hitWall = true;
			}
		}

		if (!hitWall && colSys.Raycast((this.transform.position + Vector3.Cross(movement, Vector3.up) * 0.25f).ToJVector(), movement.ToJVector(), null, out body, out normal, out fraction))
		{
			if (fraction < 0.1f)
			{
				hitWall = true;
			}
		}

		if (!hitWall && colSys.Raycast((this.transform.position + Vector3.Cross(movement, Vector3.up) * -0.25f).ToJVector(), movement.ToJVector(), null, out body, out normal, out fraction))
		{
			if (fraction < 0.6f)
			{
				hitWall = true;
			}
		}

		if (!hitWall)
			this.transform.position += movement * Time.deltaTime;
	}
}
