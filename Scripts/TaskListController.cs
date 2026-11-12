#nullable enable
using Godot;
using System;
using System.Collections.Generic;

namespace Cozytasks.Scripts;

public partial class TaskListController : Node
{
	public override void _Ready()
	{
		// Give TaskManager a brief moment to initialize its Instance singleton
		CallDeferred(nameof(InjectFakeTasks));
	}

	private void InjectFakeTasks()
	{
		if (TaskManager.Instance == null)
		{
			GD.PrintErr("TaskManager instance not found!");
			return;
		}

		GD.Print("Injecting fake tasks into TaskManager...");

		// 1. Create a daily recurring morning routine task
		TaskManager.Instance.CreateTask(
			title: "Morning Stretch & Yoga",
			description: "15 minutes of light mobility work.",
			icon: "yoga",
			times: [new TimeSpan(7, 30, 0)],
			duration: TimeSpan.FromMinutes(15),
			isScheduled: true,
			pattern: RecurrencePattern.Daily,
			startDate: DateTime.Today,
			weekdays: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]
		);

		// 2. Create a weekly chore task
		TaskManager.Instance.CreateTask(
			title: "Deep Clean Kitchen",
			description: "Wipe counters, mop floors, take out trash.",
			icon: "clean",
			times: [new TimeSpan(10, 0, 0)],
			duration: TimeSpan.FromMinutes(60),
			isScheduled: true,
			pattern: RecurrencePattern.Weekly,
			startDate: DateTime.Today,
			weekdays: [DayOfWeek.Saturday]
		);

		// 3. Create an unscheduled backlog task
		TaskManager.Instance.CreateTask(
			title: "Fix Living Room Lamp",
			description: "Replace the frayed power cord.",
			icon: "tools",
			times: [],
			duration: TimeSpan.FromMinutes(30),
			isScheduled: false
		);

		GD.Print("Fake tasks injected successfully!");
	}
}
