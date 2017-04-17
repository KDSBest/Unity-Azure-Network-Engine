//using UnityEditor;
//
//[CustomEditor(typeof(JFpsController))]
//public class JFpsControllerEditor : Editor
//{
//	private SerializedProperty test;
//	private SerializedProperty mass;
//	private SerializedProperty isStatic;
//	private SerializedProperty affectedByGravity;
//	private SerializedProperty allowDeactivation;
//	private SerializedProperty speculativeContacts;
//	private SerializedProperty enableDebugDraw;
//
//	private SerializedProperty stepOffset;
//	private SerializedProperty center;
//	private SerializedProperty height;
//	private SerializedProperty radius;
//	private SerializedProperty slopeLimit;
//
//	void OnEnable()
//	{
//		stepOffset = serializedObject.FindProperty("stepOffset");
//		center = serializedObject.FindProperty("center");
//		height = serializedObject.FindProperty("height");
//		radius = serializedObject.FindProperty("radius");
//		slopeLimit = serializedObject.FindProperty("slopeLimit");
//
//		mass = serializedObject.FindProperty("mass");
//		isStatic = serializedObject.FindProperty("isStatic");
//		affectedByGravity = serializedObject.FindProperty("affectedByGravity");
//		allowDeactivation = serializedObject.FindProperty("allowDeactivation");
//		speculativeContacts = serializedObject.FindProperty("speculativeContacts");
//		enableDebugDraw = serializedObject.FindProperty("enableDebugDraw");
//	}
//
//	public override void OnInspectorGUI()
//	{
//		serializedObject.Update();
//
//		EditorGUILayout.PropertyField(stepOffset);
//		EditorGUILayout.PropertyField(center);
//		EditorGUILayout.PropertyField(height);
//		EditorGUILayout.PropertyField(radius);
//		EditorGUILayout.PropertyField(slopeLimit);
//		EditorGUILayout.Separator();
//
//		if (radius.floatValue < .001f)
//		{
//			radius.floatValue = .001f;
//		}
//		if (height.floatValue < 2 * radius.floatValue)
//		{
//			height.floatValue = 2 * radius.floatValue;
//		}
//
//		EditorGUILayout.PropertyField(isStatic);
//		EditorGUILayout.PropertyField(affectedByGravity);
//		EditorGUILayout.PropertyField(allowDeactivation);
//		EditorGUILayout.PropertyField(speculativeContacts);
//		EditorGUILayout.Separator();
//
//		EditorGUILayout.PropertyField(enableDebugDraw);
//
//		if (mass.floatValue <= 0)
//		{
//			mass.floatValue = .001f;
//		}
//
//		var modified = serializedObject.ApplyModifiedProperties();
//		if (modified)
//		{
//			foreach (JFpsController controller in targets)
//			{
//				//
//			}
//			SceneView.RepaintAll();
//		}
//	}
//}