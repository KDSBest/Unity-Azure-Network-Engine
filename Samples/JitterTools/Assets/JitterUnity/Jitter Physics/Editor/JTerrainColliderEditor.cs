using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JTerrainCollider))]
[CanEditMultipleObjects]
public class JTerrainColliderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var collider = (JTerrainCollider)target;

		EditorGUILayout.LabelField("Heights resolution", collider.Resolution.ToString());
		EditorGUILayout.LabelField("Size", collider.Size.ToString());

		if (GUILayout.Button("Update collider"))
		{
			collider.UpdateShape();
			SceneView.RepaintAll();
		}
	}
}