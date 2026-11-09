using Godot;
using System;
using System.Collections.Generic;

public partial class TaskDefinition
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public String Title { get; set; }
	public String Description { get; set; }
	public int Reward { get; set; }
	// public Sprite2D Icon { get; set; }
	
	public TimeSpan Duration { get; set; }
	public RecurrenceRule Rule { get; set; } = new();
	
}	
