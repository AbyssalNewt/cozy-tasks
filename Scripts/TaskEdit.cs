using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Cozytasks.Scripts;
public partial class TaskEdit : PanelContainer
{
	[Export] private LineEdit _titleLineEdit = null!;
	[Export] private TextEdit _descTextEdit = null!;
	[Export] private SpinBox _durationSpinBox = null!;
	[Export] private CheckBox _isScheduledCheckBox = null!;
	[Export] private OptionButton _patternOptionButton = null!;
	[Export] private Button _deleteButton = null!;
	[Export] private Button _saveButton = null!;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_patternOptionButton.Clear();
		_patternOptionButton.AddItem("None", (int)RecurrencePattern.None);
		_patternOptionButton.AddItem("Daily", (int)RecurrencePattern.Daily);
		_patternOptionButton.AddItem("Weekly", (int)RecurrencePattern.Weekly);

		_deleteButton.Pressed += OnDeletePressed;
		_saveButton.Pressed += OnSavePressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void OnDeletePressed()
	{
	}
	private void OnSavePressed()
	{
		string title = _titleLineEdit.Text.Trim();
		if (string.IsNullOrEmpty(title))
		{
			GD.PrintErr("Task title cannot be empty!");
			return;
		}

		string description = _descTextEdit.Text;
		TimeSpan duration = TimeSpan.FromMinutes(_durationSpinBox.Value);
		bool isScheduled = _isScheduledCheckBox.ButtonPressed;
		
		RecurrencePattern pattern = (RecurrencePattern)_patternOptionButton.GetSelectedId();

		// Build default times list (e.g., defaulting to 9:00 AM if scheduled)
		List<TimeSpan> times = isScheduled ? [new TimeSpan(9, 0, 0)] : [];

		// Call your teammate's TaskManager to create the task
		TaskManager.Instance.CreateTask(
			title: title,
			description: description,
			icon: "default_icon",
			times: times,
			duration: duration,
			isScheduled: isScheduled,
			pattern: pattern,
			startDate: DateTime.Today,
			weekdays: isScheduled ? [DateTime.Today.DayOfWeek] : null
		);

		GD.Print($"Successfully created task: {title}");
		ClearForm();
	}

	private void ClearForm()
	{
		_titleLineEdit.Clear();
		_descTextEdit.Text = string.Empty;
		_durationSpinBox.Value = 30;
		_isScheduledCheckBox.ButtonPressed = false;
		_patternOptionButton.Select(0);
	}

	public override void _ExitTree()
	{
		_saveButton.Pressed -= OnSavePressed;
	}
}
