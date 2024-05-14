using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Factories;

public class EnglishLeagueCupFactory(
    IOptions<Settings> settings,
    IClubHelper clubHelper) : ICompetitionFactory
{
    private readonly Settings Settings = settings.Value;

    public string Type => "Cup";

    public ICompetition CreateCompetition(Settings.CompetitionModel competition)
    {
        var clubs = Settings.Clubs
            .Where(p => new List<int> { 1, 2, 3, 4 }.Contains(p.LeagueId))
            .Select(p => new Club
            {
                Id = p.Id,
                Name = p.Name,
                LeagueId = p.LeagueId
            });

        var cup = new Cup()
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = clubs.ToList(),
            Round = 1,
        };

        GenerateNextRoundOfFixtures(cup);

        return cup;
    }

    public void GenerateNextRoundOfFixtures(Cup cup)
    {
        if (cup.Round == 1)
        {
            var clubs = cup.Clubs
                .Where(p => new List<int> { 2, 3, 4 }.Contains(p.LeagueId))
                .Select(p => new Club()
                {
                    Id = p.Id,
                    Name = p.Name
                }).ToList();
            cup.Fixtures = GenerateNextRoundOfFixtures(clubs.ToList(), new DateOnly(2016, 08, 07), cup.Round);
            return;
        }
        throw new NotImplementedException();
    }

    private List<Fixture> GenerateNextRoundOfFixtures(List<Club> clubs, DateOnly date, int round) 
    {
        var fixtures = new List<Fixture>();
        for(int i = 0; i < clubs.Count(); i+=2)
        {
            fixtures.Add(new Fixture()
            {
                HomeClub = clubs.ElementAt(i),
                AwayClub = clubs.ElementAt(i+1),
                Date = date,
                Round = round
            });
        }
        return fixtures;
    }
}
