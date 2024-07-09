namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Interest shape that provides configuration for cone and its convertion to interest cells.
	/// Supports interest cells caching - cells from previous evaluation are reused until properties or transform change.
	/// </summary>
	public class InterestCone : InterestShape
	{
		// PUBLIC MEMBERS

		[InlineHelp][Tooltip("Only enabled shapes are evaluated and contribute to player interest.")]
		public bool IsEnabled = true;
		[InlineHelp][Tooltip("Length of the cone.")]
		public float Length = 1.0f;
		[InlineHelp][Tooltip("Width(x) and height(y) at the start of the cone.")]
		public Vector2 StartSize;
		[InlineHelp][Tooltip("Width(x) and height(y) at the end of the cone.")]
		public Vector2 EndSize = Vector2.one;
		[InlineHelp][Tooltip("Minimum width(x) or height(y) of the cone at any distance. If set, StartSize and EndSize are clamped to this value.")]
		public Vector2 MinSize;
		[InlineHelp][Tooltip("Maximum width(x) or height(y) of the cone at any distance. If set, StartSize and EndSize are clamped to this value.")]
		public Vector2 MaxSize;
		[InlineHelp][Tooltip("Color used for rendering shape gizmos.")]
		public Color GizmoColor = Color.white;

		// PRIVATE MEMBERS

		private HashSet<int> _cachedCells = new HashSet<int>();
		private Vector3      _cachedPosition;
		private Quaternion   _cachedRotation;
		private int          _cachedCellSize;
		private float        _cachedLength;
		private Vector2      _cachedStartSize;
		private Vector2      _cachedEndSize;
		private Vector2      _cachedMinSize;
		private Vector2      _cachedMaxSize;
		private long         _cachedVersion;

		// InterestShape INTERFACE

		public override void GetCells(PlayerInterestView playerView, HashSet<int> cells, EInterestMode interestMode)
		{
			if (IsEnabled == false || Length <= 0.0f)
				return;

			Transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

			if (position.Equals(_cachedPosition) == false || rotation.Equals(_cachedRotation) == false || Length != _cachedLength || StartSize.Equals(_cachedStartSize) == false || EndSize.Equals(_cachedEndSize) == false || MinSize.Equals(_cachedMinSize) == false || MaxSize.Equals(_cachedMaxSize) == false || Fusion.Simulation.AreaOfInterest.CELL_SIZE != _cachedCellSize || Version != _cachedVersion)
			{
				_cachedCells.Clear();

				InterestConeUtility.GetCells(position, rotation, Length, StartSize, EndSize, MinSize, MaxSize, _cachedCells);

				_cachedPosition  = position;
				_cachedRotation  = rotation;
				_cachedCellSize  = Fusion.Simulation.AreaOfInterest.CELL_SIZE;
				_cachedLength    = Length;
				_cachedStartSize = StartSize;
				_cachedEndSize   = EndSize;
				_cachedMinSize   = MinSize;
				_cachedMaxSize   = MaxSize;
				_cachedVersion   = Version;
			}

			CombineCells(_cachedCells, cells, interestMode);
		}

		public override void DrawGizmo(PlayerInterestView playerView)
		{
			if (IsEnabled == false || Length <= 0.0f)
				return;

			Transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
			InterestConeUtility.DrawGizmo(position, rotation, Length, StartSize, EndSize, MinSize, MaxSize, GizmoColor);
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
