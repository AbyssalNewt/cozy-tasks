using Godot;
using System.Collections.Generic;
using System.IO;

public partial class FurnitureCatalog : Node
{
	[Export(PropertyHint.Dir)]
	public string FurnitureFolder = "res://Assets/Furniture";

	public class CatalogCategory
	{
		public string Name;
		public Dictionary<string, string> Items = new();
		public Dictionary<string, CatalogCategory> SubCategories = new();
	}

	public CatalogCategory Categories = new() { Name = "Furniture" };
	public Dictionary<string, string> Furniture = new();
	public override void _EnterTree()
	{
		Categories = new() { Name = "Furniture" };
		Furniture.Clear();
		LoadFurniture(FurnitureFolder, Categories);
		GD.Print($"Loaded {Furniture.Count} furniture sprites.");
	}

	private void LoadFurniture(string folder, CatalogCategory currentCategory)
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


			currentCategory.Items[id] = path;
			Furniture[id] = path;
		}

		foreach (string subfolder in directory.GetDirectories())
		{
			
			CatalogCategory subCat = new(){ Name = subfolder };
			currentCategory.SubCategories[subfolder] = subCat;

			LoadFurniture(folder.PathJoin(subfolder), subCat);
		}
	}

	public Texture2D GetFurnitureTexture(string id)
	{
		if (!Furniture.TryGetValue(id, out string path))
			return null;

		return GD.Load<Texture2D>(path);
	}
}
