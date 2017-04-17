using UnityEditor;

[CustomEditor(typeof(JCapsuleCollider))]
[CanEditMultipleObjects]
public class JCapsuleColliderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var collider = (JCapsuleCollider)target;
		 
		var radius = collider.Radius;
		radius = EditorGUILayout.FloatField("Radius", radius);
		if (collider.Radius != radius)
		{
			collider.Radius = radius;
			SceneView.RepaintAll();
		}

		var length = collider.Length;
		length = EditorGUILayout.FloatField("Height", length);
		if (collider.Length != length)
		{
			collider.Length = length;
			SceneView.RepaintAll();
		}
	}
}