using UnityEditor;

[CustomEditor(typeof(JBoxCollider))]
[CanEditMultipleObjects]
public class JBoxColliderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var collider = (JBoxCollider)target;

		var size = collider.Size;
		size = EditorGUILayout.Vector3Field("Size", size);
		if (collider.Size != size)
		{
			collider.Size = size;
			SceneView.RepaintAll();
		}
	}
}