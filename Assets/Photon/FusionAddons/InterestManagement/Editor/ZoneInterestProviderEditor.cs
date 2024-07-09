namespace Fusion.Addons.InterestManagement.Editor
{
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.IMGUI.Controls;

	[CustomEditor(typeof(ZoneInterestProvider), true)]
	public class ZoneInterestProviderEditor : InterestProviderEditor
	{
		// PRIVATE MEMBERS

		private BoxBoundsHandle _boundsHandle = new BoxBoundsHandle();

		// InterestProviderEditor INTERFACE

		protected override void DrawInspectorGUI()
		{
			serializedObject.Update();

			DrawPropertiesExcluding(serializedObject, nameof(ZoneInterestProvider.Bounds));

			ZoneInterestProvider zone = target as ZoneInterestProvider;

			EditorGUI.BeginChangeCheck();

			InterestEditorUtility.DrawLine(Color.grey);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			Vector3 size   = EditorGUILayout.Vector3Field("Bounds Size",   zone.Bounds.size);
			Vector3 center = EditorGUILayout.Vector3Field("Bounds Center", zone.Bounds.center);
			EditorGUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(zone, "Change Bounds");

				Bounds bounds = new Bounds();
				bounds.size   = size;
				bounds.center = center;

				zone.Bounds = bounds;
			}

			serializedObject.ApplyModifiedProperties();
		}

		// PRIVATE METHODS

		private void OnSceneGUI()
		{
			ZoneInterestProvider zone = target as ZoneInterestProvider;

			_boundsHandle.size   = zone.Bounds.size;
			_boundsHandle.center = zone.Transform.position + zone.Bounds.center;

			EditorGUI.BeginChangeCheck();

			Color handlesColor = Handles.color;
			Handles.color = ZoneInterestProvider.HandleColor;
			_boundsHandle.DrawHandle();
			Handles.color = handlesColor;

			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(zone, "Change Bounds");

				Bounds bounds = new Bounds();
				bounds.size   = _boundsHandle.size;
				bounds.center = _boundsHandle.center - zone.Transform.position;

				zone.Bounds = bounds;
			}
		}
	}
}
