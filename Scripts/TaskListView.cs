using Godot;
using System;
using System.Linq;
namespace Cozytasks.Scripts;
public partial class TaskListView : Control
{
	[Export] private PackedScene _taskRowScene = null!;
	
	// Drag your VBoxContainer node here in the Godot Inspector
	[Export] private VBoxContainer _listContainer = null!;

	private DateTime _selectedDate = DateTime.Today;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Subscribe to TaskManager updates
		if (TaskManager.Instance != null)
		{
			TaskManager.Instance.TasksUpdated += RefreshList;
		}

		RefreshList();
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void SetDate(DateTime date)
	{
		_selectedDate = date.Date;
		RefreshList();
	}

	public void RefreshList()
	{
		foreach (Node child in _listContainer.GetChildren())
		{
			child.QueueFree();
		}

		if (TaskManager.Instance == null) return;

		var definitions = TaskManager.Instance.GetAllDefinitions();

		foreach (var kvp in definitions)
		{
			var def = kvp.Value;
			GD.Print($"Instantiating row for task definition: {def.Title}");
			
			TaskRow row = _taskRowScene.Instantiate<TaskRow>();
			_listContainer.AddChild(row);
			row.Setup(def);
		}
	}
	public override void _ExitTree()
	{
		if (TaskManager.Instance != null)
		{
			TaskManager.Instance.TasksUpdated -= RefreshList;
		}
	}
}
