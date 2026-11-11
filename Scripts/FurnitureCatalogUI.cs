using Godot;
using System.Collections.Generic;

public partial class FurnitureCatalogUI : Control
{
	[Export] 
	public FurnitureCatalog Catalog { get; set; }

	[Export] 
	public TabContainer MainTabContainer { get; set; }

	[Signal]
	public delegate void FurnitureSelectedEventHandler(string furnitureId);

	public override void _Ready()
	{
		if (Catalog == null || MainTabContainer == null)
		{
			GD.PushError("Catalog or MainTabContainer reference is missing!");
			return;
		}
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		
		PopulateCatalogUI();
	}

	private void PopulateCatalogUI()
	{
		foreach (Node child in MainTabContainer.GetChildren())
		{
			child.QueueFree();
		}

		// Build main tabs from root subfolders/categories
		foreach (var kvp in Catalog.Categories.SubCategories)
		{
			string categoryName = kvp.Key;
			var categoryData = kvp.Value;

			Control tabContent = BuildCategoryControl(categoryData);
			tabContent.Name = categoryName[..categoryName.IndexOf('_')];
			MainTabContainer.AddChild(tabContent);
		}
	}

	private Control BuildCategoryControl(FurnitureCatalog.CatalogCategory category)
	{
		// If this category has sub-categories, create a nested TabContainer for them
		if (category.SubCategories.Count > 0)
		{
			var nestedTabs = new TabContainer
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = Control.SizeFlags.ExpandFill
			};

			// First, if this folder also has direct items, put them in an "All" or "General" tab inside the nested container
			if (category.Items.Count > 0)
			{
				var directItemsScroll = BuildTabContent(category.Name, category.Items);
				directItemsScroll.Name = "General";
				nestedTabs.AddChild(directItemsScroll);
			}

			// Then add tabs for each sub-category
			foreach (var subKvp in category.SubCategories)
			{
				string subName = subKvp.Key;
				var subData = subKvp.Value;

				Control subControl = BuildCategoryControl(subData);
				subControl.Name = subName;
				nestedTabs.AddChild(subControl);
			}

			return nestedTabs;
		}
		else
		{
			return BuildTabContent(category.Name, category.Items);
		}
	}

	private ScrollContainer BuildTabContent(string category, Dictionary<string, string> items)
	{
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };

        var container = new HFlowContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		container.AddThemeConstantOverride("h_separation", 10);
		container.AddThemeConstantOverride("v_separation", 10);

		scroll.AddChild(container);

		foreach (var itemKvp in items)
		{
			string id = itemKvp.Key;
			Texture2D texture = Catalog.GetFurnitureTexture(id);

			var button = new Button
			{
				CustomMinimumSize = new Vector2(90, 90),
				Text = category + id[id.LastIndexOf('_')..],
				Icon = texture,
				ExpandIcon = true,
				IconAlignment = HorizontalAlignment.Center,
				VerticalIconAlignment = VerticalAlignment.Top
			};

			button.Pressed += () => OnFurnitureClicked(id);
			container.AddChild(button);
		}

		return scroll;
	}

	private void OnFurnitureClicked(string id)
	{
		GD.Print($"Picked furniture item: {id}");
		EmitSignal(SignalName.FurnitureSelected, id);
	}
}
