
using Godot;
using System.Collections.Generic;

public partial class PlacementManager : Node
{
	[Export] public TileMapLayer Floor;
	[Export] public TileMapLayer Walls;

	// Cells currently occupied by furniture.
	private readonly HashSet<Vector2I> occupiedCells = new();

	// Convert a global mouse position to grid coordinates.
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
	// Convert a 16px sub-grid cell to its origin cell coordinate.
	public Vector2I ToTileCell(Vector2I subGridCell)
	{
		int anchorX = Floor.GetUsedRect().Position.X;
		int anchorY = Floor.GetUsedRect().Position.Y;

		float x = (subGridCell.X - anchorX + 1) / 3f;
		float y = (subGridCell.Y - anchorY + 1) / 3f;

		return new Vector2I(
			(x>0? Mathf.FloorToInt(x) : Mathf.CeilToInt(x)) * 3 + anchorX,
			(y>0? Mathf.FloorToInt(y) : Mathf.CeilToInt(y)) * 3 + anchorY
		);
	}	
	// Check whether a grid cell contains a floor.
	public bool HasFloor(Vector2I cell)
	{
		return Floor.GetCellTileData(ToTileCell(cell)) != null;
	}

	// Check whether a cell contains a blocking wall.
	public bool HasWall(Vector2I cell)
	{
		return Walls.GetCellTileData(ToTileCell(cell)) != null;
	}
	
	
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
	
	public override void _Ready()
	{
		if (Floor == null)
		{
			GD.PushError(
	            "PlacementManager: Floor TileMapLayer is not assigned!"
			);
		}

		if (Walls == null)
		{
			GD.PushError(
	            "PlacementManager: Walls TileMapLayer is not assigned!"
			);
		}
		PrintFloorGrid();
	}
	public void PrintFloorGrid()
{
	if (Floor == null)
	{
		GD.PrintErr("Floor TileMapLayer is not assigned!");
		return;
	}

	Rect2I rect = Floor.GetUsedRect();
	GD.Print($"=== Floor Grid Map (Bounds: Pos {rect.Position}, Size {rect.Size}) ===");

	for (int y = rect.Position.Y; y < rect.End.Y; y++)
	{
		string rowStr = "";
		for (int x = rect.Position.X; x < rect.End.X; x++)
		{
			Vector2I tilePos = new Vector2I(x, y);
			int sourceId = Floor.GetCellSourceId(tilePos);
			
			// 'F' if a floor tile exists, '.' if the cell is empty (-1)
			rowStr += (sourceId != -1) ? "F " : ". ";
		}
		GD.Print($"[Y:{y,3}] {rowStr}");
	}
	GD.Print("==========================================================");
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
