﻿namespace FootballManagerSimulator.Models;

public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TransferBudget { get; set; }
    public int LeagueId { get; set; }
    public string Stadium { get; set; } = "";
    public int StadiumSize { get; set; } = 30000;
    public string TransferBudgetFriendly { get => $"£{TransferBudget:n}"; }
    public int WageBudget { get; set; }
    public List<TacticSlot> TacticSlots { get; set; } = new List<TacticSlot>();
    public string Formation { get; set; } = "4-4-2";
}

