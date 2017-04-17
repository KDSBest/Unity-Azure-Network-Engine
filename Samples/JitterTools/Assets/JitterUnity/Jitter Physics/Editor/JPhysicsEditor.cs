using UnityEditor;

[CustomEditor(typeof(JPhysics))]
public class JPhysicsEditor : Editor
{
	private SerializedProperty defaultMaterial;
	private SerializedProperty linearDamping;
	private SerializedProperty angularDamping;
	private SerializedProperty sleepAngularVelocity;
	private SerializedProperty sleepVelocity;
	private SerializedProperty runInBackground;

	private void OnEnable()
	{
		defaultMaterial = serializedObject.FindProperty("defaultMaterial");
		linearDamping = serializedObject.FindProperty("linearDamping");
		angularDamping = serializedObject.FindProperty("angularDamping");
		sleepAngularVelocity = serializedObject.FindProperty("sleepAngularVelocity");
		sleepVelocity = serializedObject.FindProperty("sleepVelocity");
		runInBackground = serializedObject.FindProperty("runInBackground");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(defaultMaterial);
		EditorGUILayout.PropertyField(linearDamping);
		EditorGUILayout.PropertyField(angularDamping);
		EditorGUILayout.PropertyField(sleepAngularVelocity);
		EditorGUILayout.PropertyField(sleepVelocity);
		EditorGUILayout.PropertyField(runInBackground);

		bool modified = serializedObject.ApplyModifiedProperties();
		if (modified)
		{
			((JPhysics)target).UpdateWorld();
			SceneView.RepaintAll();
		}
	}
}