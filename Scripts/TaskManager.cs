//CONTAINS TaskManager, TaskInstanceViewModel, TaskInstanceState
#nullable enable
using Godot;
using System;
using System.Collections.Generic;

namespace Cozytasks.Scripts;

public partial class TaskManager : Node
{
	public static TaskManager Instance { get; private set; }

	// Godot signal to notify UI controls when task data changes
	[Signal]
	public delegate void TasksUpdatedEventHandler();

	public override void _Ready()
	{
		Instance = this;
	}
	
	
	private Dictionary<Guid, TaskDefinition> _definitions = new();
	
	/// <summary>
	/// Persistent state store (serialized to JSON?)
	/// </summary>
	private Dictionary<(Guid DefinitionId, DateTime Date), TaskInstanceState> _instanceStates = new();

	/// <summary>
	/// Creates a task definition and emits TasksUpdated.
	/// </summary>
	/// <param name="title"></param>
	/// <param name="description"></param>
	/// <param name="icon"></param>
	/// <param name="times"></param>
	/// <param name="duration"></param>
	/// <param name="isScheduled"></param>
	/// <param name="pattern"></param>
	/// <param name="startDate"></param>
	/// <param name="endDate"></param>
	/// <param name="weekdays"></param>
	public void CreateTask(string title,  string description, string icon, List<TimeSpan> times, 
		TimeSpan duration, bool isScheduled, RecurrencePattern pattern = RecurrencePattern.None, 
		DateTime? startDate = null, DateTime? endDate = null, List<DayOfWeek>? weekdays = null)
	{
		TaskDefinition newTask = new TaskDefinition(title, description, icon, times, duration, 
			isScheduled, pattern, startDate,  endDate, weekdays);
		_definitions.Add(newTask.Id, newTask);
		
		//Need async database state changing to happen here
		
		EmitSignal(SignalName.TasksUpdated);
	}
	
	/// <summary>
	///Creates a new TaskInstanceState (if necessary) for a given task instance and writes it to the instance dict
	/// </summary>
	public void SetTaskCompleted(Guid definitionId, DateTime date)
	{
		var key = (definitionId, date.Date);
		if (!_instanceStates.TryGetValue(key, out TaskInstanceState value))
		{
			value = new TaskInstanceState();
			_instanceStates[key] = value;
		}

		value.IsCompleted = !(value.IsCompleted);
		EmitSignal(SignalName.TasksUpdated);
	}
	
	/// <summary>
	///Dynamically generates tasks
	/// </summary> 
	public List<TaskInstanceViewModel> GetMonthViewInstances(DateTime monthStart, DateTime monthEnd)
	{
		var viewModels = new List<TaskInstanceViewModel>();
		
		foreach (var def in _definitions.Values)
		{
			// 1. Evaluate rule math & exceptions (Virtual Projections)
			if (def.Rule == null) continue;
			foreach (var ruleDate in def.Rule.GetOccurrences(monthStart, monthEnd))
			{
				foreach (var time in def.Rule.Times)
				{
					var taskTime = ruleDate + time;
					if (def.Rule.RescheduledDates.ContainsKey(taskTime)) continue;
					if (def.Rule.ExcludedDates.Contains(taskTime)) continue;

					viewModels.Add(CreateViewModel(def, taskTime));
				}
			}

			// 2. Inject rescheduled instances
			foreach (var (originalDate, newDate) in def.Rule.RescheduledDates)
			{
				//ensure only view models for the current month are injected
				if (newDate >= monthStart && newDate <= monthEnd)
				{
					viewModels.Add(CreateViewModel(def, newDate, isRescheduled: true));
				}
			}
		}

		return viewModels;
	}

	/// <summary>
	/// Creates view model for a task instance. Checks if a persisted record of an altered task exists to inject
	/// modified task data (such as completion) into the model.
	/// </summary>
	private TaskInstanceViewModel CreateViewModel(TaskDefinition def, DateTime date, bool isRescheduled = false)
	{
		// Check if a persistent state record exists for this specific instance
		var stateKey = (def.Id, date.Date);
		//consider taking out the instanceState if we want to also retrieve other instance data like an updated description
		bool completed = _instanceStates.TryGetValue(stateKey, out var state) && state.IsCompleted;

		return new TaskInstanceViewModel
		{
			DefinitionId = def.Id,
			Title = def.Title,
			ScheduledDateTime = date,
			IsCompleted = completed,
			IsRescheduled = isRescheduled,
			Description = def.Description,
			Duration = def.Duration
		};
	}

	
}
/// <summary>
/// Contains altered state data of an instance of a task. Subject to expansion.
///Any new data added here MUST be retrieved in TaskManager.CreateViewModel() when returning a new TaskInstanceViewModel
/// </summary>
public partial class TaskInstanceState
{
	public bool IsCompleted { get; set; }
}
/// <summary>
/// View model generated dynamically by TaskManager for user view. Necessary to avoid storing generated virtual tasks.
/// </summary>
public class TaskInstanceViewModel
{
	public Guid DefinitionId { get; set; }
	public string Title { get; set; }
	public DateTime ScheduledDateTime { get; set; }
	public bool IsCompleted { get; set; }
	public bool IsRescheduled { get; set; }
	public String Description { get; set; }
	public TimeSpan Duration { get; set; }
}
