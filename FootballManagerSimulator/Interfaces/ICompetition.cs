﻿using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetition
{
    int ID { get; } 
    string Name { get; }
    IEnumerable<Fixture> Fixtures { get; set; }
    IEnumerable<Club> Clubs { get; set; }
    JObject SerialisableCompetition();
}
