using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Models;

public class Cup : ICompetition
{
    public string Type => "Cup";
    public int Round { get; set; } = 1;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public List<Club> Clubs { get; set; } = new List<Club>();
}
