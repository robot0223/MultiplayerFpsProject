namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Contains utility methods for sorting, gizmo drawing and miscellaneous.
	/// </summary>
	public static class InterestUtility
	{
		// CONSTANTS

		public const float SQRT2 = 1.4142135623730950488016887242097f;
		public const float SQRT3 = 1.7320508075688772935274463415059f;

		// PUBLIC MEMBERS

		/// <summary>Enable drawing of interest cells gizmos.</summary>
		public static bool DrawInterestCells = false;
		/// <summary>Enable drawing of player interest gizmos.</summary>
		public static bool DrawPlayerInterest = true;
		/// <summary>Enable drawing of interest shape sampling positions.</summary>
		public static bool DrawSamplePositions = false;
		/// <summary>Size of interest cell gizmo. Valid range is 0.0f - 1.0f.</summary>
		public static float InterestCellGizmoSize = 1.0f;
		/// <summary>Color of interest cell gizmo.</summary>
		public static Color InterestCellGizmoColor = new Color(0.0f, 1.0f, 0.0f, 0.25f);
		/// <summary>Size of interest object gizmo. Valid range is 0.0f - 1.0f.</summary>
		public static float InterestObjectGizmoSize = 2.0f;
		/// <summary>Color of interest object gizmo.</summary>
		public static Color InterestObjectGizmoColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
		/// <summary>Color of interest object gizmo.</summary>
		public static string InterestObjectFilter = default;

		// PUBLIC METHODS

		/// <summary>
		/// Sorts interest providers based on IInterestProvider.SortOrder property.
		/// Providers with same value don't change relative order.
		/// </summary>
		public static void SortInterestProviders(List<IInterestProvider> interestProviders)
		{
			int count = interestProviders.Count;
			if (count <= 1)
				return;

			bool isSorted = false;
			int  leftIndex;
			int  rightIndex;

			while (isSorted == false)
			{
				isSorted = true;

				leftIndex  = count - 2;
				rightIndex = count - 1;

				IInterestProvider rightProvider = interestProviders[rightIndex];

				while (rightIndex > 0)
				{
					IInterestProvider leftProvider = interestProviders[leftIndex];

					if (rightProvider.SortOrder >= leftProvider.SortOrder)
					{
						rightProvider = leftProvider;
					}
					else
					{
						interestProviders[rightIndex] = leftProvider;
						interestProviders[leftIndex]  = rightProvider;

						isSorted = false;
					}

					--leftIndex;
					--rightIndex;
				}
			}
		}

		/// <summary>
		/// Draws interest cells gizmos.
		/// </summary>
		public static void DrawCells(HashSet<int> cells)
		{
			DrawCells(cells, InterestCellGizmoColor, InterestCellGizmoSize);
		}

		/// <summary>
		/// Draws interest cells gizmos.
		/// </summary>
		public static void DrawCells(HashSet<int> cells, Color color)
		{
			DrawCells(cells, color, InterestCellGizmoSize);
		}

		/// <summary>
		/// Draws interest cells gizmos.
		/// </summary>
		public static void DrawCells(HashSet<int> cells, Color color, float size)
		{
			if (size <= 0.0f)
				return;
			if (cells.Count <= 0)
				return;

			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;

			var cellSize = Simulation.AreaOfInterest.GetCellSize();
			var gridSize = Simulation.AreaOfInterest.GetGridSize();

			int gridSizeX  = gridSize.x;
			int gridSizeXY = gridSize.x * gridSize.y;
			int gridHalfX  = gridSize.x / 2;
			int gridHalfY  = gridSize.y / 2;
			int gridHalfZ  = gridSize.z / 2;
			int cellHalf   = cellSize / 2;

			float mcX = cellHalf - gridHalfX * cellSize;
			float mcY = cellHalf - gridHalfY * cellSize;
			float mcZ = cellHalf - gridHalfZ * cellSize;

			Vector3 cubeSize = Vector3.one * cellSize * size;

			foreach (int cell in cells)
			{
				int index = cell - 1;
				int z = index / gridSizeXY;
				index -= z * gridSizeXY;
				int y = index / gridSizeX;
				int x = index % gridSizeX;

				Vector3 cellCenter;
				cellCenter.x = x * cellSize + mcX;
				cellCenter.y = y * cellSize + mcY;
				cellCenter.z = z * cellSize + mcZ;

				Gizmos.DrawCube(cellCenter, cubeSize);
			}

			Gizmos.color = gizmoColor;
		}

		/// <summary>
		/// Draws gizmos for all objects in player interest.
		/// </summary>
		public static void DrawObjectsInPlayerInterest(List<NetworkObject> objectsInPlayerInterest)
		{
			DrawObjectsInPlayerInterest(objectsInPlayerInterest, InterestObjectGizmoColor, InterestObjectGizmoSize);
		}

		/// <summary>
		/// Draws gizmos for all objects in player interest.
		/// </summary>
		public static void DrawObjectsInPlayerInterest(List<NetworkObject> objectsInPlayerInterest, Color color)
		{
			DrawObjectsInPlayerInterest(objectsInPlayerInterest, color, InterestObjectGizmoSize);
		}

		/// <summary>
		/// Draws gizmos for all objects in player interest.
		/// </summary>
		public static void DrawObjectsInPlayerInterest(List<NetworkObject> objectsInPlayerInterest, Color color, float size)
		{
			if (size <= 0.0f)
				return;

			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;

			for (int i = 0, count = objectsInPlayerInterest.Count; i < count; ++i)
			{
				NetworkObject networkObject = objectsInPlayerInterest[i];

				if (string.IsNullOrEmpty(InterestObjectFilter) == false && networkObject.name.Contains(InterestObjectFilter) == false)
					continue;

				Gizmos.DrawWireSphere(networkObject.transform.position, size);
			}

			Gizmos.color = gizmoColor;
		}

		/// <summary>
		/// Draws camera frustum gizmo.
		/// </summary>
		public static void DrawCameraFrustum(Camera camera, Vector3 position, Quaternion rotation, Color color)
		{
			if (ReferenceEquals(camera, null) == true)
				return;

			Matrix4x4 gizmoMatrix = Gizmos.matrix;
			Color     gizmoColor  = Gizmos.color;

			Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
			Gizmos.color  = color;

			Gizmos.DrawFrustum(Vector3.zero, camera.fieldOfView, camera.farClipPlane, camera.nearClipPlane, camera.aspect);

			Gizmos.matrix = gizmoMatrix;
			Gizmos.color  = gizmoColor;
		}

		/// <summary>
		/// Draws line gizmo.
		/// </summary>
		public static void DrawLine(Vector3 fromPosition, Vector3 toPosition, Color color)
		{
			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;
			Gizmos.DrawLine(fromPosition, toPosition);
			Gizmos.color = gizmoColor;
		}

		/// <summary>
		/// Draws wire sphere gizmo.
		/// </summary>
		public static void DrawWireSphere(Vector3 position, float radius, Color color)
		{
			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;
			Gizmos.DrawWireSphere(position, radius);
			Gizmos.color = gizmoColor;
		}

		/// <summary>
		/// Draws wire cube gizmo.
		/// </summary>
		public static void DrawWireCube(Vector3 position, Vector3 size, Color color)
		{
			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;
			Gizmos.DrawWireCube(position, size);
			Gizmos.color = gizmoColor;
		}

		/// <summary>
		/// Draws angle gizmo.
		/// </summary>
		public static void DrawAngle(Vector3 position, Quaternion rotation, float angle, float minDistance, float maxDistance, Color color, int rollSteps = 16, int pitchSteps = 4)
		{
			Gizmos.DrawIcon(position, "d_ViewToolOrbit On@2x");

			if (rollSteps  < 6) { rollSteps  = 6; }
			if (pitchSteps < 3) { pitchSteps = 3; }

			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;

			Vector3 centerMinPoint = position + rotation * new Vector3(0.0f, 0.0f, minDistance);
			Vector3 centerMaxPoint = position + rotation * new Vector3(0.0f, 0.0f, maxDistance);

			List<AngleSegment> segments = new List<AngleSegment>();

			for (int i = 1; i <= pitchSteps; ++i)
			{
				AngleSegment segment = new AngleSegment();
				segment.PointRotation = Quaternion.Euler(angle * i / pitchSteps, 0.0f, 0.0f);
				segment.BaseMinPoint  = segment.PointRotation * new Vector3(0.0f, 0.0f, minDistance);
				segment.BaseMaxPoint  = segment.PointRotation * new Vector3(0.0f, 0.0f, maxDistance);
				segment.LastMinPoint  = position + rotation * segment.BaseMinPoint;
				segment.LastMaxPoint  = position + rotation * segment.BaseMaxPoint;

				segments.Add(segment);
			}

			AngleSegment firstSegment = segments[0];
			Gizmos.DrawLine(centerMinPoint, firstSegment.LastMinPoint);
			Gizmos.DrawLine(centerMaxPoint, firstSegment.LastMaxPoint);

			for (int i = 1; i < segments.Count; ++i)
			{
				AngleSegment currentSegment  = segments[i];
				AngleSegment previousSegment = segments[i - 1];

				Gizmos.DrawLine(previousSegment.LastMinPoint, currentSegment.LastMinPoint);
				Gizmos.DrawLine(previousSegment.LastMaxPoint, currentSegment.LastMaxPoint);
			}

			AngleSegment lastSegment = segments[segments.Count - 1];
			Gizmos.DrawLine(lastSegment.LastMinPoint, lastSegment.LastMaxPoint);

			for (int j = 1; j <= rollSteps; ++j)
			{
				for (int i = 0; i < segments.Count; ++i)
				{
					AngleSegment segment = segments[i];

					segment.PointRotation = Quaternion.Euler(0.0f, 0.0f, 360.0f * j / rollSteps);
					segment.MinPoint      = position + rotation * (segment.PointRotation * segment.BaseMinPoint);
					segment.MaxPoint      = position + rotation * (segment.PointRotation * segment.BaseMaxPoint);

					Gizmos.DrawLine(segment.LastMinPoint, segment.MinPoint);
					Gizmos.DrawLine(segment.LastMaxPoint, segment.MaxPoint);

					segments[i] = segment;
				}

				firstSegment = segments[0];
				Gizmos.DrawLine(centerMinPoint, firstSegment.LastMinPoint);
				Gizmos.DrawLine(centerMaxPoint, firstSegment.LastMaxPoint);

				for (int i = 1; i < segments.Count; ++i)
				{
					AngleSegment currentSegment  = segments[i];
					AngleSegment previousSegment = segments[i - 1];

					Gizmos.DrawLine(previousSegment.LastMinPoint, currentSegment.LastMinPoint);
					Gizmos.DrawLine(previousSegment.LastMaxPoint, currentSegment.LastMaxPoint);
				}

				lastSegment = segments[segments.Count - 1];
				Gizmos.DrawLine(lastSegment.LastMinPoint, lastSegment.LastMaxPoint);


				for (int i = 0; i < segments.Count; ++i)
				{
					AngleSegment segment = segments[i];

					segment.LastMinPoint = segment.MinPoint;
					segment.LastMaxPoint = segment.MaxPoint;

					segments[i] = segment;
				}
			}

			Gizmos.color = gizmoColor;
		}

		// DATA STRUCTURES

		private struct AngleSegment
		{
			public Quaternion PointRotation;
			public Vector3    BaseMinPoint;
			public Vector3    BaseMaxPoint;
			public Vector3    LastMinPoint;
			public Vector3    LastMaxPoint;
			public Vector3    MinPoint;
			public Vector3    MaxPoint;
		}
	}
}
