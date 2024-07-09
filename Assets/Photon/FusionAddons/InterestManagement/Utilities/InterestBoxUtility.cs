namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Contains helper methods related to interest box.
	/// </summary>
	public static class InterestBoxUtility
	{
		// PUBLIC METHODS

		/// <summary>
		/// Converts box to Fusion interest cell coordinate system.
		/// </summary>
		public static void GetCells(Vector3 position, Quaternion rotation, Vector3 size, bool isAxisAligned, HashSet<int> cells)
		{
			if (isAxisAligned == false)
			{
				Vector2 coneSize   = new Vector2(size.x, size.y);
				float   coneLength = size.z;

				position -= rotation * Vector3.forward * coneLength * 0.5f;

				InterestConeUtility.GetCells(position, rotation, coneLength, coneSize, coneSize, coneSize, coneSize, cells);
				return;
			}

			var   gridSize   = Simulation.AreaOfInterest.GetGridSize();
			int   gridSizeX  = gridSize.Item1;
			int   gridSizeY  = gridSize.Item2;
			int   gridSizeZ  = gridSize.Item3;
			int   gridSizeXY = gridSizeX * gridSizeY;
			int   gridHalfX  = gridSizeX / 2;
			int   gridHalfY  = gridSizeY / 2;
			int   gridHalfZ  = gridSizeZ / 2;
			int   cellSize   = Simulation.AreaOfInterest.GetCellSize();
			float cellInv    = 1.0f / cellSize;

			if (size.x > 0.0f && size.y > 0.0f && size.z > 0.0f)
			{
				size *= 0.5f;

				GetCellCoordinates(position - size, out int minX, out int minY, out int minZ);
				GetCellCoordinates(position + size, out int maxX, out int maxY, out int maxZ);

				for (int z = minZ * gridSizeXY; z <= maxZ * gridSizeXY; z += gridSizeXY)
				{
					for (int y = minY * gridSizeX; y <= maxY * gridSizeX; y += gridSizeX)
					{
						for (int x = minX; x <= maxX; ++x)
						{
							int cell = z + y + x + 1;
							cells.Add(cell);
						}
					}
				}
			}
			else
			{
				GetCellCoordinates(position, out int x, out int y, out int z);

				int cell = z * gridSizeXY + y * gridSizeX + x + 1;
				cells.Add(cell);
			}

			return;

			void GetCellCoordinates(Vector3 targetPosition, out int x, out int y, out int z)
			{
				x = gridHalfX + (int)(targetPosition.x * cellInv);
				y = gridHalfY + (int)(targetPosition.y * cellInv);
				z = gridHalfZ + (int)(targetPosition.z * cellInv);

				if (targetPosition.x < 0.0f) { x -= 1; }
				if (targetPosition.y < 0.0f) { y -= 1; }
				if (targetPosition.z < 0.0f) { z -= 1; }

				if (x < 0) { x = 0; } else if (x >= gridSizeX) { x = gridSizeX - 1; }
				if (y < 0) { y = 0; } else if (y >= gridSizeY) { y = gridSizeY - 1; }
				if (z < 0) { z = 0; } else if (z >= gridSizeZ) { z = gridSizeZ - 1; }
			}
		}

		/// <summary>
		/// Draws interest box gizmo.
		/// </summary>
		public static void DrawGizmo(Vector3 position, Quaternion rotation, Vector3 size, bool isAxisAligned, Color color, bool drawIcon = true)
		{
			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;

			if (drawIcon == true)
			{
				Gizmos.DrawIcon(position, "d_ViewToolOrbit On@2x");
			}

			if (isAxisAligned == true)
			{
				if (size.x > 0.0f && size.y > 0.0f && size.z > 0.0f)
				{
					Gizmos.DrawWireCube(position, size);
				}
			}
			else
			{
				Vector2 coneSize   = new Vector2(size.x, size.y);
				float   coneLength = size.z;

				position -= rotation * Vector3.forward * coneLength * 0.5f;

				InterestConeUtility.DrawGizmo(position, rotation, coneLength, coneSize, coneSize, coneSize, coneSize, color, false);
			}

			Gizmos.color = gizmoColor;
		}
	}
}
