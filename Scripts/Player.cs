using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 150.0f;

	//private AnimatedSprite2D _sprite;
	
	private AppearanceController _appearance;

	// Direction the player continues facing after they stop moving.
	private string _facingDirection = "down";

	/*public override void _Ready()
	{
		_appearance = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		_appearance.SpriteFrames =
			AnimationLoader.CreateAnimations(
				"res://moderninteriors-win/2_Characters/Character_Generator/Bodies/48x48/Body_48x48_01.png"
			);

		_appearance.Play("stand_down");
	}
	*/
	public override void _Ready()
	{
		_appearance =
			GetNodeOrNull<AppearanceController>("Appearance");

		if (_appearance == null)
		{
			GD.PrintErr(
	            "Player could not find AppearanceController on the Appearance node!"
			);

			return;
		}

		GD.Print("AppearanceController found!");

		_appearance.Play("stand_down");
	}
	
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
            "move_down"
		);

		Velocity = direction * Speed;

		UpdateAnimation(direction);

		MoveAndSlide();
	}

	private void UpdateAnimation(Vector2 direction)
	{
		// Player stopped moving.
		if (direction == Vector2.Zero)
		{
			_appearance.Play($"stand_{_facingDirection}");
			return;
		}

		// Determine which direction the character should face.
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			if (direction.X > 0)
				_facingDirection = "right";
			else
				_facingDirection = "left";
		}
		else
		{
			if (direction.Y > 0)
				_facingDirection = "down";
			else
				_facingDirection = "up";
		}

		_appearance.Play($"walk_{_facingDirection}");
	}
}
