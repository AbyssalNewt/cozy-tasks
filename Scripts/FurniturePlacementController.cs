
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FurniturePlacementController : Node2D
{
	[Export] public FurnitureCatalog Catalog;
	[Export] public PlacementManager Placement;
	[Export] public Node2D FurnitureContainer;
	[Export] public PackedScene FurnitureScene;

	private readonly List<Furniture> placedFurniture = new();

	private List<string> furnitureIds = new();
	private int selectedIndex = -1;

	private Furniture preview;
	private bool movingExisting = false;

	private Vector2I previousCell;
	private Vector2 previousPosition;

	public override void _Ready()
	{
		furnitureIds = Catalog.Furniture.Keys
			.OrderBy(id => id)
			.ToList();

		GD.Print($"Available furniture: {furnitureIds.Count}");
		GD.Print("F: select furniture | N: next | Left click: place/move | Right click: cancel");
	}

	public override void _Process(double delta)
	{
		if (preview == null || Placement == null || Placement.Floor == null || Placement.Walls == null)
			return;

		Vector2 mousePosition = Placement.Floor.ToLocal(GetGlobalMousePosition());
		Vector2I cell = Placement.GetGridPosition(mousePosition);

		preview.OriginCell = cell;
		preview.GlobalPosition = Placement.GetWorldPosition(cell);

		bool valid = Placement.CanPlace(
			cell,
			preview.PlacementSize
		);

		preview.SetPreview(valid);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key &&
			key.Pressed &&
			!key.Echo)
		{
			switch (key.Keycode)
			{
				case Key.F:
					CancelPlacement();
					StartNewFurniture(0);
					break;

				case Key.N:
					CancelPlacement();
					StartNewFurniture(1);
					break;

				case Key.Escape:
					CancelPlacement();
					break;
			}
		}

		if (@event is InputEventMouseButton mouse &&
			mouse.Pressed)
		{
			if (mouse.ButtonIndex == MouseButton.Left)
			{
				if (preview != null)
					ConfirmPlacement();
				else
					SelectExistingFurniture();
			}

			if (mouse.ButtonIndex == MouseButton.Right)
				CancelPlacement();
		}
	}

	private void StartNewFurniture(int direction)
	{
		if (furnitureIds.Count == 0)
		{
			GD.PrintErr("No furniture found in catalog.");
			return;
		}

		CancelPlacement();

		if (direction == 0)
		{
			if (selectedIndex < 0)
				selectedIndex = 0;
		}
		else
		{
			selectedIndex =
				(selectedIndex + direction + furnitureIds.Count)
				% furnitureIds.Count;
		}

		string id = furnitureIds[selectedIndex];
		Texture2D texture = Catalog.GetFurnitureTexture(id);

		if (texture == null)
		{
			GD.PrintErr($"Could not load furniture: {id}");
			return;
		}

		preview = FurnitureScene.Instantiate<Furniture>();
		FurnitureContainer.AddChild(preview);

		Vector2I tileSize = Placement.Floor.TileSet.TileSize;

		preview.Setup(id, texture, tileSize / 3);
		preview.SetPreview(false);

		GD.Print($"Selected: {id}");
	}

	private Vector2 GetCellCorner(Vector2I cell)
	{
		TileMapLayer floor = Placement.Floor;

		Vector2 localCenter = floor.MapToLocal(cell);
		Vector2 halfTile = new Vector2(
			floor.TileSet.TileSize.X / 2f,
			floor.TileSet.TileSize.Y / 2f
		);

		return floor.ToGlobal(localCenter - halfTile);
	}

	private void ConfirmPlacement()
	{
		if (preview == null)
			return;

		GD.Print(preview.OriginCell);
		GD.Print(preview.PlacementSize);
		GD.Print(Placement.ToTileCell(preview.OriginCell));
		GD.Print(Placement.ToTileCell(preview.OriginCell+preview.PlacementSize));
		GD.Print(Placement.Floor.GetCellSourceId(Placement.ToTileCell(preview.OriginCell)));

		if (!Placement.CanPlace(
			preview.OriginCell,
			preview.PlacementSize))
		{
			GD.Print("Invalid placement.");
			return;
		}

		Placement.OccupyCells(
			preview.OriginCell,
			preview.PlacementSize
		);

		preview.SetPlaced();

		if (!movingExisting)
			placedFurniture.Add(preview);

		preview = null;
		movingExisting = false;

		GD.Print("Furniture placed.");
	}

	private void SelectExistingFurniture()
	{
		Vector2 mousePosition = GetGlobalMousePosition();

		// Check in reverse order for visually overlapping objects.
		for (int i = placedFurniture.Count - 1; i >= 0; i--)
		{
			Furniture item = placedFurniture[i];

			if (!item.ContainsPoint(mousePosition))
				continue;

			preview = item;
			movingExisting = true;

			previousCell = item.OriginCell;
			previousPosition = item.GlobalPosition;

			Placement.FreeCells(
				item.OriginCell,
				item.PlacementSize
			);

			preview.SetPreview(true);
			return;
		}
	}

	private void CancelPlacement()
	{
		if (preview == null)
			return;

		if (movingExisting)
		{
			preview.OriginCell = previousCell;
			preview.GlobalPosition = previousPosition;

			Placement.OccupyCells(
				previousCell,
				preview.PlacementSize
			);

			preview.SetPlaced();
		}
		else
		{
			preview.QueueFree();
		}

		preview = null;
		movingExisting = false;
	}
}
