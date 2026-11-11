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
	private static readonly Dictionary<string, Interaction> InteractionRegistry = new()
	{
		{ "BedsFront", Interaction.SLEEP },
		{ "BedsL", Interaction.SLEEP },
		{ "BedsR", Interaction.SLEEP },
		{ "BunkBed", Interaction.SLEEP },
		{ "DoubleBedsFront", Interaction.SLEEP },
		{ "Sink", Interaction.WASH_SINK },
		{ "Grill", Interaction.COOK },
		{ "Stove", Interaction.COOK },
		{ "Plates", Interaction.EAT },
		{ "Washing Machine", Interaction.WASH_MACHINE }		
	};

	public Interaction GetInteraction(string id)
	{
		if (InteractionRegistry.TryGetValue(id[..id.LastIndexOf('_')], out var interaction))
		{
			return interaction;
		}
		return Interaction.NONE;
	}
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

			string fileName = Path.GetFileNameWithoutExtension(file);  
			string id = currentCategory.Name + fileName[fileName.LastIndexOf('_')..];
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
