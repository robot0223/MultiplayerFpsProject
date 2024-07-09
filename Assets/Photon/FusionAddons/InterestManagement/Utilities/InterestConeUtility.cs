namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Contains helper methods related to interest cone.
	/// </summary>
	public static class InterestConeUtility
	{
		// PUBLIC METHODS

		/// <summary>
		/// Converts cone to Fusion interest cell coordinate system.
		/// </summary>
		public static void GetCells(Vector3 position, Quaternion rotation, float length, Vector2 startSize, Vector2 endSize, Vector2 minSize, Vector2 maxSize, HashSet<int> cells)
		{
			if (length <= 0.0f)
				return;

			if (startSize.x < 0.0f) startSize.x = 0.0f;
			if (startSize.y < 0.0f) startSize.y = 0.0f;

			if (endSize.x < 0.0f) endSize.x = 0.0f;
			if (endSize.y < 0.0f) endSize.y = 0.0f;

			if (minSize.x <= 0.0f) { minSize.x = Mathf.Min(startSize.x, endSize.x); }
			if (minSize.y <= 0.0f) { minSize.y = Mathf.Min(startSize.y, endSize.y); }

			if (maxSize.x <= 0.0f) { maxSize.x = Mathf.Max(startSize.x, endSize.x); }
			if (maxSize.y <= 0.0f) { maxSize.y = Mathf.Max(startSize.y, endSize.y); }

			if (maxSize.x < minSize.x) { maxSize.x = minSize.x; }
			if (maxSize.y < minSize.y) { maxSize.y = minSize.y; }

			var   gridSize    = Simulation.AreaOfInterest.GetGridSize();
			int   gridSizeX   = gridSize.Item1;
			int   gridSizeY   = gridSize.Item2;
			int   gridSizeZ   = gridSize.Item3;
			int   gridHalfX   = gridSizeX / 2;
			int   gridHalfY   = gridSizeY / 2;
			int   gridHalfZ   = gridSizeZ / 2;
			int   cellSize    = Simulation.AreaOfInterest.GetCellSize();
			int   cellHalf    = cellSize / 2;
			float cellInv     = 1.0f / cellSize;
			float maxStepSize = cellHalf * InterestUtility.SQRT2;

			Vector3 forward = rotation * Vector3.forward;
			Vector3 right   = rotation * Vector3.right;
			Vector3 up      = rotation * Vector3.up;

			int     zSteps = (int)(length / maxStepSize) + 1;
			Vector3 zStep  = forward * length / zSteps;
			float   zInv   = 1.0f / zSteps;

			for (int z = 0; z <= zSteps; ++z)
			{
				float zAlpha = z * zInv;

				float   yLength = Mathf.Clamp(Mathf.LerpUnclamped(startSize.y, endSize.y, zAlpha), minSize.y, maxSize.y);
				int     ySteps  = (int)(yLength / maxStepSize) + 1;
				Vector3 yStep   = up * yLength / ySteps;

				float   xLength = Mathf.Clamp(Mathf.LerpUnclamped(startSize.x, endSize.x, zAlpha), minSize.x, maxSize.x);
				int     xSteps  = (int)(xLength / maxStepSize) + 1;
				Vector3 xStep   = right * xLength / xSteps;

				Vector3 basePosition = position + zStep * z - 0.5f * (yStep * ySteps + xStep * xSteps);

				for (int y = 0; y <= ySteps; ++y)
				{
					Vector3 checkPosition = basePosition + yStep * y;

					for (int x = 0; x <= xSteps; ++x)
					{
						int x1 = gridHalfX + (int)(checkPosition.x * cellInv);
						int y1 = gridHalfY + (int)(checkPosition.y * cellInv);
						int z1 = gridHalfZ + (int)(checkPosition.z * cellInv);

						if (checkPosition.x < 0.0f) { x1 -= 1; }
						if (checkPosition.y < 0.0f) { y1 -= 1; }
						if (checkPosition.z < 0.0f) { z1 -= 1; }

						if (x1 < 0) { x1 = 0; } else if (x1 >= gridSizeX) { x1 = gridSizeX - 1; }
						if (y1 < 0) { y1 = 0; } else if (y1 >= gridSizeY) { y1 = gridSizeY - 1; }
						if (z1 < 0) { z1 = 0; } else if (z1 >= gridSizeZ) { z1 = gridSizeZ - 1; }

						int cell = z1 * gridSizeY * gridSizeX + y1 * gridSizeX + x1 + 1;
						cells.Add(cell);

						checkPosition += xStep;
					}
				}
			}
		}

		/// <summary>
		/// Draws interest cone gizmo.
		/// </summary>
		public static void DrawGizmo(Vector3 position, Quaternion rotation, float length, Vector2 startSize, Vector2 endSize, Vector2 minSize, Vector2 maxSize, Color color, bool drawIcon = true)
		{
			if (length <= 0.0f)
				return;

			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;

			if (drawIcon == true)
			{
				Gizmos.DrawIcon(position, "d_ViewToolOrbit On@2x");
			}

			Vector3 centerPosition = position;

			Vector3 forward = rotation * Vector3.forward;
			Vector3 right   = rotation * Vector3.right;
			Vector3 up      = rotation * Vector3.up;

			Vector3 rightOffset = right * startSize.x * 0.5f;
			Vector3 upOffset    = up    * startSize.y * 0.5f;

			Vector3 point00 = centerPosition + upOffset - rightOffset;
			Vector3 point01 = centerPosition + upOffset + rightOffset;
			Vector3 point02 = centerPosition - upOffset - rightOffset;
			Vector3 point03 = centerPosition - upOffset + rightOffset;

			centerPosition += forward * length;

			rightOffset = right * endSize.x * 0.5f;
			upOffset    = up    * endSize.y * 0.5f;

			Vector3 point10 = centerPosition + upOffset - rightOffset;
			Vector3 point11 = centerPosition + upOffset + rightOffset;
			Vector3 point12 = centerPosition - upOffset - rightOffset;
			Vector3 point13 = centerPosition - upOffset + rightOffset;

			Gizmos.DrawLine(point10, point11);
			Gizmos.DrawLine(point11, point13);
			Gizmos.DrawLine(point13, point12);
			Gizmos.DrawLine(point12, point10);

			Gizmos.DrawLine(point00, point10);
			Gizmos.DrawLine(point01, point11);
			Gizmos.DrawLine(point02, point12);
			Gizmos.DrawLine(point03, point13);

			Gizmos.DrawLine(point00, point01);
			Gizmos.DrawLine(point01, point03);
			Gizmos.DrawLine(point03, point02);
			Gizmos.DrawLine(point02, point00);

			if (InterestUtility.DrawSamplePositions == true)
			{
				DrawSamplePositions(position, rotation, length, startSize, endSize, minSize, maxSize);
			}

			Gizmos.color = gizmoColor;
		}

		/// <summary>
		/// Draws positions that are sampled when resolving interest cells.
		/// </summary>
		public static void DrawSamplePositions(Vector3 position, Quaternion rotation, float length, Vector2 startSize, Vector2 endSize, Vector2 minSize, Vector2 maxSize)
		{
			if (length <= 0.0f)
				return;

			if (startSize.x < 0.0f) startSize.x = 0.0f;
			if (startSize.y < 0.0f) startSize.y = 0.0f;

			if (endSize.x < 0.0f) endSize.x = 0.0f;
			if (endSize.y < 0.0f) endSize.y = 0.0f;

			if (minSize.x <= 0.0f) { minSize.x = Mathf.Min(startSize.x, endSize.x); }
			if (minSize.y <= 0.0f) { minSize.y = Mathf.Min(startSize.y, endSize.y); }

			if (maxSize.x <= 0.0f) { maxSize.x = Mathf.Max(startSize.x, endSize.x); }
			if (maxSize.y <= 0.0f) { maxSize.y = Mathf.Max(startSize.y, endSize.y); }

			if (maxSize.x < minSize.x) { maxSize.x = minSize.x; }
			if (maxSize.y < minSize.y) { maxSize.y = minSize.y; }

			var   gridSize    = Simulation.AreaOfInterest.GetGridSize();
			int   gridSizeX   = gridSize.Item1;
			int   gridSizeY   = gridSize.Item2;
			int   gridSizeZ   = gridSize.Item3;
			int   gridHalfX   = gridSizeX / 2;
			int   gridHalfY   = gridSizeY / 2;
			int   gridHalfZ   = gridSizeZ / 2;
			int   cellSize    = Simulation.AreaOfInterest.GetCellSize();
			int   cellHalf    = cellSize / 2;
			float cellInv     = 1.0f / cellSize;
			float maxStepSize = cellHalf * InterestUtility.SQRT2;

			Vector3 forward = rotation * Vector3.forward;
			Vector3 right   = rotation * Vector3.right;
			Vector3 up      = rotation * Vector3.up;

			int     zSteps = (int)(length / maxStepSize) + 1;
			Vector3 zStep  = forward * length / zSteps;
			float   zInv   = 1.0f / zSteps;

			Color gizmoColor = Gizmos.color;
			Gizmos.color = Color.red;

			for (int z = 0; z <= zSteps; ++z)
			{
				float zAlpha = z * zInv;

				float   yLength = Mathf.Clamp(Mathf.LerpUnclamped(startSize.y, endSize.y, zAlpha), minSize.y, maxSize.y);
				int     ySteps  = (int)(yLength / maxStepSize) + 1;
				Vector3 yStep   = up * yLength / ySteps;

				float   xLength = Mathf.Clamp(Mathf.LerpUnclamped(startSize.x, endSize.x, zAlpha), minSize.x, maxSize.x);
				int     xSteps  = (int)(xLength / maxStepSize) + 1;
				Vector3 xStep   = right * xLength / xSteps;

				Vector3 basePosition = position + zStep * z - 0.5f * (yStep * ySteps + xStep * xSteps);

				for (int y = 0; y <= ySteps; ++y)
				{
					Vector3 checkPosition = basePosition + yStep * y;

					for (int x = 0; x <= xSteps; ++x)
					{
						Gizmos.DrawSphere(checkPosition, cellSize * 0.05f);

						int x1 = gridHalfX + (int)(checkPosition.x * cellInv);
						int y1 = gridHalfY + (int)(checkPosition.y * cellInv);
						int z1 = gridHalfZ + (int)(checkPosition.z * cellInv);

						if (checkPosition.x < 0.0f) { x1 -= 1; }
						if (checkPosition.y < 0.0f) { y1 -= 1; }
						if (checkPosition.z < 0.0f) { z1 -= 1; }

						if (x1 < 0) { x1 = 0; } else if (x1 >= gridSizeX) { x1 = gridSizeX - 1; }
						if (y1 < 0) { y1 = 0; } else if (y1 >= gridSizeY) { y1 = gridSizeY - 1; }
						if (z1 < 0) { z1 = 0; } else if (z1 >= gridSizeZ) { z1 = gridSizeZ - 1; }

						Vector3 cellPosition = default;
						cellPosition.x = (x1 - gridHalfX) * cellSize + cellHalf;
						cellPosition.y = (y1 - gridHalfY) * cellSize + cellHalf;
						cellPosition.z = (z1 - gridHalfZ) * cellSize + cellHalf;

						checkPosition += xStep;
					}
				}
			}

			Gizmos.color = gizmoColor;
		}
	}
}
