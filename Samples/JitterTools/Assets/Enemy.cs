using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
	public float Speed = 4;

	public float RotationSpeed = 4;

	private Protocol.ClientUpdate cu;

	public void NewClientUpdate(Protocol.ClientUpdate cu)
	{
		this.cu = cu;
	}

	// Use this for initialization
	public void Start()
	{

	}

	// Update is called once per frame
	public void Update()
	{
		transform.position = Vector3.Lerp(transform.position, new Vector3(cu.Position.X, cu.Position.Y, cu.Position.Z), Time.deltaTime * Speed);
		transform.rotation = Quaternion.Lerp(transform.rotation, new Quaternion(cu.Rotation.X, cu.Rotation.Y, cu.Rotation.Z, cu.Rotation.w), Time.deltaTime * this.RotationSpeed);
	}
}
