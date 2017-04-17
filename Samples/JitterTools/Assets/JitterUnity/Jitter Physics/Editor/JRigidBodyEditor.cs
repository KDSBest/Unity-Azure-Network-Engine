using UnityEditor;

[CustomEditor(typeof(JRigidBody))]
[CanEditMultipleObjects]
public class JRigidBodyEditor : Editor
{
	private SerializedProperty test;
	private SerializedProperty jmaterial;
	private SerializedProperty mass;
	private SerializedProperty isStatic;
	private SerializedProperty affectedByGravity;
	private SerializedProperty allowDeactivation;
	private SerializedProperty speculativeContacts;
	private SerializedProperty linearDamping;
	private SerializedProperty angularDamping;
	private SerializedProperty enableDebugDraw;

	void OnEnable()
	{
		jmaterial = serializedObject.FindProperty("jMaterial");
		mass = serializedObject.FindProperty("mass");
		isStatic = serializedObject.FindProperty("isStatic");
		affectedByGravity = serializedObject.FindProperty("affectedByGravity");
		allowDeactivation = serializedObject.FindProperty("allowDeactivation");
		speculativeContacts = serializedObject.FindProperty("speculativeContacts");
		linearDamping = serializedObject.FindProperty("linearDamping");
		angularDamping = serializedObject.FindProperty("angularDamping");
		enableDebugDraw = serializedObject.FindProperty("enableDebugDraw");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(jmaterial);
		EditorGUILayout.PropertyField(mass);
		EditorGUILayout.PropertyField(isStatic);
		EditorGUILayout.PropertyField(affectedByGravity);
		EditorGUILayout.PropertyField(allowDeactivation);
		EditorGUILayout.PropertyField(speculativeContacts);
		EditorGUILayout.Separator();
		EditorGUILayout.PropertyField(linearDamping);
		EditorGUILayout.PropertyField(angularDamping);
		EditorGUILayout.Separator();
		EditorGUILayout.PropertyField(enableDebugDraw);

		if (mass.floatValue <= 0)
		{
			mass.floatValue = .001f;
		}

		var modified = serializedObject.ApplyModifiedProperties();
		if (modified)
		{
			foreach (JRigidBody joint in targets)
			{
				joint.Refresh();
			}
			SceneView.RepaintAll();
		}
	}
}