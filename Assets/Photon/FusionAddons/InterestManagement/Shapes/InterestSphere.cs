namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Interest shape that provides configuration for sphere and its convertion to interest cells.
	/// Supports interest cells caching - cells from previous evaluation are reused until properties or transform change.
	/// </summary>
	public class InterestSphere : InterestShape
	{
		// PUBLIC MEMBERS

		[InlineHelp][Tooltip("Only enabled shapes are evaluated and contribute to player interest.")]
		public bool IsEnabled = true;
		[InlineHelp][Tooltip("Radius of the sphere.")]
		public float Radius = 1.0f;
		[InlineHelp][Tooltip("Color used for rendering shape gizmos.")]
		public Color GizmoColor = Color.white;

		// PRIVATE MEMBERS

		private HashSet<int> _cachedCells = new HashSet<int>();
		private Vector3      _cachedPosition;
		private Quaternion   _cachedRotation;
		private int          _cachedCellSize;
		private float        _cachedRadius;
		private long         _cachedVersion;

		// InterestShape INTERFACE

		public override void GetCells(PlayerInterestView playerView, HashSet<int> cells, EInterestMode interestMode)
		{
			if (IsEnabled == false)
				return;

			Transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

			if (position.Equals(_cachedPosition) == false || rotation.Equals(_cachedRotation) == false || Radius != _cachedRadius || Fusion.Simulation.AreaOfInterest.CELL_SIZE != _cachedCellSize || Version != _cachedVersion)
			{
				_cachedCells.Clear();

				InterestSphereUtility.GetCells(position, Radius, _cachedCells);

				_cachedPosition = position;
				_cachedRotation = rotation;
				_cachedCellSize = Fusion.Simulation.AreaOfInterest.CELL_SIZE;
				_cachedRadius   = Radius;
				_cachedVersion  = Version;
			}

			CombineCells(_cachedCells, cells, interestMode);
		}

		public override void DrawGizmo(PlayerInterestView playerView)
		{
			if (IsEnabled == false)
				return;

			Transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
			InterestSphereUtility.DrawGizmo(position, Radius, GizmoColor);
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
