
using System;
using Godot;

public partial class Furniture : Node2D
{
	private Sprite2D sprite;
	private StaticBody2D body;

	public string FurnitureId { get; private set; } = "";
	public Vector2I PlacementSize { get; private set; }
	public Vector2I OriginCell { get; set; }
	public Interaction CurrentInteraction { get; private set; } = Interaction.NONE;	
	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		body = GetNode<StaticBody2D>("StaticBody2D");
	}

	public void Setup(
		string id,
		Texture2D texture,
		Vector2I tileSize,
		Interaction interaction)
	{
		YSortEnabled = true;
		FurnitureId = id;
		PlacementSize = new(Mathf.RoundToInt(texture.GetWidth() / 16f), 
			Mathf.RoundToInt(Mathf.Clamp(texture.GetHeight() / 2f, 16f, 48f) / 16f));

		sprite.Texture = texture;
		sprite.Centered = false;

		sprite.Position = new Vector2(
			0f,
			-PlacementSize.Y * 16 // - texture.GetHeight()
		);
		CurrentInteraction = interaction;
		GenerateCollision(texture);
	}

	private static Vector2I GetBottomLeftPixel(Image image)
	{
		if (image == null || image.IsEmpty())
		{
			return Vector2I.Zero;
		}
		int width = image.GetWidth();
		int height = image.GetHeight();

		for (int y = height - 1; y >= 0; y--)
		{
			for (int x = 0; x < width; x++)
			{
				if (image.GetPixel(x, y).A > 0.1f)
				{
					return new Vector2I(x, y);
				}
			}
		}
		return Vector2I.Zero;
	}
	private void GenerateCollision(Texture2D texture)
	{
		// Clear previously generated polygons.
		foreach (Node child in body.GetChildren())
		{
			if (child is CollisionPolygon2D)
			{
				body.RemoveChild(child);
				child.QueueFree();
			}
		}

		float footprintWidth = PlacementSize.X * 16; 
		float footprintHeight = PlacementSize.Y * 16;

		// Create a clean rectangular collision polygon matching the footprint bounds
		CollisionPolygon2D collision = new CollisionPolygon2D();
		collision.BuildMode = CollisionPolygon2D.BuildModeEnum.Solids;

		collision.Polygon =
		[
			new(0, 0),
			new(footprintWidth, 0),
			new(footprintWidth, footprintHeight),
			new(0, footprintHeight)
		];

		collision.Position = new(0, -footprintHeight / 2);

		body.AddChild(collision);

		GD.Print($"Generated footprint collision box for {FurnitureId}");
	}

	public void SetPreview(bool valid)
	{
		sprite.Modulate = valid
			? new Color(0.5f, 1f, 0.5f, 0.7f)
			: new Color(1f, 0.4f, 0.4f, 0.7f);

		// Disable collision while moving furniture.
		body.CollisionLayer = 0;

		YSortEnabled = false; // Temporarily stop Y-sorting calculations
		ZIndex = 2;         // Push to a high Z layer so it draws above everything
	}

	public void SetPlaced()
	{
		sprite.Modulate = Colors.White;

		// Re-enable player collision.
		body.CollisionLayer = 1;

		ZIndex = 0;           // Reset to default drawing layer
		YSortEnabled = true;  // Re-enable normal Y-sorting against the player
	}

	public bool ContainsPoint(Vector2 point)
	{
		if (sprite.Texture == null)
			return false;

		Vector2 localPoint = sprite.ToLocal(point);

		return sprite.GetRect().HasPoint(localPoint);
	}
}
