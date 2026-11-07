//CONTAINS TaskManager, TaskInstanceViewModel, TaskInstanceState
using Godot;
using System;
using System.Collections.Generic;

public class TaskManager
{
	private Dictionary<Guid, TaskDefinition> _definitions = new();
	
	// Persistent state store (serialized to JSON?)
	private Dictionary<(Guid DefinitionId, DateTime Date), TaskInstanceState> _instanceStates = new();

	public List<TaskInstanceViewModel> GetMonthViewInstances(DateTime monthStart, DateTime monthEnd)
	{
		var viewModels = new List<TaskInstanceViewModel>();

		foreach (var def in _definitions.Values)
		{
			// 1. Evaluate rule math & exceptions (Virtual Projections)
			foreach (var ruleDate in def.Rule.GetOccurrences(monthStart, monthEnd))
			{
				if (def.Rule.RescheduledDates.ContainsKey(ruleDate)) continue;
				if (def.Rule.ExcludedDates.Contains(ruleDate)) continue;

				viewModels.Add(CreateViewModel(def, ruleDate));
			}

			// 2. Inject rescheduled instances
			foreach (var (originalDate, newDate) in def.Rule.RescheduledDates)
			{
				if (newDate >= monthStart && newDate <= monthEnd)
				{
					viewModels.Add(CreateViewModel(def, newDate, isRescheduled: true));
				}
			}
		}

		return viewModels;
	}

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
			ScheduledDate = date,
			IsCompleted = completed,
			IsRescheduled = isRescheduled,
			Description = def.Description
		};
	}

	public void SetTaskCompleted(Guid definitionId, DateTime date, bool completed)
	{
		var key = (definitionId, date.Date);
		if (!_instanceStates.ContainsKey(key))
		{
			_instanceStates[key] = new TaskInstanceState();
		}
		_instanceStates[key].IsCompleted = completed;
	}
}

public partial class TaskInstanceState
{
	public bool IsCompleted { get; set; }
}

public class TaskInstanceViewModel
{
	public Guid DefinitionId { get; set; }
	public string Title { get; set; }
	public DateTime ScheduledDate { get; set; }
	public bool IsCompleted { get; set; }
	public bool IsRescheduled { get; set; }
	public String Description { get; set; }
}
