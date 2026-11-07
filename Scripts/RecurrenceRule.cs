using Godot;
using System;
using System.Collections.Generic;

public enum RecurrencePattern
{
	None,
	Daily,
	Weekly,
	Monthly
}

public class RecurrenceRule
{
	public RecurrencePattern Pattern { get; set; } = RecurrencePattern.None;
	public List<DayOfWeek> AllowedDays { get; set; } = new();
	public DateTime StartDate { get; set; }
	public DateTime? EndDate { get; set; }
	
	public HashSet<DateTime> ExcludedDates { get; set; } = new();
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
				if (cursor >= windowStart.Date && cursor <= end)
					yield return cursor;
				break;

			case RecurrencePattern.Daily:
				while (cursor <= end)
				{
					if (cursor >= windowStart.Date)
						yield return cursor;
					cursor = cursor.AddDays(1);
				}
				break;

			case RecurrencePattern.Weekly:
				while (cursor <= end)
				{
					if (cursor >= windowStart.Date && AllowedDays.Contains(cursor.DayOfWeek))
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
