using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;
using Xunit.Abstractions;

namespace FootballManagerSimulator.Tests.Services;

[Collection(SaveFilesCollection.Name)]
public class DifficultySimulationTests : IDisposable
{
    // Set DIFFICULTY_SEASONS for a shorter (or longer) run.
    private static readonly int Seasons = int.TryParse(Environment.GetEnvironmentVariable("DIFFICULTY_SEASONS"), out var seasons) ? seasons : 10;
    private const int SigningsPerSeason = 3;
    private const int MaxSigningAge = 30;
    private const int ContractYears = 3;

    private readonly GameHost _host = new();
    private readonly ITestOutputHelper _output;

    // Set DIFFICULTY_LOG to choose where results are written as the run goes; xUnit only shows output at the end.
    private readonly string _logPath = Environment.GetEnvironmentVariable("DIFFICULTY_LOG")
        ?? Path.Combine(Path.GetTempPath(), "difficulty-simulation.log");

    public DifficultySimulationTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _host.Dispose();

    private void Log(string line = "")
    {
        _output.WriteLine(line);
        File.AppendAllText(_logPath, line + Environment.NewLine);
    }

    private static void PlayToday(GameEngine engine)
    {
        if (engine.AdvanceTodaysFixtures() == GameEngine.TodaysFixturesResult.Results)
            return;

        engine.AutoPickTeam();
        var error = engine.StartMatch();
        if (error != null)
        {
            foreach (var fixture in engine.TodaysFixtures().Where(f => !f.Concluded).ToList())
                engine.MatchSimulator.ConcludeFixture(fixture, engine.GetCompetitionForFixture(fixture)!);
            return;
        }

        var mine = engine.MyFixtureToday()!;
        engine.MatchSimulator.ConcludeFixture(mine, engine.GetCompetitionForFixture(mine)!);
        engine.CompleteRemainingFixtures();
    }

    private static void AnswerBids(GameEngine engine)
    {
        foreach (var pending in engine.State.Notifications.Where(n => n.RequiresAction).ToList())
        {
            if (pending.JobOfferClubId != null || pending.IsBoardWarning)
                pending.RequiresAction = false;
            else
                engine.RejectBid(pending);
        }
    }

    private static void PlayUntil(GameEngine engine, DateOnly until)
    {
        var guard = 0;
        while (engine.State.Date < until)
        {
            if (++guard > 2000)
                throw new Exception($"Game stuck at {engine.State.Date}");
            AnswerBids(engine);
            if (engine.Advance() == GameEngine.AdvanceResult.MustPlayFixtures)
                PlayToday(engine);
        }
    }

    // Same measure the game uses for MySquadAverageRating: the average of a club's best 20 players.
    private static double SquadStrength(GameEngine engine, int clubId)
    {
        var ratings = engine.State.Players
            .Where(p => p.Contract?.ClubId == clubId)
            .Select(p => p.Rating)
            .OrderByDescending(r => r)
            .Take(GameEngine.SquadAverageBestPlayers)
            .ToList();
        return ratings.Count == 0 ? 0 : ratings.Average();
    }

    // 1 = strongest squad in the league.
    private static int StrengthRank(GameEngine engine, LeagueModel league, int clubId)
    {
        var mine = SquadStrength(engine, clubId);
        return 1 + league.Clubs.Count(c => c.Id != clubId && SquadStrength(engine, c.Id) > mine);
    }

    private static int AskingFee(GameEngine engine, PlayerModel player) =>
        player.Contract == null
            ? 0
            : engine.GetTransferListItem(player.Id)?.AskingPrice ?? (int)(engine.GetTransferValue(player) * 1.3);

    // Signs the best-rated players who will join, are no older than MaxSigningAge and fit an even share of the budget.
    private static List<string> SignPlayers(GameEngine engine)
    {
        var signed = new List<string>();
        var myClubId = engine.MyClub.Id;

        for (var i = 0; i < SigningsPerSeason; i++)
        {
            var squadAverage = engine.MySquadAverageRating() ?? 0;
            var budgetShare = engine.MyClub.TransferBudget / (SigningsPerSeason - i);

            var target = engine.State.Players
                .Where(p => p.Contract?.ClubId != myClubId
                    && p.Rating > squadAverage
                    && p.GetAge(engine.State.Date) <= MaxSigningAge
                    && !engine.IsPlayerRetiring(p)
                    && engine.SigningLevelRefusal(p) == null)
                .Select(p => new { Player = p, Fee = AskingFee(engine, p) })
                .Where(c => c.Fee <= budgetShare)
                .OrderByDescending(c => c.Player.Rating)
                .ThenBy(c => c.Fee)
                .FirstOrDefault();

            if (target == null)
                break;

            var player = target.Player;
            if (player.Contract == null)
            {
                engine.SignFreeAgent(player.Id, ContractYears);
            }
            else
            {
                var bid = engine.SubmitBid(player.Id, target.Fee);
                if (!bid.Accepted)
                    break;
                engine.CompletePurchase(player.Id, target.Fee, ContractYears);
            }

            if (player.Contract?.ClubId != myClubId)
                break;

            signed.Add($"{player.Name} ({player.PreferredPosition}, {player.Rating}, £{target.Fee:n0})");
        }

        return signed;
    }

    // Keeps any player with an expiring contract who is among the squad's best 20 and isn't retiring.
    private static List<string> RenewBestContracts(GameEngine engine)
    {
        var myClubId = engine.MyClub.Id;
        var seasonEnd = new DateOnly(engine.State.Date.Month < 7 ? engine.State.Date.Year : engine.State.Date.Year + 1, 6, 30);
        var squad = engine.State.Players
            .Where(p => p.Contract?.ClubId == myClubId)
            .OrderByDescending(p => p.Rating)
            .ToList();
        var best = squad.Take(GameEngine.SquadAverageBestPlayers).ToHashSet();

        var renewed = new List<string>();
        foreach (var player in squad.Where(p => p.Contract!.ExpiryDate <= seasonEnd && best.Contains(p) && !engine.IsPlayerRetiring(p)))
        {
            engine.RenewContract(player.Id, ContractYears);
            if (player.Contract!.ExpiryDate > seasonEnd)
                renewed.Add($"{player.Name} ({player.Rating})");
        }
        return renewed;
    }

    // The out-of-contract pool on the first day of the season, before anyone (including us) signs from it.
    private static string FreeAgentPool(GameEngine engine)
    {
        var freeAgents = engine.State.Players.Where(p => p.Contract == null && !engine.IsPlayerRetiring(p)).ToList();
        if (freeAgents.Count == 0)
            return "0";

        var ours = engine.MySquadAverageRating() ?? 0;
        var ratings = freeAgents.Select(p => p.Rating).OrderByDescending(r => r).ToList();
        return $"{freeAgents.Count} (avg {ratings.Average():F1}, best {string.Join("/", ratings.Take(3))}, " +
            $"{ratings.Count(r => r > ours)} above our strength of {ours:F1}, " +
            $"{freeAgents.Count(p => p.Rating > ours && engine.SigningLevelRefusal(p) == null)} of them willing to join)";
    }

    // How full the AI squads are; clubs at 30 players sign no free agents.
    private static string AISquadSizes(GameEngine engine)
    {
        var sizes = engine.State.Players
            .Where(p => p.Contract != null && p.Contract.ClubId != engine.State.MyClubId)
            .GroupBy(p => p.Contract!.ClubId)
            .Select(g => g.Count())
            .ToList();
        return $"avg {sizes.Average():F1}, {sizes.Count(s => s >= 30)} of {sizes.Count} at 30+";
    }

    private record SeasonSnapshot(
        string LeagueName, int Rank, int Position, int Points,
        int Wins, int Draws, int Losses, int GF, int GA, string GD,
        int Budget, double Strength, int StrengthRank);

    private static SeasonSnapshot? CaptureSeasonEnd(GameEngine engine)
    {
        var state = engine.State;
        if (state.MyClubId is not { } myClubId) return null;
        var myLeagueId = state.Clubs.Single(c => c.Id == myClubId).LeagueId;
        var league = state.Competitions.OfType<LeagueModel>().SingleOrDefault(l => l.Id == myLeagueId);
        if (league == null) return null;

        var table = league.GenerateLeagueTable().ToList();
        var myEntry = table.FirstOrDefault(t => t.Club.Id == myClubId);
        if (myEntry == null) return null;

        int wins = 0, draws = 0, losses = 0;
        var myFixtures = league.Fixtures.Where(f => f.Concluded && f.FixtureType == FixtureType.Base &&
            (f.HomeClub.Id == myClubId || f.AwayClub.Id == myClubId));
        foreach (var f in myFixtures)
        {
            if (f.GoalsHome == f.GoalsAway) draws++;
            else if ((f.HomeClub.Id == myClubId && f.GoalsHome > f.GoalsAway) ||
                     (f.AwayClub.Id == myClubId && f.GoalsAway > f.GoalsHome)) wins++;
            else losses++;
        }

        return new SeasonSnapshot(league.Name, league.Rank, table.IndexOf(myEntry) + 1, myEntry.Points,
            wins, draws, losses, myEntry.GoalsScored, myEntry.GoalsConceded, myEntry.GoalDifference,
            engine.MyClub.TransferBudget, SquadStrength(engine, myClubId), StrengthRank(engine, league, myClubId));
    }

    [SimulationFact]
    public void TenSeasons_AverageDivision4Club_SigningThreePlayersASeason()
    {
        var engine = _host.Engine;
        engine.StartNewGame();
        engine.SetManagerName("Test Manager");

        // Players exist before a club is chosen, so pick the Division 4 club closest to the league's average strength.
        var division4 = _host.Settings.Competitions.First(c => c.IsLeague && c.CountryId == 1 && c.Rank == 4);
        var division4Clubs = engine.State.Clubs.Where(c => c.LeagueId == division4.Id).ToList();
        var leagueAverage = division4Clubs.Average(c => SquadStrength(engine, c.Id));
        var club = division4Clubs.OrderBy(c => Math.Abs(SquadStrength(engine, c.Id) - leagueAverage)).First();

        engine.SelectLeague(division4.Id);
        engine.SelectClub(club.Id);
        var state = engine.State;
        var startLeague = state.Competitions.OfType<LeagueModel>().Single(l => l.Id == division4.Id);

        File.WriteAllText(_logPath, "");
        Log($"=== DIFFICULTY SIMULATION: {Seasons} SEASONS, {SigningsPerSeason} SIGNINGS A SEASON, BEST-20 CONTRACTS RENEWED ===");
        Log($"Club: {club.Name} (ID {club.Id})");
        Log($"Starting League: {division4.Name} | League average strength {leagueAverage:F1}, ours {SquadStrength(engine, club.Id):F1} (rank {StrengthRank(engine, startLeague, club.Id)} of {startLeague.Clubs.Count})");
        Log($"Starting Budget: £{engine.MyClub.TransferBudget:n0}");
        Log("Strength = average of best 20 players; Str Rank = 1 is the strongest squad in the league; Free Agts = pool on 1 July");
        Log();

        var header = $"{"Season",-9} {"League",-14} {"Pos",-5} {"Pts",-5} {"W",-4} {"D",-4} {"L",-4} {"GF",-5} {"GA",-5} {"GD",-6} {"Budget",13} {"Strength",9} {"Str Rank",9} {"Free Agts",10}";
        Log(header);
        Log(new string('-', header.Length));
        var signingLog = new List<string>();
        var renewalLog = new List<string>();
        var freeAgentLog = new List<string>();
        var flowLog = new List<string>();
        var myClubId = club.Id;

        var seasonStart = new DateOnly(2026, 7, 1);
        var lastRank = division4.Rank;
        var sacked = false;

        for (var season = 1; season <= Seasons; season++)
        {
            var yearLabel = $"{seasonStart.Year}/{(seasonStart.Year + 1) % 100:D2}";

            var freeAgentCount = engine.State.Players.Count(p => p.Contract == null && !engine.IsPlayerRetiring(p));
            freeAgentLog.Add($"{yearLabel}: {FreeAgentPool(engine)}");
            var poolAtStart = engine.State.Players.Where(p => p.Contract == null).Select(p => p.Id).ToHashSet();
            var maxIdAtStart = engine.State.Players.Max(p => p.Id);
            var squadsAtStart = AISquadSizes(engine);
            var totalAtStart = engine.State.Players.Count;

            var signed = SignPlayers(engine);
            signingLog.Add($"{yearLabel}: {(signed.Count == 0 ? "no affordable targets" : string.Join(", ", signed))}");

            PlayUntil(engine, new DateOnly(seasonStart.Year, 9, 1));
            var squadsAfterSummerWindow = AISquadSizes(engine);

            PlayUntil(engine, new DateOnly(seasonStart.Year + 1, 6, 29));
            if (engine.IsUnemployed)
            {
                Log($"{yearLabel,-9} *** SACKED ***");
                sacked = true;
                break;
            }

            var snap = CaptureSeasonEnd(engine);
            if (snap == null)
            {
                Log($"{yearLabel,-9} NO DATA");
                break;
            }

            lastRank = snap.Rank;
            Log($"{yearLabel,-9} {snap.LeagueName,-14} {snap.Position,-5} {snap.Points,-5} {snap.Wins,-4} {snap.Draws,-4} {snap.Losses,-4} {snap.GF,-5} {snap.GA,-5} {snap.GD,-6} {"£" + snap.Budget.ToString("n0"),13} {snap.Strength,9:F1} {snap.StrengthRank,9} {freeAgentCount,10}");

            var renewed = RenewBestContracts(engine);
            renewalLog.Add($"{yearLabel}: {(renewed.Count == 0 ? "none expiring" : string.Join(", ", renewed))}");

            // During the season: who left the pool, and who was added by the youth intake.
            var players = engine.State.Players.ToDictionary(p => p.Id);
            var aiSigned = poolAtStart.Count(id => players.TryGetValue(id, out var p) && p.Contract != null && p.Contract.ClubId != myClubId);
            var weSigned = poolAtStart.Count(id => players.TryGetValue(id, out var p) && p.Contract?.ClubId == myClubId);
            var stillUnsigned = poolAtStart.Count(id => players.TryGetValue(id, out var p) && p.Contract == null);
            var youthIntake = engine.State.Players.Count(p => p.Id > maxIdAtStart);
            var newlyRetiring = poolAtStart.Count(id => players.TryGetValue(id, out var p) && p.Contract == null && engine.IsPlayerRetiring(p));

            // The end of the season on 30 June: releases, retirements and squad top-ups.
            var contracted = engine.State.Players.Where(p => p.Contract != null).ToDictionary(p => p.Id, p => p.Contract!.ClubId);
            var unsigned = engine.State.Players.Where(p => p.Contract == null).Select(p => p.Id).ToHashSet();
            var maxIdBeforeEnd = engine.State.Players.Max(p => p.Id);

            PlayUntil(engine, new DateOnly(seasonStart.Year + 1, 6, 30));
            if (engine.IsUnemployed)
            {
                Log($"*** SACKED at the end of {yearLabel} ***");
                sacked = true;
                break;
            }

            players = engine.State.Players.ToDictionary(p => p.Id);
            // A released player can be signed by a short squad the same day, so "released" means left his club.
            var released = contracted.Count(c => players.TryGetValue(c.Key, out var p) && p.Contract?.ClubId != c.Value);
            var retiredFromClubs = contracted.Count(c => !players.ContainsKey(c.Key));
            var retiredFromPool = unsigned.Count(id => !players.ContainsKey(id));
            var signedOn30June = engine.State.Players.Count(p => p.Contract != null && p.Id <= maxIdBeforeEnd &&
                (unsigned.Contains(p.Id) || (contracted.TryGetValue(p.Id, out var before) && before != p.Contract.ClubId)));
            var toppedUp = engine.State.Players.Count(p => p.Id > maxIdBeforeEnd);

            flowLog.Add($"{yearLabel}: pool on 1 July {poolAtStart.Count} | AI clubs signed {aiSigned}, we signed {weSigned}, still unsigned on 29 June {stillUnsigned} ({newlyRetiring} of them now retiring) | " +
                $"30 June: released {released}, signed straight back by short squads {signedOn30June}, retired from clubs {retiredFromClubs}, left the pool (retired or unsigned all season) {retiredFromPool} | " +
                $"total players in the game {totalAtStart} on 1 July, {engine.State.Players.Count} after 30 June | " +
                $"new players: youth intake {youthIntake}, top-ups {toppedUp} | " +
                $"AI squads on 1 July {squadsAtStart}, on 1 September {squadsAfterSummerWindow}");

            seasonStart = seasonStart.AddYears(1);
        }

        Log();
        Log("=== FREE AGENTS AT THE START OF EACH SEASON ===");
        freeAgentLog.ForEach(Log);
        if (!sacked)
            Log($"{seasonStart.Year}/{(seasonStart.Year + 1) % 100:D2}: {FreeAgentPool(engine)}");

        Log();
        Log("=== WHERE FREE AGENTS COME FROM AND GO ===");
        flowLog.ForEach(Log);

        Log();
        Log("=== SIGNINGS ===");
        signingLog.ForEach(Log);

        Log();
        Log("=== CONTRACT RENEWALS ===");
        renewalLog.ForEach(Log);

        Log();
        Log("=== SUMMARY ===");
        Log($"Started: Rank {division4.Rank} | Finished: Rank {lastRank}{(sacked ? " (sacked)" : "")}");
        Log($"Divisions climbed: {division4.Rank - lastRank}");
    }
}
