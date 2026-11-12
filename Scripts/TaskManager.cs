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
	//cases:
	//unscheduled task is being scheduled.
	//scheduled task is changed but not its scheduling data
	//scheduled task is changed, including scheduling data
	public void UpdateTask(Guid taskId, string? title = null, string? description = null, string? icon = null,
		List<TimeSpan>? times = null, TimeSpan? duration = null, int? reward = null,
		RecurrencePattern? pattern = null, DateTime? startDate = null, DateTime? endDate = null, 
		List<DayOfWeek>? weekdays = null)
	{
		TaskDefinition curTask =  _definitions[taskId];
		
		//If the scheduling data would change, replace the TaskDefinition
		if (pattern != null || times != null || startDate != null || endDate != null || weekdays != null)
		{
			TaskDefinition newTask = new TaskDefinition
			{
				Title = title ?? curTask.Title,
				Description = description ?? curTask.Description,
				Reward = reward ?? curTask.Reward,
				Icon = icon ?? curTask.Icon,
				Duration = duration ?? curTask.Duration,
				IsScheduled = true,
				Rule = new RecurrenceRule()
			};

			if (curTask.Rule == null)
			{//This segment of code should only be reached when startDate has a value. It's an optional parameter in case we do not change the scheduling
				newTask.Rule.Pattern = pattern ?? RecurrencePattern.None;
				newTask.Rule.Times = times ?? [];
				newTask.Rule.StartDate = startDate ?? DateTime.Now; //potentially problematic. startDate ideally is passed from GUI using date of task being altered
				newTask.Rule.EndDate = endDate ?? null;
				newTask.Rule.AllowedDays = weekdays ?? [];
			}
			else if (curTask.Rule != null)
			{
				newTask.Rule.Pattern = pattern ?? curTask.Rule.Pattern;
				newTask.Rule.Times = times ?? curTask.Rule.Times;
				newTask.Rule.StartDate = startDate ?? DateTime.Now; //also potentially problematic, same reason as other comment.
				newTask.Rule.EndDate = endDate ?? curTask.Rule.EndDate;
				newTask.Rule.AllowedDays = weekdays ?? curTask.Rule.AllowedDays;
				
				var cutOffset = startDate ?? DateTime.Now;
				curTask.Rule.EndDate = cutOffset.Subtract(TimeSpan.FromDays(1)); //Set original task's end before new start
			}
			_definitions.Add(newTask.Id, newTask);
		}
		else
		{
			curTask.Title = title ?? curTask.Title;
			curTask.Description = description ?? curTask.Description;
			curTask.Icon = icon ?? curTask.Icon;
			curTask.Reward = reward ?? curTask.Reward;
			curTask.Duration = duration ?? curTask.Duration;
		}
		
		//Need async database state changing to happen here
		
		EmitSignal(SignalName.TasksUpdated);
	}
	
	/// <summary>
	/// Prevents the task manager from generating future virtual tasks from a scheduled recurring task beyond
	/// the set time (if applicable). Intended to terminate a recurring task while keeping record of past instances.
	/// </summary>
	/// <param name="taskId"></param>
	/// <param name="ending"></param>
	public void DiscontinueTask(Guid taskId, DateTime? ending = null)
	{
		
		TaskDefinition deadTask =  _definitions[taskId];
		if (deadTask.Rule == null) return; //This should not happen, but the IDE was complaining.
		
		deadTask.Rule.EndDate = ending ?? DateTime.Now;
		
		// Need async database state changing to happen here
		
		EmitSignal(SignalName.TasksUpdated);
	}

	/// <summary>
	/// Ideally, this is used only for deleting non-recurring, non-scheduled tasks. Deleting a recurring task
	/// will remove completed past instances as well. Be careful with where this function is called through UI.
	/// </summary>
	public bool DeleteTask(Guid taskId)
	{
		if (!_definitions.ContainsKey(taskId)) return false;
		
		_definitions.Remove(taskId);
		
		// Need async 

		EmitSignal(SignalName.TasksUpdated);
		
		return true;
	}
	
	/// <summary>
	/// Cancels an instance of a task, causing it not to appear on the calendar when GetMonthViewInstances is called.
	/// SHOULD NOT be called with a task that has no recurrence rule.
	/// </summary>
	/// <param name="taskId"></param>
	/// <param name="excludedDate"></param>
	public void CancelTaskInstance(Guid taskId, DateTime excludedDate)
	{
		var task = _definitions[taskId];
		if (task.Rule == null) return;
		task.Rule.ExcludedDates.Add(excludedDate);
		
		// Need async database state changing to happen here

		EmitSignal(SignalName.TasksUpdated);
	}

	public void RescheduleTaskInstance(Guid taskId, DateTime oldDate, DateTime newDate)
	{
		var task = _definitions[taskId];
		if (task.Rule == null) return; //this should not happen

		task.Rule.RescheduledDates[oldDate] = newDate;
		
		//need async database state changing to happen here
		
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
		
		// Need async database state changing to happen here
		
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
				//ensure only view models for the current month are injected, and only if they are still
				//valid (in case the RuleDefinition is decommissioned)
				if (newDate <= def.Rule.EndDate && newDate >= monthStart && newDate <= monthEnd)
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
	public required string Title { get; set; }
	public DateTime ScheduledDateTime { get; set; }
	public bool IsCompleted { get; set; }
	public bool IsRescheduled { get; set; }
	public String? Description { get; set; }
	public TimeSpan Duration { get; set; }
}
