using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Cozytasks.Scripts;

public class DbResult<T>
{
	public bool Ok { get; set; }
	public T Data { get; set; }
	public string Error { get; set; } = "";
}
 
public static class Visibility
{
	public const string Individual = "individual";   // only the owner (+ household if set)
	public const string Friends = "friends";         // owner's friends (+ household if set)
	public const string Public = "public";           // everyone
}
 
public class Profile
{
	public Guid Id { get; set; }
	public string DisplayName { get; set; }
	public string Avatar { get; set; }
	public string AccountType { get; set; }   // "adult" or "child"
	public bool HasLogin { get; set; }        // false = I made it so a child doesnt have a login by email but just a name
	public string FriendCode { get; set; }    // adults only
	public int Xp { get; set; }
 
	public bool IsChild => AccountType == "child";
}
public class Household
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string InviteCode { get; set; }
}
/// One of the households the logged-in player belongs to.
public class HouseholdMembership
{
	public string Role { get; set; }          // "admin" or "member"
	public Household Household { get; set; }
}
/// One person inside a household (member list, leaderboard, "who is playing?").
public class HouseholdMember
{
	public string Role { get; set; }
	public Guid UserId { get; set; }
	public Profile Profile { get; set; }
}
 
public class Friendship
{
	public Guid Id { get; set; }
	public Guid RequesterId { get; set; }
	public Guid AddresseeId { get; set; }
	public string Status { get; set; }        // "pending" or "accepted"
	public Profile Requester { get; set; }
	public Profile Addressee { get; set; }
 
	/// The other person in this friendship.
	public Profile Other(Guid me) => RequesterId == me ? Addressee : Requester;
	/// True if someone sent the the user this request and the user hasn't answered yet.
	public bool IsIncomingRequest(Guid me) => Status == "pending" && AddresseeId == me;
}
 
/// A task row from the database. Use ToDefinition() to get the game's TaskDefinition.
public class TaskRecord
{
	public Guid Id { get; set; }
	public Guid OwnerId { get; set; }
	public Guid? HouseholdId { get; set; }    // null = personal task
	public string Visibility { get; set; } = global::Visibility.Individual;
	public Guid? AssignedTo { get; set; }     // null = anyone in the household
 
	public string Title { get; set; }
	public string Description { get; set; }
	public int XpReward { get; set; } = 10;
	public int DurationMinutes { get; set; }
 
	public string Recurrence { get; set; } = "none";   // none, daily, weekly, monthly
	public List<int> AllowedDays { get; set; } = new(); // 0 = Sunday ... 6 = Saturday
	public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
	public DateOnly? EndDate { get; set; }
	public List<DateOnly> ExcludedDates { get; set; } = new();
	public Dictionary<string, string> RescheduledDates { get; set; } = new();  // "yyyy-MM-dd" -> "yyyy-MM-dd"
 
	public Profile Owner { get; set; }        // only DisplayName (null if you can't see them)
	public Profile Assignee { get; set; }     // only DisplayName
 
	private const string DateFormat = "yyyy-MM-dd";
 
	/// Converts to the game's TaskDefinition (Id stays the same as the database id).
	public TaskDefinition ToDefinition() => new TaskDefinition
	{
		Id = Id,
		Title = Title,
		Description = Description,
		Reward = XpReward,
		Duration = TimeSpan.FromMinutes(DurationMinutes),
		Rule = new RecurrenceRule
		{
			Pattern = Recurrence switch
			{
				"daily" => RecurrencePattern.Daily,
				"weekly" => RecurrencePattern.Weekly,
				"monthly" => RecurrencePattern.Monthly,
				_ => RecurrencePattern.None,
			},
			AllowedDays = AllowedDays.Select(d => (DayOfWeek)d).ToList(),
			StartDate = ToDateTime(StartDate),
			EndDate = EndDate.HasValue ? ToDateTime(EndDate.Value) : null,
			ExcludedDates = ExcludedDates.Select(ToDateTime).ToHashSet(),
			RescheduledDates = RescheduledDates.ToDictionary(
				kv => ParseDate(kv.Key),
				kv => ParseDate(kv.Value)),
		},
	};
 
	/// Builds a record from a TaskDefinition so it can be saved.
	public static TaskRecord FromDefinition(TaskDefinition def, string visibility = global::Visibility.Individual,
		Guid? householdId = null, Guid? assignedTo = null) => new TaskRecord
	{
		Id = def.Id,
		HouseholdId = householdId,
		Visibility = visibility,
		AssignedTo = assignedTo,
		Title = def.Title,
		Description = def.Description,
		XpReward = def.Reward,
		DurationMinutes = (int)def.Duration.TotalMinutes,
		Recurrence = def.Rule.Pattern.ToString().ToLowerInvariant(),
		AllowedDays = def.Rule.AllowedDays.Select(d => (int)d).ToList(),
		StartDate = DateOnly.FromDateTime(def.Rule.StartDate),
		EndDate = def.Rule.EndDate.HasValue ? DateOnly.FromDateTime(def.Rule.EndDate.Value) : null,
		ExcludedDates = def.Rule.ExcludedDates.Select(DateOnly.FromDateTime).ToList(),
		RescheduledDates = def.Rule.RescheduledDates.ToDictionary(
			kv => kv.Key.ToString(DateFormat, CultureInfo.InvariantCulture),
			kv => kv.Value.ToString(DateFormat, CultureInfo.InvariantCulture)),
	};
	/// Only the columns that get saved (used internally by Db.cs).
	internal Dictionary<string, object> ToWrite(bool includeId)
	{
		var row = new Dictionary<string, object>
		{
			["household_id"] = HouseholdId,
			["visibility"] = Visibility,
			["assigned_to"] = AssignedTo,
			["title"] = Title,
			["description"] = Description,
			["xp_reward"] = XpReward,
			["duration_minutes"] = DurationMinutes,
			["recurrence"] = Recurrence,
			["allowed_days"] = AllowedDays,
			["start_date"] = StartDate,
			["end_date"] = EndDate,
			["excluded_dates"] = ExcludedDates,
			["rescheduled_dates"] = RescheduledDates,
		};
		if (includeId && Id != Guid.Empty) row["id"] = Id;
		if (OwnerId != Guid.Empty) row["owner_id"] = OwnerId;
		return row;
	}
	private static DateTime ToDateTime(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);
	private static DateTime ParseDate(string s) =>
		DateOnly.ParseExact(s, DateFormat, CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue);
}
/// One completed task instance (who did which task on which day).

public class TaskCompletion
{
	public Guid TaskId { get; set; }
	public DateOnly OccurrenceDate { get; set; }
	public Guid ProfileId { get; set; }
	public int XpEarned { get; set; }
	public DateTime CompletedAt { get; set; }
	public TaskRecord Task { get; set; }      // only Title (activity feed)
	public Profile Profile { get; set; }      // only DisplayName (activity feed)
}
// ---- used internally by Db.cs 
public class AuthSession
{
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }
	public int ExpiresIn { get; set; }
	public AuthUser User { get; set; }
}
 
public class AuthUser
{
	public Guid Id { get; set; }
	public string Email { get; set; }
}
 
