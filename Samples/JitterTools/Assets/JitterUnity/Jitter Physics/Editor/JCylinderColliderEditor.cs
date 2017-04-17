using UnityEditor;

[CustomEditor(typeof(JCylinderCollider))]
[CanEditMultipleObjects]
public class JCylinderColliderEditor : Editor
{
	private readonly string[] axisArray = { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };

	public override void OnInspectorGUI()
	{
		var collider = (JCylinderCollider)target;

		var axis = collider.Axis;
		axis = (AxisAlignment)EditorGUILayout.Popup("Axis", (int)axis, axisArray);
		if (collider.Axis != axis)
		{
			collider.Axis = axis;
			SceneView.RepaintAll();
		}

		var offset = collider.Offset;
		offset = EditorGUILayout.Vector3Field("Offset", offset);
		if (collider.Offset != offset)
		{
			collider.Offset = offset;
			SceneView.RepaintAll();
		}

		float radius = collider.Radius;
		radius = EditorGUILayout.FloatField("Radius", radius);
		if (collider.Radius != radius)
		{
			collider.Radius = radius;
			SceneView.RepaintAll();
		}

		float height = collider.Height;
		height = EditorGUILayout.FloatField("Height", height);
		if (collider.Height != height)
		{
			collider.Height = height;
			SceneView.RepaintAll();
		}
	}
}