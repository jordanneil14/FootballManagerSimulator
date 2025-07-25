﻿using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Models;

public class Settings
{
    public GeneralModel General { get; set; } = new GeneralModel();
    public class GeneralModel
    {
        public string StartDate { get; set; } = "";
        public DateOnly StartDateAsDate => DateOnly.Parse(StartDate);
    }
    public List<Club> Clubs { get; set; } = new List<Club>();
    public IEnumerable<CompetitionModel> Competitions { get; set; } = new List<CompetitionModel>();
    public IEnumerable<IdNameModel> Countries { get; set; } = new List<IdNameModel>();

    public class IdNameModel
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
    }

}
