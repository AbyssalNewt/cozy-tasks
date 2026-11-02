using Godot;

public partial class AppearanceController : Node2D
{
	private AnimatedSprite2D _body;
	private AnimatedSprite2D _eyes;
	private AnimatedSprite2D _outfit;
	private AnimatedSprite2D _hairstyle;

	[Export(PropertyHint.File, "*.png")]
	public string BodyPath { get; set; }

	[Export(PropertyHint.File, "*.png")]
	public string EyesPath { get; set; }

	[Export(PropertyHint.File, "*.png")]
	public string OutfitPath { get; set; }

	[Export(PropertyHint.File, "*.png")]
	public string HairstylePath { get; set; }

	public override void _Ready()
	{
		_body = GetNode<AnimatedSprite2D>("Body");
		_eyes = GetNode<AnimatedSprite2D>("Eyes");
		_outfit = GetNode<AnimatedSprite2D>("Outfit");
		_hairstyle = GetNode<AnimatedSprite2D>("Hairstyle");

		LoadAppearance();
	}

	private void LoadAppearance()
	{
		if (!string.IsNullOrEmpty(BodyPath))
			_body.SpriteFrames = AnimationLoader.CreateAnimations(BodyPath);

		if (!string.IsNullOrEmpty(EyesPath))
			_eyes.SpriteFrames = AnimationLoader.CreateAnimations(EyesPath);

		if (!string.IsNullOrEmpty(OutfitPath))
			_outfit.SpriteFrames = AnimationLoader.CreateAnimations(OutfitPath);

		if (!string.IsNullOrEmpty(HairstylePath))
			_hairstyle.SpriteFrames = AnimationLoader.CreateAnimations(HairstylePath);
	}

	public void Play(string animationName)
	{
		PlayLayer(_body, animationName);
		PlayLayer(_eyes, animationName);
		PlayLayer(_outfit, animationName);
		PlayLayer(_hairstyle, animationName);
	}

	private void PlayLayer(
		AnimatedSprite2D layer,
		string animationName)
	{
		if (layer.SpriteFrames == null)
			return;

		if (!layer.SpriteFrames.HasAnimation(animationName))
			return;

		layer.Play(animationName);
	}
}
