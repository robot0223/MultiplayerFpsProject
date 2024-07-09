namespace Fusion.Addons.InterestManagement.Editor
{
	using System.Collections.Generic;
	using UnityEditor;
	using Fusion.Editor;

	[CustomEditor(typeof(PlayerInterestManager), true)]
	public class PlayerInterestManagerEditor : BehaviourEditor
	{
		// PRIVATE MEMBERS

		private static InterestProviderSet _interestProviders       = new InterestProviderSet();
		private static List<NetworkObject> _objectsInPlayerInterest = new List<NetworkObject>();

		// Editor INTERFACE

		public override bool RequiresConstantRepaint() => true;

		public override void OnInspectorGUI()
		{
			FusionEditorGUI.InjectScriptHeaderDrawer(serializedObject);

			DrawInspectorGUI();

			PlayerInterestManager playerInterestManager = target as PlayerInterestManager;

			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.LabelField("Player Owner",        $"{playerInterestManager.Player}");
				EditorGUILayout.LabelField("Observed Player",     $"{playerInterestManager.ObservedPlayer}");
				EditorGUILayout.LabelField("Interest Cell Count", $"{playerInterestManager.InterestCellCount}");
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndVertical();

			InterestEditorUtility.DrawInterestButtons(true);

			if (playerInterestManager.Runner.TryGetGlobalInterestManager(out GlobalInterestManager globalInterestManager, false) == true)
			{
				if (globalInterestManager.GetProvidersForPlayer(playerInterestManager.ObservedPlayer, _interestProviders) == true)
				{
					InterestEditorUtility.DrawInterestProviders(_interestProviders, true);
				}

				HashSet<int> playerCells = null;
				if (playerInterestManager.Player == playerInterestManager.Runner.LocalPlayer)
				{
					playerCells = playerInterestManager.GizmoInterestCells;
				}

				if (NetworkRunnerExtensions.GetObjectsInPlayerInterest(playerInterestManager.Runner, playerInterestManager.Player, _objectsInPlayerInterest, playerCells) == true)
				{
					InterestEditorUtility.DrawObjectsInPlayerInterest(_objectsInPlayerInterest, true);
				}
			}
		}

		// PlayerInterestManagerEditor INTERFACE

		protected virtual void DrawInspectorGUI()
		{
			DrawDefaultInspector();
		}
	}
}
