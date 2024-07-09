namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Contains helper methods related to interest sphere.
	/// </summary>
	public static class InterestSphereUtility
	{
		// PUBLIC METHODS

		/// <summary>
		/// Converts sphere to Fusion interest cell coordinate system.
		/// </summary>
		public static void GetCells(Vector3 position, float radius, HashSet<int> cells)
		{
			var   gridSize   = Simulation.AreaOfInterest.GetGridSize();
			int   gridSizeX  = gridSize.Item1;
			int   gridSizeY  = gridSize.Item2;
			int   gridSizeXY = gridSizeX * gridSizeY;
			int   gridSizeZ  = gridSize.Item3;
			int   gridHalfX  = gridSizeX / 2;
			int   gridHalfY  = gridSizeY / 2;
			int   gridHalfZ  = gridSizeZ / 2;
			int   cellSize   = Simulation.AreaOfInterest.GetCellSize();
			int   cellHalf   = cellSize / 2;
			float cellInv    = 1.0f / cellSize;

			if (radius > 0.0f)
			{
				float sqrDistance = radius + cellHalf * InterestUtility.SQRT3;
				sqrDistance *= sqrDistance;

				GetCellCoordinates(position - new Vector3(radius, radius, radius), out int minX, out int minY, out int minZ);
				GetCellCoordinates(position + new Vector3(radius, radius, radius), out int maxX, out int maxY, out int maxZ);

				for (int z = minZ; z <= maxZ; ++z)
				{
					for (int y = minY; y <= maxY; ++y)
					{
						for (int x = minX; x <= maxX; ++x)
						{
							Vector3 cellPosition = default;
							cellPosition.x = (float)((x - gridHalfX) * cellSize + cellHalf);
							cellPosition.y = (float)((y - gridHalfY) * cellSize + cellHalf);
							cellPosition.z = (float)((z - gridHalfZ) * cellSize + cellHalf);

							if (Vector3.SqrMagnitude(cellPosition - position) <= sqrDistance)
							{
								int cell = z * gridSizeXY + y * gridSizeX + x + 1;
								cells.Add(cell);
							}
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
		/// Draws interest sphere gizmo.
		/// </summary>
		public static void DrawGizmo(Vector3 position, float radius, Color color, bool drawIcon = true)
		{
			Color gizmoColor = Gizmos.color;
			Gizmos.color = color;

			if (drawIcon == true)
			{
				Gizmos.DrawIcon(position, "d_ViewToolOrbit On@2x");
			}

			if (radius > 0.0f)
			{
				Gizmos.DrawWireSphere(position, radius);
			}

			if (InterestUtility.DrawSamplePositions == true)
			{
				DrawSamplePositions(position, radius);
			}

			Gizmos.color = gizmoColor;
		}

		/// <summary>
		/// Draws positions that are sampled when resolving interest cells.
		/// </summary>
		public static void DrawSamplePositions(Vector3 position, float radius)
		{
			Color gizmoColor = Gizmos.color;

			var   gridSize  = Simulation.AreaOfInterest.GetGridSize();
			int   gridSizeX = gridSize.Item1;
			int   gridSizeY = gridSize.Item2;
			int   gridSizeZ = gridSize.Item3;
			int   gridHalfX = gridSizeX / 2;
			int   gridHalfY = gridSizeY / 2;
			int   gridHalfZ = gridSizeZ / 2;
			int   cellSize  = Simulation.AreaOfInterest.GetCellSize();
			int   cellHalf  = cellSize / 2;
			float cellInv   = 1.0f / cellSize;

			if (radius > 0.0f)
			{
				float sqrDistance = radius + cellHalf * InterestUtility.SQRT3;
				sqrDistance *= sqrDistance;

				GetCellCoordinates(position - new Vector3(radius, radius, radius), out int minX, out int minY, out int minZ);
				GetCellCoordinates(position + new Vector3(radius, radius, radius), out int maxX, out int maxY, out int maxZ);

				for (int z = minZ; z <= maxZ; ++z)
				{
					for (int y = minY; y <= maxY; ++y)
					{
						for (int x = minX; x <= maxX; ++x)
						{
							Vector3 cellPosition = default;
							cellPosition.x = (float)((x - gridHalfX) * cellSize + cellHalf);
							cellPosition.y = (float)((y - gridHalfY) * cellSize + cellHalf);
							cellPosition.z = (float)((z - gridHalfZ) * cellSize + cellHalf);

							if (Vector3.SqrMagnitude(cellPosition - position) > sqrDistance)
							{
								Gizmos.color = Color.red;
								Gizmos.DrawCube(cellPosition, new Vector3(cellHalf, cellHalf, cellHalf));
							}
							else
							{
								Gizmos.color = Color.green;
								Gizmos.DrawCube(cellPosition, new Vector3(cellHalf, cellHalf, cellHalf));
							}
						}
					}
				}
			}
			else
			{
				GetCellCoordinates(position, out int x, out int y, out int z);

				Vector3 cellPosition = default;
				cellPosition.x = (x - gridHalfX) * cellSize + cellHalf;
				cellPosition.y = (y - gridHalfY) * cellSize + cellHalf;
				cellPosition.z = (z - gridHalfZ) * cellSize + cellHalf;

				Gizmos.color = Color.green;
				Gizmos.DrawCube(cellPosition, new Vector3(cellHalf, cellHalf, cellHalf));
			}

			Gizmos.color = gizmoColor;

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
	}
}
