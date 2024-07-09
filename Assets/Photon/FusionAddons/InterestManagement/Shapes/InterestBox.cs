namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Interest shape that provides configuration for box and its convertion to interest cells.
	/// Regular box internally uses same algorithms as cone shape.
	/// Axis-aligned box is much faster to evaluate.
	/// Supports interest cells caching - cells from previous evaluation are reused until properties or transform change.
	/// </summary>
	public class InterestBox : InterestShape
	{
		// PUBLIC MEMBERS

		[InlineHelp][Tooltip("Only enabled shapes are evaluated and contribute to player interest.")]
		public bool IsEnabled = true;
		[InlineHelp][Tooltip("Use axis-aligned box instead of regular. It ignores object rotation but is much faster to evaluate.")]
		public bool IsAxisAligned = true;
		[InlineHelp][Tooltip("Defines size of the interest box.")]
		public Vector3 Size = Vector3.one;
		[InlineHelp][Tooltip("Color used for rendering shape gizmos.")]
		public Color GizmoColor = Color.white;

		// PRIVATE MEMBERS

		private HashSet<int> _cachedCells = new HashSet<int>();
		private Vector3      _cachedPosition;
		private Quaternion   _cachedRotation;
		private int          _cachedCellSize;
		private bool         _cachedIsAxisAligned;
		private Vector3      _cachedSize;
		private long         _cachedVersion;

		// InterestShape INTERFACE

		public override void GetCells(PlayerInterestView playerView, HashSet<int> cells, EInterestMode interestMode)
		{
			if (IsEnabled == false)
				return;

			Transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

			if (position.Equals(_cachedPosition) == false || rotation.Equals(_cachedRotation) == false || IsAxisAligned != _cachedIsAxisAligned || Size.Equals(_cachedSize) == false || Fusion.Simulation.AreaOfInterest.CELL_SIZE != _cachedCellSize || Version != _cachedVersion)
			{
				_cachedCells.Clear();

				InterestBoxUtility.GetCells(position, rotation, Size, IsAxisAligned, _cachedCells);

				_cachedPosition      = position;
				_cachedRotation      = rotation;
				_cachedCellSize      = Fusion.Simulation.AreaOfInterest.CELL_SIZE;
				_cachedIsAxisAligned = IsAxisAligned;
				_cachedSize          = Size;
				_cachedVersion       = Version;
			}

			CombineCells(_cachedCells, cells, interestMode);
		}

		public override void DrawGizmo(PlayerInterestView playerView)
		{
			if (IsEnabled == false)
				return;

			Transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
			InterestBoxUtility.DrawGizmo(position, rotation, Size, IsAxisAligned, GizmoColor);
		}

		// PROTECTED METHODS

		/// <summary>
		/// Marks cached interest cells as invalid, forcing new shape => cells conversion when the shape is evaluated next time.
		/// </summary>
		protected void InvalidateCache()
		{
			_cachedVersion = default;
		}
	}
}
