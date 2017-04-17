using UnityEditor;

[CustomEditor(typeof(JMaterial))]
public class JMaterialEditor : Editor
{
	[MenuItem("Jitter physics/Create JMaterial")]
	public static void CreateJMaterial()
	{
		var material = new JMaterial();
		AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath("Assets/JMaterial.asset"));
	}

	public override void OnInspectorGUI()
	{
		var material = (JMaterial)target;

		material.KineticFriction = EditorGUILayout.FloatField("Kinetic friction", material.KineticFriction);
		if (material.KineticFriction < 0)
		{
			material.KineticFriction = 0;
		}

		material.StaticFriction = EditorGUILayout.FloatField("Static friction", material.StaticFriction);
		if (material.StaticFriction < 0)
		{
			material.StaticFriction = 0;
		}

		material.Restitution = EditorGUILayout.FloatField("Restitution", material.Restitution);
		if (material.Restitution < 0)
		{
			material.Restitution = 0;
		}
	}
}