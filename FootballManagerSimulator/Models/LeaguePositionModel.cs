﻿namespace FootballManagerSimulator.Structures;


public class LeaguePositionModel
{
    public string ClubName { get; set; } = "";
    public int Points { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public int Played { get; set; }
    public string GoalDifference => FormatGoalDifference();

    public string FormatGoalDifference()
    {
        var goalDifference = GoalsScored - GoalsConceded;
        return goalDifference > 0 ? $"+{goalDifference}" : goalDifference.ToString();
    }
}