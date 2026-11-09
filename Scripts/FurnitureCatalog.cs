using Godot;
using System.Collections.Generic;
using System.IO;

public partial class FurnitureCatalog : Node
{
	[Export(PropertyHint.Dir)]
	public string FurnitureFolder = "res://Assets/Furniture";

	public Dictionary<string, string> Furniture = new();

	public override void _EnterTree()
	{
		Furniture.Clear();
		LoadFurniture(FurnitureFolder);
		GD.Print($"Loaded {Furniture.Count} furniture sprites.");
	}

	private void LoadFurniture(string folder)
	{
		using var directory = DirAccess.Open(folder);

		if (directory == null)
		{
			GD.PushError($"Cannot open furniture folder: {folder}");
			return;
		}

		foreach (string file in directory.GetFiles())
		{
			if (!file.EndsWith(".png",
				System.StringComparison.OrdinalIgnoreCase))
				continue;

			string id = Path.GetFileNameWithoutExtension(file);
			string path = folder.PathJoin(file);

			Furniture[id] = path;
		}

		foreach (string subfolder in directory.GetDirectories())
		{
			LoadFurniture(folder.PathJoin(subfolder));
		}
	}

	public Texture2D GetFurnitureTexture(string id)
	{
		if (!Furniture.TryGetValue(id, out string path))
			return null;

		return GD.Load<Texture2D>(path);
	}
}
