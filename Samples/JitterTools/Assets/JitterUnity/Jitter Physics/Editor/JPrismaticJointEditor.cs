using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JPrismaticJoint))]
[CanEditMultipleObjects]
public class JPrismaticJointEditor : Editor
{
	private SerializedProperty body1;
	private SerializedProperty body2;
	private SerializedProperty useDistances;
	private SerializedProperty minDistance;
	private SerializedProperty minDistanceSoftness;
	private SerializedProperty maxDistance;
	private SerializedProperty maxDistanceSoftness;

	void OnEnable()
	{
		body1 = serializedObject.FindProperty("body1");
		body2 = serializedObject.FindProperty("body2");
		useDistances = serializedObject.FindProperty("useDistances");
		minDistance = serializedObject.FindProperty("minDistance");
		minDistanceSoftness = serializedObject.FindProperty("minDistanceSoftness");
		maxDistance = serializedObject.FindProperty("maxDistance");
		maxDistanceSoftness = serializedObject.FindProperty("maxDistanceSoftness");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(body1);
		EditorGUILayout.PropertyField(body2);
		
		EditorGUILayout.Separator();
		EditorGUILayout.PropertyField(useDistances);

		if (useDistances.boolValue)
		{
			EditorGUILayout.PropertyField(minDistance);
			if (minDistance.floatValue > maxDistance.floatValue)
			{
				maxDistance.floatValue = minDistance.floatValue;
			}
			EditorGUILayout.PropertyField(minDistanceSoftness, new GUIContent("Softness"));
			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(maxDistance);
			if (maxDistance.floatValue < minDistance.floatValue)
			{
				minDistance.floatValue = maxDistance.floatValue;
			}
			EditorGUILayout.PropertyField(maxDistanceSoftness, new GUIContent("Softness"));
		}

		var modified = serializedObject.ApplyModifiedProperties();
		if (modified)
		{
			foreach (JPrismaticJoint joint in targets)
			{
				joint.Refresh();
			}
		}
	}
}