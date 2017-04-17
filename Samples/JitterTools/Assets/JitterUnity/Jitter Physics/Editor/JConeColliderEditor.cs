using UnityEditor;

[CustomEditor(typeof(JConeCollider))]
[CanEditMultipleObjects]
public class JConeColliderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var collider = (JConeCollider)target;
		
		var radius = collider.Radius;
		radius = EditorGUILayout.FloatField("Radius", radius);
		if (collider.Radius != radius)
		{
			collider.Radius = radius;
			SceneView.RepaintAll();
		}

		var height = collider.Height;
		height = EditorGUILayout.FloatField("Height", height);
		if (collider.Height != height)
		{
			collider.Height = height;
			SceneView.RepaintAll();
		}
	}
}