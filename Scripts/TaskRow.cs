using Godot;
using System;
namespace Cozytasks.Scripts;
public partial class TaskRow : PanelContainer
{
	[Export] private CheckBox _completedCheckBox = null!;
	[Export] private Label _titleLabel = null!;
	[Export] private Label _timeLabel = null!;
	[Export] private Label _descriptionLabel = null!;
	[Export] private Label _detailsLabel = null!;

	private Guid _definitionId;
	private DateTime _scheduledDateTime;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_completedCheckBox.Toggled += OnCheckBoxToggled;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Setup(TaskDefinition definition)
	{
		_definitionId = definition.Id;
		_titleLabel.Text = definition.Title;
		_descriptionLabel.Text = definition.Description;

		string scheduleType = definition.Rule != null? $"Scheduled ({definition.Rule.Pattern})" : "Unscheduled Task";
		_detailsLabel.Text = $"{scheduleType} | Duration: {definition.Duration.TotalMinutes}m | Reward: {definition.Reward}";
	}
	public void Setup(TaskInstanceViewModel viewModel)
	{
		_definitionId = viewModel.DefinitionId;
		_scheduledDateTime = viewModel.ScheduledDateTime;

		_titleLabel.Text = viewModel.Title;
		_timeLabel.Text = viewModel.ScheduledDateTime.ToString("hh:mm tt");

		_descriptionLabel.Text = viewModel.Description;

		string scheduleType = viewModel.Rule != null? $"Scheduled ({viewModel.Rule.Pattern})" : "Unscheduled Task";
		_detailsLabel.Text = $"{scheduleType} | Duration: {viewModel.Duration.TotalMinutes}m | Reward: {viewModel.Reward}";

		_completedCheckBox.SetPressedNoSignal(viewModel.IsCompleted);
	}

	private void OnCheckBoxToggled(bool toggledOn)
	{
		TaskManager.Instance.SetTaskCompleted(_definitionId, _scheduledDateTime);
	}

	public override void _ExitTree()
	{
		_completedCheckBox.Toggled -= OnCheckBoxToggled;
	}
}
