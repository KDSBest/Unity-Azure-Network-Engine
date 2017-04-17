using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JHingeJoint))]
[CanEditMultipleObjects]
public class JHingeJointEditor : Editor
{
	private SerializedProperty body1;
	private SerializedProperty body2;
	private GUIContent axisLabel;

	public JRigidBody Body1
	{
		get { return (JRigidBody)(body1.objectReferenceValue); }
	}

	public JRigidBody Body2
	{
		get { return (JRigidBody)(body2.objectReferenceValue); }
	}

	private void OnEnable()
	{
		body1 = serializedObject.FindProperty("body1");
		body2 = serializedObject.FindProperty("body2");
	}

	private void OnSceneGUI()
	{
		const float RADIUS = .2f;

		var transform = ((JHingeJoint)target).transform;
		var dir = transform.forward;

		var p1 = Body1 != null ? Body1.transform.position : transform.position;
		var p2 = Body2 != null ? p1 + dir * (Body2.transform.position - p1).magnitude : p1 + dir;
		var n1 = GeometryUtilities.GetArbitraryPerpendicular(dir) * RADIUS;
		var n2 = Vector3.Cross(dir, n1);

		var color = Handles.color;
		Handles.color = JPhysics.Color;
		{
			Handles.DrawLine(p1, p2);

			Handles.DrawWireDisc(p1, dir, RADIUS);
			Handles.DrawWireDisc(p2, dir, RADIUS);

			Handles.DrawLine(p1 - n1, p1 + n1);
			Handles.DrawLine(p1 - n2, p1 + n2);
			Handles.DrawLine(p2 - n1, p2 + n1);
			Handles.DrawLine(p2 - n2, p2 + n2);
		}
		Handles.color = color;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(body1);
		EditorGUILayout.PropertyField(body2);

		bool modified = serializedObject.ApplyModifiedProperties();
		if (modified)
		{
			foreach (JHingeJoint joint in targets)
			{
				joint.Refresh();
			}
		}

		if (Body1 != null && Body2 != null)
		{
		}
		if (GUILayout.Button("Put bodies on axis"))
		{

		}
		if (GUILayout.Button("Align axis to bodies"))
		{
			var transform = ((JHingeJoint)target).transform;
			transform.position = Body1.transform.position;
			transform.forward = (Body2.transform.position - Body1.transform.position).normalized;
		}
	}
}