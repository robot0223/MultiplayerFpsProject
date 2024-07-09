namespace Fusion.Addons.InterestManagement
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Base class for interest shapes. The purpose of this component is to hold configuration,
	/// get immediate visual feedback and convert to Fusion interest cell coordinate system.
	/// </summary>
	public abstract class InterestShape : MonoBehaviour
	{
		// PUBLIC MEMBERS

		/// <summary>
		/// Cached Transform component.
		/// </summary>
		public Transform Transform { get { if (ReferenceEquals(_transform, null) == true) { _transform = transform; } return transform; } }

		// PROTECTED MEMBERS

		/// <summary>
		/// Used for cache invalidation.
		/// </summary>
		protected static readonly long Version = DateTime.Now.Ticks;

		// PRIVATE MEMBERS

		private Transform _transform;

		// InterestShape INTERFACE

		/// <summary>
		/// Override this method to convert interest shape into interest cells.
		/// PlayerInterestView can be optionally used for further filtering.
		/// </summary>
		/// <param name="playerView">Player interest view with player information.</param>
		/// <param name="cells">Interest cells that should be set.</param>
		/// <param name="interestMode">Interest mode of the shape.</param>
		public abstract void GetCells(PlayerInterestView playerView, HashSet<int> cells, EInterestMode interestMode);

		/// <summary>
		/// Override this method to draw shape gizmos.
		/// </summary>
		/// <param name="playerView">Player interest view with player information. Can be null.</param>
		public abstract void DrawGizmo(PlayerInterestView playerView);

		/// <summary>
		/// Combines interest cells based on interest mode.
		/// </summary>
		protected static void CombineCells(HashSet<int> sourceCells, HashSet<int> targetCells, EInterestMode interestMode)
		{
			if (interestMode == EInterestMode.Default || interestMode == EInterestMode.Override)
			{
				foreach (int cell in sourceCells)
				{
					targetCells.Add(cell);
				}
			}
			else if (interestMode == EInterestMode.Exclude)
			{
				foreach (int cell in sourceCells)
				{
					targetCells.Remove(cell);
				}
			}
			else
			{
				throw new NotSupportedException(nameof(interestMode));
			}
		}

#if UNITY_EDITOR
		[UnityEditor.DrawGizmo(UnityEditor.GizmoType.Active)]
		private static void DrawGizmo(InterestShape interestShape, UnityEditor.GizmoType gizmoType)
		{
			if ((gizmoType & UnityEditor.GizmoType.Active) != UnityEditor.GizmoType.Active)
				return;

			interestShape.DrawGizmo(default);
		}
#endif
	}
}
