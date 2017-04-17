using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JMeshCollider))]
[CanEditMultipleObjects]
public class JMeshColliderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var collider = (JMeshCollider)target;

		var mesh = collider.Mesh;
		mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", mesh, typeof(Mesh), false);
		if (collider.Mesh != mesh)
		{
			collider.Mesh = mesh;
			SceneView.RepaintAll();
		}
	}
}