using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Interfaces;

public interface ILeagueFactory
{
    League CreateLeague(Settings.LeagueModel league);
}

