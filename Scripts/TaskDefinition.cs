#nullable enable
using System;
using System.Collections.Generic;

namespace Cozytasks.Scripts;

public partial class TaskDefinition
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; }
	public string Description { get; set; }
	public int Reward { get; set; } 
	public string Icon { get; set; }
	
	public TimeSpan Duration { get; set; }
	public RecurrenceRule? Rule { get; set; }
	public bool IsScheduled { get; set; }

	public TaskDefinition(string title,  string description, string icon, List<TimeSpan> times, 
		TimeSpan duration, bool isScheduled, RecurrencePattern pattern = RecurrencePattern.None, 
		DateTime? startDate = null, DateTime? endDate = null, List<DayOfWeek>? weekdays = null
		)
	{
		Title = title;
		Description = description;
		Icon = icon;
		Duration = duration;

		IsScheduled = isScheduled;
		if (IsScheduled && startDate != null && endDate != null && weekdays != null)
		{
			Rule = new RecurrenceRule(pattern, weekdays, times, startDate.Value,  endDate.Value);
		}
		else if (IsScheduled && startDate != null && weekdays != null)
		{
			Rule = new RecurrenceRule(pattern, weekdays, times, startDate.Value);
		}
	}
	
}	
