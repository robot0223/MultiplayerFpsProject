namespace Fusion.Addons.InterestManagement.Editor
{
	using UnityEngine;
	using UnityEditor;

	[CustomEditor(typeof(PlayerInterestView), true)]
	public class PlayerInterestViewEditor : InterestProviderEditor
	{
		// InterestProviderEditor INTERFACE

		protected override void DrawInspectorGUI()
		{
			DrawDefaultInspector();

			if (Application.isPlaying == false)
				return;

			PlayerInterestView playerView = target as PlayerInterestView;

			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Player Transform", playerView.PlayerTransform, typeof(Object), true);
				EditorGUILayout.ObjectField("Camera Transform", playerView.CameraTransform, typeof(Object), true);
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndVertical();
		}
	}
}
