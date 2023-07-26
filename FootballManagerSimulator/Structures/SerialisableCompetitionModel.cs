using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FootballManagerSimulator.Structures.Fixture;

namespace FootballManagerSimulator.Structures;

public class SerialisableCompetitionModel
{
    public string Name { get; set; } = "";

    public string CompetitionType { get; set; } = "";

    public List<SerialisableFixtureModel> Fixtures { get; set; } = new List<SerialisableFixtureModel>();
}
