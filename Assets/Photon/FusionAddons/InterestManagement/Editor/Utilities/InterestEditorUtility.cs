namespace Fusion.Addons.InterestManagement.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	/// <summary>
	/// Utility class for drawing info in inspectors.
	/// </summary>
	public static class InterestEditorUtility
	{
		// PRIVATE MEMBERS

		private static bool _providersFoldout       = true;
		private static bool _interestObjectsFoldout = true;
		private static int  _interestObjectCount    = default;

		// PUBLIC METHODS

		public static void DrawLine(Color color, float thickness = 1.0f, float padding = 10.0f)
		{
			Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));

			controlRect.height = thickness;
			controlRect.y += padding * 0.5f;

			EditorGUI.DrawRect(controlRect, color);
		}

		public static void DrawInterestButtons(bool addSpace)
		{
			if (addSpace == true)
			{
				EditorGUILayout.Space();
			}

			EditorGUILayout.BeginHorizontal();
			{
				if (DrawButton($"Draw Player Interest", InterestUtility.DrawPlayerInterest) == true)
				{
					InterestUtility.DrawPlayerInterest = !InterestUtility.DrawPlayerInterest;
				}

				if (DrawButton($"Draw Interest Cells", InterestUtility.DrawInterestCells) == true)
				{
					InterestUtility.DrawInterestCells = !InterestUtility.DrawInterestCells;
				}

				if (DrawButton($"Draw Sample Positions", InterestUtility.DrawSamplePositions) == true)
				{
					InterestUtility.DrawSamplePositions = !InterestUtility.DrawSamplePositions;
				}
			}
			EditorGUILayout.EndHorizontal();

			if (InterestUtility.DrawPlayerInterest == true)
			{
				InterestUtility.InterestObjectGizmoColor = EditorGUILayout.ColorField(InterestUtility.InterestObjectGizmoColor);
				InterestUtility.InterestObjectGizmoSize  = EditorGUILayout.Slider(InterestUtility.InterestObjectGizmoSize, 0.0f, 4.0f);
			}

			if (InterestUtility.DrawInterestCells == true || InterestUtility.DrawSamplePositions == true)
			{
				EditorGUILayout.BeginHorizontal();
				{
					DrawCellSizeButton(8);
					DrawCellSizeButton(16);
					DrawCellSizeButton(24);
					DrawCellSizeButton(32);
					DrawCellSizeButton(48);
					DrawCellSizeButton(64);
				}
				EditorGUILayout.EndHorizontal();

				InterestUtility.InterestCellGizmoColor = EditorGUILayout.ColorField(InterestUtility.InterestCellGizmoColor);
				InterestUtility.InterestCellGizmoSize  = EditorGUILayout.Slider(InterestUtility.InterestCellGizmoSize, 0.0f, 1.0f);
			}
		}

		public static void DrawInterestProviders(InterestProviderSet interestProviders, bool addSpace)
		{
			interestProviders.Sort();
			DrawInterestProviders(interestProviders.ReadOnlyValues, addSpace);
		}

		public static void DrawInterestProviders(List<IInterestProvider> interestProviders, bool addSpace)
		{
			if (interestProviders.Count <= 0)
				return;

			if (addSpace == true)
			{
				EditorGUILayout.Space();
			}

			_providersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_providersFoldout, $"Interest Providers ({interestProviders.Count})");
			if (_providersFoldout == true)
			{
				EditorGUI.BeginDisabledGroup(true);
				{
					for (int i = 0, count = interestProviders.Count; i < count; ++i)
					{
						IInterestProvider interestProvider = interestProviders[i];
						EditorGUILayout.BeginHorizontal();
						GUILayout.Button($"{interestProvider.SortOrder}", GUILayout.Width(40.0f));
						GUILayout.Button($"{interestProvider.InterestMode}", GUILayout.Width(80.0f));
						EditorGUILayout.ObjectField(interestProvider as Component, typeof(Object), true);
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		public static void DrawObjectsInPlayerInterest(List<NetworkObject> objectsInPlayerInterest, bool addSpace)
		{
			if (objectsInPlayerInterest.Count <= 0)
				return;

			if (addSpace == true)
			{
				EditorGUILayout.Space();
			}

			int interestObjectCount = string.IsNullOrEmpty(InterestUtility.InterestObjectFilter) == true ? objectsInPlayerInterest.Count : _interestObjectCount;

			_interestObjectCount = objectsInPlayerInterest.Count;

			_interestObjectsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_interestObjectsFoldout, $"Objects In Player Interest ({interestObjectCount})");
			if (_interestObjectsFoldout == true)
			{
				EditorGUILayout.BeginHorizontal();
				InterestUtility.InterestObjectFilter = EditorGUILayout.TextField(InterestUtility.InterestObjectFilter);
				EditorGUILayout.EndHorizontal();

				EditorGUI.BeginDisabledGroup(true);
				{
					_interestObjectCount = 0;

					foreach (NetworkObject objectInPlayerInterest in objectsInPlayerInterest)
					{
						if (string.IsNullOrEmpty(InterestUtility.InterestObjectFilter) == false && objectInPlayerInterest.name.Contains(InterestUtility.InterestObjectFilter) == false)
							continue;

						EditorGUILayout.ObjectField(objectInPlayerInterest, typeof(Object), true);
						++_interestObjectCount;
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		// PRIVATE METHODS

		private static bool DrawButton(string label, bool isActive)
		{
			Color backupBackgroundColor = GUI.backgroundColor;

			if (isActive == true)
			{
				GUI.backgroundColor = Color.green;
			}

			bool result = GUILayout.Button(label) == true;

			GUI.backgroundColor = backupBackgroundColor;

			return result;
		}

		private static void DrawCellSizeButton(int cellSize)
		{
			if (DrawButton($"{cellSize}", Fusion.Simulation.AreaOfInterest.CELL_SIZE == cellSize) == true)
			{
				Fusion.Simulation.AreaOfInterest.CELL_SIZE = cellSize;
			}
		}
	}
}
