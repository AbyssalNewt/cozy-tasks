using Godot;
using System;
using System.Collections.Generic;

namespace Cozytasks.Scripts;

public enum RecurrencePattern
{
	None,
	Daily,
	Weekly,
	Monthly
}
#nullable enable
public class RecurrenceRule
{
	public RecurrenceRule(RecurrencePattern pattern, List<DayOfWeek> weekDays, 
		List<TimeSpan> times, DateTime startDate, DateTime? endDate = null)
	{
		StartDate = startDate;
		EndDate = endDate;
		Pattern = pattern;
		AllowedDays = weekDays;
		Times = times;
	}
	
	public RecurrencePattern Pattern { get; set; }	//recurrence category
	public List<DayOfWeek> AllowedDays { get; set; } = [];	//days of the week which a task occurs
	public DateTime StartDate { get; set; } //the starting window of a scheduled task, doesn't need HR:MIN:SEC
	public DateTime? EndDate { get; set; } //the date-wise ending window of a scheduled task

	public List<TimeSpan> Times { get; set; } = []; // times in the day a task occurs, HR:MIN(:SEC)
	
	//stores exact DateTime (necessary for proper exclusion in GetMonthViewInstances)
	public HashSet<DateTime> ExcludedDates { get; set; } = [];
	
	//stores exact DateTime (necessary for proper checking and injection in GetMonthViewInstances)
	public Dictionary<DateTime, DateTime> RescheduledDates { get; set; } = new();


	
	/// <summary>
	/// Pure function that yields all concrete dates this rule hits within a given window.
	/// Perfect for populating a month view grid on demand.
	/// </summary>
	public IEnumerable<DateTime> GetOccurrences(DateTime windowStart, DateTime windowEnd)
	{
		var cursor = StartDate.Date;
		
		var end = EndDate.HasValue && EndDate.Value.Date < windowEnd.Date 
		? EndDate.Value.Date 
		: windowEnd.Date;
		
		if (cursor > end) yield break;

		switch (Pattern)
		{
			case RecurrencePattern.None:
				if (cursor >= windowStart.Date && cursor <= end) //Find the one day it's supposed to happen
					yield return cursor;
				break;

			case RecurrencePattern.Daily:
				while (cursor <= end)
				{
					if (cursor >= windowStart.Date) //It's daily, so we will return a day for every day of the month within the window
						yield return cursor;
					cursor = cursor.AddDays(1);
				}
				break;

			case RecurrencePattern.Weekly:
				while (cursor <= end)
				{
					if (cursor >= windowStart.Date && AllowedDays.Contains(cursor.DayOfWeek)) //Return all days of the week within the window
						yield return cursor;
					cursor = cursor.AddDays(1);
				}
				break;

			case RecurrencePattern.Monthly:
				while (cursor <= end)
				{
					if (cursor >= windowStart.Date)
						yield return cursor;
					// Safely jump to next month same day
					try { cursor = cursor.AddMonths(1); }
					catch { break; }
				}
				break;
		}
	}
}
