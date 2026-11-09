
using Godot;

public partial class Furniture : Node2D
{
	private Sprite2D sprite;
	private StaticBody2D body;

	public string FurnitureId { get; private set; } = "";
	public Vector2I PlacementSize { get; private set; }
	public Vector2I OriginCell { get; set; }

	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		body = GetNode<StaticBody2D>("StaticBody2D");
	}

	public void Setup(
		string id,
		Texture2D texture,
		Vector2I gridSize,
		Vector2I tileSize)
	{
		FurnitureId = id;
		PlacementSize = gridSize;

		Vector2 footprintSize = new Vector2(
			gridSize.X * tileSize.X,
			gridSize.Y * tileSize.Y
		);

		sprite.Texture = texture;
		sprite.Centered = false;

		// Align the bottom-center of the sprite
		// with the bottom-center of the footprint.
		sprite.Position = new Vector2(
			(footprintSize.X - texture.GetWidth()) / 2f
				- tileSize.X / 2f,
			footprintSize.Y - texture.GetHeight()
				- tileSize.Y / 2f
		);

		GenerateCollision(texture);
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

		Image image = texture.GetImage();

		if (image == null || image.IsEmpty())
		{
			GD.PushError(
				$"Could not read image for {FurnitureId}"
			);
			return;
		}

		// Create a mask from the image's alpha channel.
		using Bitmap bitmap = new Bitmap();

		bitmap.CreateFromImageAlpha(image, 0.1f);

		Rect2I rect = new Rect2I(
			0, 0,
			image.GetWidth(),
			image.GetHeight()
		);

		// Convert opaque regions into polygons.
		var polygons = bitmap.OpaqueToPolygons(rect, 1.0f);

		foreach (var points in polygons)
		{
			if (points.Length < 3)
				continue;

			CollisionPolygon2D collision =
				new CollisionPolygon2D();

			collision.BuildMode =
				CollisionPolygon2D.BuildModeEnum.Solids;

			collision.Polygon = points;

			// Match the sprite's actual drawing position.
			collision.Position = sprite.Position;

			body.AddChild(collision);
		}

		GD.Print(
			$"Generated {polygons.Count} polygons for {FurnitureId}"
		);
	}

	public void SetPreview(bool valid)
	{
		sprite.Modulate = valid
			? new Color(0.5f, 1f, 0.5f, 0.7f)
			: new Color(1f, 0.4f, 0.4f, 0.7f);

		// Disable collision while moving furniture.
		body.CollisionLayer = 0;
	}

	public void SetPlaced()
	{
		sprite.Modulate = Colors.White;

		// Re-enable player collision.
		body.CollisionLayer = 1;
	}

	public bool ContainsPoint(Vector2 point)
	{
		if (sprite.Texture == null)
			return false;

		Vector2 localPoint = sprite.ToLocal(point);

		return sprite.GetRect().HasPoint(localPoint);
	}
}
