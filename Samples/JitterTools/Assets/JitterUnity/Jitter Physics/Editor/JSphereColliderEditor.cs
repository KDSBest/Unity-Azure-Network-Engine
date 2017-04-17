using UnityEditor;

[CustomEditor(typeof(JSphereCollider))]
[CanEditMultipleObjects]
public class JSphereColliderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var collider = (JSphereCollider)target;
		
		var radius = collider.Radius;
		radius = EditorGUILayout.FloatField("Radius", radius);
		if (collider.Radius != radius)
		{
			collider.Radius = radius;
			SceneView.RepaintAll();
		}
	}
}