using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JConvexHullCollider))]
[CanEditMultipleObjects]
public class JConvexHullColliderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var collider = (JConvexHullCollider)target;

		var mesh = collider.Mesh;
		mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", mesh, typeof(Mesh), false);
		if (collider.Mesh != mesh)
		{
			collider.Mesh = mesh;
			SceneView.RepaintAll();
		}
	}
}