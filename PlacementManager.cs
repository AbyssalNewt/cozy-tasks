
using Godot;
using System.Collections.Generic;

public partial class PlacementManager : Node
{
	[Export] public TileMapLayer Floor;
	[Export] public TileMapLayer Walls;

	// Cells currently occupied by furniture.
	private readonly HashSet<Vector2I> occupiedCells = new();

	// Convert a global mouse position to grid coordinates.
	public Vector2I GetGridPosition(Vector2 globalPosition)
	{
		Vector2 localPosition = Floor.ToLocal(globalPosition);
		return Floor.LocalToMap(localPosition);
	}

	// Get the center of a grid cell in global coordinates.
	public Vector2 GetWorldPosition(Vector2I cell)
	{
		return Floor.ToGlobal(Floor.MapToLocal(cell));
	}

	// Check whether a grid cell contains a floor.
	public bool HasFloor(Vector2I cell)
	{
		return Floor.GetCellSourceId(cell) != -1;
	}

	// Check whether a cell contains a blocking wall.
	public bool HasWall(Vector2I cell)
	{
		return Walls.GetCellSourceId(cell) != -1;
	}

	// Check whether furniture can occupy a region.
	public bool CanPlace(Vector2I origin, Vector2I size)
	{
		if (size.X <= 0 || size.Y <= 0)
			return false;

		for (int x = 0; x < size.X; x++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				Vector2I cell = origin + new Vector2I(x, y);

				if (!HasFloor(cell))
					return false;

				if (HasWall(cell))
					return false;

				if (occupiedCells.Contains(cell))
					return false;
			}
		}

		return true;
	}
	
	public Vector2 GetFurnitureCenter(Vector2I origin,Vector2I size)
	{
  		Vector2 tileSize = Floor.TileSet.TileSize;

		Vector2 centerOffset = new Vector2((size.X - 1) * tileSize.X / 2f,(size.Y - 1) * tileSize.Y / 2f);
		Vector2 localPosition = Floor.MapToLocal(origin) + centerOffset;
		return Floor.ToGlobal(localPosition);
		}

	// Mark cells as occupied.
	public void OccupyCells(Vector2I origin, Vector2I size)
	{
		for (int x = 0; x < size.X; x++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				occupiedCells.Add(origin + new Vector2I(x, y));
			}
		}
	}

	// Free cells when moving or removing furniture.
	public void FreeCells(Vector2I origin, Vector2I size)
	{
		for (int x = 0; x < size.X; x++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				occupiedCells.Remove(origin + new Vector2I(x, y));
			}
		}
	}
}
