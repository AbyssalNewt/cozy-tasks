using Godot;

// Sprite sheet layout:
//
// Orientation order for normal animations:
// Right, Back, Left, Down
//
// Row 0  - Standing
// Row 1  - Idle (6 frames)
// Row 2  - Walk (6 frames)
// Row 3  - Sleep (6 frames)
// Row 4  - Sitting with legs (Right, Left) (6 frames)
// Row 5  - Sitting without legs (Right, Left) (6 frames)
// Row 6  - Phone (12 frames, loop frames 4-9)
// Row 7  - Book (12 frames, loop frames 1-6)
// Row 8  - Push cart (6 frames), Cart (3 frames at end)
// Row 9  - Pick up (12 frames)
// Row 10 - Gift (10 frames)
// Row 11 - Lift (14 frames)
// Row 12 - Throw (14 frames)
// Rows 13-18 - Combat / currently unused

public static class AnimationLoader
{
	private const int FrameWidth = 48;
	private const int FrameHeight = 96;

	private static readonly string[] Directions =
	{
		"right",
		"up",       // "back" on the sprite sheet
		"left",
        "down"
	};

	private static readonly AnimationInfo[] Animations =
	{
		new("stand",      0,  1, true),
		new("idle",       1,  6, true),
		new("walk",       2,  6, true),
		new("sleep",      3,  6, true),

		// Sitting animations are handled separately.

		new("phone",      6, 12, false),
		new("book",       7, 12, false),
		new("push_cart",  8,  6, true),
		new("pickup",     9, 12, false),
		new("gift",      10, 10, false),
		new("lift",      11, 14, false),
		new("throw",     12, 14, false)
	};

	public static SpriteFrames CreateAnimations(string texturePath)
{
	GD.Print("Entered CreateAnimations");
	GD.Print($"Trying to load: {texturePath}");

	Texture2D texture = GD.Load<Texture2D>(texturePath);

	GD.Print("GD.Load finished");

	if (texture == null)
	{
		GD.PrintErr("ERROR: Texture is null!");
		return new SpriteFrames();
	}

	GD.Print(
		$"Texture loaded successfully: {texture.GetWidth()} x {texture.GetHeight()}"
	);

	SpriteFrames spriteFrames = new SpriteFrames();

	GD.Print("SpriteFrames created");

	if (spriteFrames.HasAnimation("default"))
		spriteFrames.RemoveAnimation("default");

	GD.Print("Beginning animation creation");

	foreach (AnimationInfo animation in Animations)
	{
		GD.Print(
			$"Creating {animation.Name} - row {animation.Row}"
		);

		for (int direction = 0; direction < 4; direction++)
		{
			string animationName =
				$"{animation.Name}_{Directions[direction]}";

			GD.Print($"  Creating {animationName}");

			AddAnimation(
				spriteFrames,
				texture,
				animationName,
				animation.Row,
				direction,
				animation.FrameCount,
				animation.Fps,
				animation.Loop
			);
		}
	}

	GD.Print("Normal animations finished");

	AddSittingAnimations(spriteFrames, texture);
	GD.Print("Sitting animations finished");

	AddPhoneLoops(spriteFrames, texture);
	GD.Print("Phone animations finished");

	AddBookLoops(spriteFrames, texture);
	GD.Print("Book animations finished");

	AddCartAnimation(spriteFrames, texture);
	GD.Print("Cart animation finished");

	GD.Print("CreateAnimations COMPLETE");

	return spriteFrames;
}

	private static void AddAnimation(
		SpriteFrames spriteFrames,
		Texture2D texture,
		string animationName,
		int row,
		int direction,
		int frameCount,
		double fps,
		bool loop)
	{
		spriteFrames.AddAnimation(animationName);
		spriteFrames.SetAnimationSpeed(animationName, fps);
		spriteFrames.SetAnimationLoop(animationName, loop);

		int startingColumn = direction * frameCount;

		for (int frame = 0; frame < frameCount; frame++)
		{
			int column = startingColumn + frame;

			AddFrame(
				spriteFrames,
				texture,
				animationName,
				row,
				column
			);
		}
	}

	private static void AddSittingAnimations(
		SpriteFrames spriteFrames,
		Texture2D texture)
	{
		// Row 4: sitting with legs
		// Right = columns 0-5
		// Left  = columns 6-11
		AddTwoDirectionAnimation(
			spriteFrames,
			texture,
			"sit_legs",
			4,
			6
		);

		// Row 5: sitting without legs
		AddTwoDirectionAnimation(
			spriteFrames,
			texture,
			"sit",
			5,
			6
		);
	}

	private static void AddTwoDirectionAnimation(
		SpriteFrames spriteFrames,
		Texture2D texture,
		string name,
		int row,
		int frameCount)
	{
		string[] directions =
		{
			"right",
            "left"
		};

		for (int direction = 0; direction < 2; direction++)
		{
			string animationName =
				$"{name}_{directions[direction]}";

			spriteFrames.AddAnimation(animationName);
			spriteFrames.SetAnimationSpeed(animationName, 10);
			spriteFrames.SetAnimationLoop(animationName, true);

			int startingColumn = direction * frameCount;

			for (int frame = 0; frame < frameCount; frame++)
			{
				AddFrame(
					spriteFrames,
					texture,
					animationName,
					row,
					startingColumn + frame
				);
			}
		}
	}

	private static void AddPhoneLoops(
		SpriteFrames spriteFrames,
		Texture2D texture)
	{
		// Documentation says frames 4-9 loop.
		//
		// Assuming the documentation numbers frames starting at 1,
		// that means indexes 3-8 in C#.

		for (int direction = 0; direction < 4; direction++)
		{
			string animationName =
				$"phone_loop_{Directions[direction]}";

			spriteFrames.AddAnimation(animationName);
			spriteFrames.SetAnimationSpeed(animationName, 10);
			spriteFrames.SetAnimationLoop(animationName, true);

			int directionStart = direction * 12;

			for (int frame = 3; frame <= 8; frame++)
			{
				AddFrame(
					spriteFrames,
					texture,
					animationName,
					6,
					directionStart + frame
				);
			}
		}
	}

	private static void AddBookLoops(
		SpriteFrames spriteFrames,
		Texture2D texture)
	{
		// Documentation says frames 1-6 loop.
		//
		// Assuming frame numbering starts at 1,
		// these are indexes 0-5.

		for (int direction = 0; direction < 4; direction++)
		{
			string animationName =
				$"book_loop_{Directions[direction]}";

			spriteFrames.AddAnimation(animationName);
			spriteFrames.SetAnimationSpeed(animationName, 10);
			spriteFrames.SetAnimationLoop(animationName, true);

			int directionStart = direction * 12;

			for (int frame = 0; frame <= 5; frame++)
			{
				AddFrame(
					spriteFrames,
					texture,
					animationName,
					7,
					directionStart + frame
				);
			}
		}
	}

	private static void AddCartAnimation(
		SpriteFrames spriteFrames,
		Texture2D texture)
	{
		const int row = 8;

		// Push animation:
		// 6 frames × 4 directions = 24 cells.
		//
		// Cart animation starts immediately after those.
		const int startColumn = 24;
		const int frameCount = 3;

		spriteFrames.AddAnimation("cart");
		spriteFrames.SetAnimationSpeed("cart", 10);
		spriteFrames.SetAnimationLoop("cart", true);

		for (int frame = 0; frame < frameCount; frame++)
		{
			AddFrame(
				spriteFrames,
				texture,
				"cart",
				row,
				startColumn + frame
			);
		}
	}

	private static void AddFrame(
	SpriteFrames spriteFrames,
	Texture2D texture,
	string animationName,
	int row,
	int column)
	{
		AtlasTexture frameTexture = new AtlasTexture
		{
			Atlas = texture,

			Region = new Rect2(
				column * FrameWidth,
				row * FrameHeight,
				FrameWidth,
				FrameHeight
			)
		};

		spriteFrames.AddFrame(animationName, frameTexture);
	}
}

public class AnimationInfo
{
	public string Name { get; }
	public int Row { get; }
	public int FrameCount { get; }
	public bool Loop { get; }
	public double Fps { get; }

	public AnimationInfo(
		string name,
		int row,
		int frameCount,
		bool loop,
		double fps = 10)
	{
		Name = name;
		Row = row;
		FrameCount = frameCount;
		Loop = loop;
		Fps = fps;
	}
}
