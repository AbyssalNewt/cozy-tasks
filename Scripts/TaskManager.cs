//CONTAINS TaskManager, TaskInstanceViewModel, TaskInstanceState
using Godot;
using System;
using System.Collections.Generic;

public class TaskManager
{
	private Dictionary<Guid, TaskDefinition> _definitions = new();
	
	// Persistent state store (serialized to JSON?)
	private Dictionary<(Guid DefinitionId, DateTime Date), TaskInstanceState> _instanceStates = new();

	//Dynamically generates tasks 
	public List<TaskInstanceViewModel> GetMonthViewInstances(DateTime monthStart, DateTime monthEnd)
	{
		var viewModels = new List<TaskInstanceViewModel>();
		
		foreach (var def in _definitions.Values)
		{
			// 1. Evaluate rule math & exceptions (Virtual Projections)
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

	// Creates view model for a task instance. Checks if a persisted record of an altered task exists to inject
	// modified task data (such as completion) into the model.
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

	//Creates a new TaskInstanceState (if necessary) for a given task instance and writes it to the instance dict
	public void SetTaskCompleted(Guid definitionId, DateTime date)
	{
		var key = (definitionId, date.Date);
		if (!_instanceStates.ContainsKey(key))
		{
			_instanceStates[key] = new TaskInstanceState();
		}
		_instanceStates[key].IsCompleted = !(_instanceStates[key].IsCompleted);
	}
}

// Contains altered state data of an instance of a task. Subject to expansion.
//Any new data added here MUST be retrieved in TaskManager.CreateViewModel() when returning a new TaskInstanceViewModel
public partial class TaskInstanceState
{
	public bool IsCompleted { get; set; }
}

// View model generated dynamically by TaskManager for user view. Necessary to avoid storing generated virtual tasks.
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
