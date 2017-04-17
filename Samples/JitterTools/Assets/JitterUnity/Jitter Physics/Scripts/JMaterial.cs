using UnityEngine;
using Material = Jitter.Dynamics.Material;

public class JMaterial : ScriptableObject
{
	private float kineticFriction = 0.3f;
	public float KineticFriction
	{
		get { return kineticFriction; }
		set { kineticFriction = value; }
	}

	private float staticFriction = 0.6f;
	public float StaticFriction
	{
		get { return staticFriction; }
		set { staticFriction = value; }
	}

	private float restitution = 0.5f;
	public float Restitution
	{
		get { return restitution; }
		set { restitution = value; }
	}

	public Material ToMaterial()
	{
		var material = new Material
							{
								KineticFriction = KineticFriction,
								StaticFriction = StaticFriction,
								Restitution = Restitution,
							};
		return material;
	}
}