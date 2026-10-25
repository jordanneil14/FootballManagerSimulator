namespace FootballManagerSimulator.PlayerGenerator;

public class PlayerGenerator
{
    private readonly Random _random;

    public PlayerGenerator()
    {
        _random = new Random();
    }

    public PlayerGenerator(int seed)
    {
        _random = new Random(seed);
    }

    public List<GeneratedPlayer> GeneratePlayers(int count, string referenceDate)
    {
        var players = new List<GeneratedPlayer>(count);
        for (int i = 0; i < count; i++)
            players.Add(GeneratePlayer(referenceDate));
        return players;
    }

    public List<GeneratedPlayer> GenerateSquad(string clubName, int squadSize = 25, string startDate = "2026-07-01", int leagueRank = 1)
    {
        var players = new List<GeneratedPlayer>(squadSize);

        var (gkCount, defCount, midCount, fwdCount) = SquadShape(squadSize);

        for (int i = 0; i < gkCount; i++)
            players.Add(GeneratePlayerForClub(clubName, "GK", startDate, leagueRank));
        for (int i = 0; i < defCount; i++)
            players.Add(GeneratePlayerForClub(clubName, PickRandom(DefenderPositions), startDate, leagueRank));
        for (int i = 0; i < midCount; i++)
            players.Add(GeneratePlayerForClub(clubName, PickRandom(MidfielderPositions), startDate, leagueRank));
        for (int i = 0; i < fwdCount; i++)
            players.Add(GeneratePlayerForClub(clubName, PickRandom(ForwardPositions), startDate, leagueRank));

        AssignSquadNumbers(players);
        return players;
    }

    // Tops up an existing squad with exactly `count` players, each one going to whichever area
    // (GK, defence, midfield, attack) is furthest below its share of the finished squad.
    // Shirt numbers are left unset, since only the caller knows which numbers the squad already uses.
    public List<GeneratedPlayer> GenerateTopUp(string clubName, int count, IEnumerable<string> existingPositions, string startDate, int leagueRank = 1)
    {
        var have = new int[4];
        foreach (var position in existingPositions)
            have[PositionGroup(position)]++;

        var (gk, def, mid, fwd) = SquadShape(have.Sum() + count);
        int[] target = [gk, def, mid, fwd];

        var players = new List<GeneratedPlayer>(count);
        for (int i = 0; i < count; i++)
        {
            var group = Enumerable.Range(0, 4).MaxBy(g => target[g] - have[g]);
            have[group]++;

            var position = group switch
            {
                0 => "GK",
                1 => PickRandom(DefenderPositions),
                2 => PickRandom(MidfielderPositions),
                _ => PickRandom(ForwardPositions)
            };
            players.Add(GeneratePlayerForClub(clubName, position, startDate, leagueRank));
        }

        return players;
    }

    private static readonly string[] DefenderPositions = ["CB", "CB", "CB", "LB", "RB", "LB", "RB"];
    private static readonly string[] MidfielderPositions = ["CDM", "CM", "CM", "CAM", "LM", "RM"];
    private static readonly string[] ForwardPositions = ["ST", "ST", "LW", "RW"];

    // How many keepers, defenders, midfielders and forwards a squad of this size should have.
    private static (int Gk, int Def, int Mid, int Fwd) SquadShape(int squadSize)
    {
        int gk = Math.Max(2, squadSize / 11);
        int def = (int)Math.Ceiling(squadSize * 0.32);
        int mid = (int)Math.Ceiling(squadSize * 0.32);
        return (gk, def, mid, squadSize - gk - def - mid);
    }

    // 0 = GK, 1 = defender, 2 = midfielder, 3 = forward.
    private static int PositionGroup(string position) => position switch
    {
        "GK" => 0,
        "CB" or "LCB" or "RCB" or "LB" or "RB" or "LWB" or "RWB" => 1,
        "CDM" or "LDM" or "RDM" or "CM" or "LCM" or "RCM" or "CAM" or "LAM" or "RAM" or "LM" or "RM" => 2,
        _ => 3
    };

    // Kept in step with ShirtNumberRules in the game, which this project cannot reference.
    private static readonly int[][] ReservedNumbers =
    [
        [1, 13, 21],    // goalkeepers
        [2, 3, 5, 6],   // defenders
        [4, 7, 8, 11],  // midfielders
        [9, 10]         // attackers
    ];

    // The best players in each line take that line's reserved numbers (keepers 1, 13, 21; defenders
    // 2, 3, 5, 6; midfielders 4, 7, 8, 11; attackers 9, 10). Everyone else, best first, takes the
    // lowest number nobody keeps: 12, 14-20, then 22 upwards.
    private static void AssignSquadNumbers(List<GeneratedPlayer> squad)
    {
        var overflow = new List<GeneratedPlayer>();
        foreach (var line in squad.GroupBy(p => PositionGroup(p.ClubPosition)))
        {
            var numbers = ReservedNumbers[line.Key];
            var players = line.OrderByDescending(p => p.Rating).ToList();
            for (var i = 0; i < players.Count; i++)
            {
                if (i < numbers.Length)
                    players[i].ShirtNumber = numbers[i];
                else
                    overflow.Add(players[i]);
            }
        }

        var reserved = ReservedNumbers.SelectMany(n => n).ToHashSet();
        var unreserved = Enumerable.Range(1, 99).Where(n => !reserved.Contains(n)).GetEnumerator();
        foreach (var player in overflow.OrderByDescending(p => p.Rating))
        {
            unreserved.MoveNext();
            player.ShirtNumber = unreserved.Current;
        }
    }

    // Academy graduates: 16 or 17 years old, rated below the senior squad, on a 2-3 year first contract.
    // A rating-5 academy knocks 10-20 off a senior rating; each point above or below 5 shifts that by 2.
    public List<GeneratedPlayer> GenerateYouthIntake(string clubName, int count, string startDate, int leagueRank, int academyRating)
    {
        var academyBonus = (academyRating - 5) * 2;
        var players = new List<GeneratedPlayer>(count);
        var startDateOnly = DateOnly.Parse(startDate);

        for (int i = 0; i < count; i++)
        {
            var position = PickRandom(["GK", "CB", "CB", "LB", "RB", "CDM", "CM", "CM", "CAM", "LM", "RM", "ST", "ST", "LW", "RW"]);
            var age = _random.Next(16, 18);
            var rating = Math.Max(35, GenerateRating(leagueRank) - _random.Next(10, 21) + academyBonus);

            // A 90+ graduate only comes through one time in five; the rest are knocked down 5 into the high 80s.
            var isGenerationalTalent = _random.Next(5) == 0;
            if (rating >= 90 && !isGenerationalTalent)
                rating -= 5;

            var player = GeneratePlayerWithPosition(position, clubName, null, startDate, startDateOnly, leagueRank, age, rating);
            player.Contract = new GeneratedContract
            {
                ClubID = 0,
                ClubName = clubName,
                ExpiryDate = new DateOnly(startDateOnly.Year, 6, 30).AddYears(_random.Next(2, 4)).ToString("yyyy-MM-dd"),
                WeeklyWage = 0,
                StartDate = startDate
            };
            players.Add(player);
        }

        return players;
    }

    public GeneratedPlayer GeneratePlayer(string referenceDate)
    {
        var position = PickRandom(NameData.Positions);
        return GeneratePlayerWithPosition(position, null, null, null, DateOnly.Parse(referenceDate), PickFreeAgentLeagueRank());
    }

    // Free agents are mostly lower-league quality, with the occasional top-flight player.
    private int PickFreeAgentLeagueRank()
    {
        var roll = _random.Next(100);
        return roll switch
        {
            < 10 => 1,
            < 30 => 2,
            < 60 => 3,
            < 85 => 4,
            _ => 5
        };
    }

    private GeneratedPlayer GeneratePlayerForClub(string clubName, string position, string startDate, int leagueRank = 1)
    {
        var contractYears = _random.Next(1, 5);
        var startDateOnly = DateOnly.Parse(startDate);
        var expiryDate = new DateOnly(startDateOnly.Year + contractYears, 6, 30);

        var player = GeneratePlayerWithPosition(position, clubName, null, startDate, startDateOnly, leagueRank);
        player.Contract = new GeneratedContract
        {
            ClubID = 0,
            ClubName = clubName,
            ExpiryDate = expiryDate.ToString("yyyy-MM-dd"),
            WeeklyWage = 0,
            StartDate = startDate
        };
        return player;
    }

    private GeneratedPlayer GeneratePlayerWithPosition(string position, string? clubName, int? shirtNumber, string? clubJoining, DateOnly referenceDate, int leagueRank = 1, int? fixedAge = null, int? fixedRating = null)
    {
        var nationality = PickWeightedNationality();
        var (firstName, lastName) = GenerateName(nationality);
        var age = fixedAge ?? GenerateAge();
        var birthDate = GenerateBirthDate(age, referenceDate);
        var rating = fixedRating ?? GenerateRating(leagueRank);
        var (height, weight) = GeneratePhysicals(position);
        var preferredFoot = _random.Next(100) < 70 ? "Right" : "Left";

        var player = new GeneratedPlayer
        {
            Name = $"{firstName} {lastName}",
            Nationality = nationality,
            NationalPosition = position,
            ClubPosition = position,
            ShirtNumber = shirtNumber,
            ClubJoining = clubJoining ?? "",
            ContractExpiry = null,
            Rating = rating,
            Height = height,
            Weight = weight,
            PreferredFoot = preferredFoot,
            BirthDate = birthDate,
            Age = age,
            PreferredPosition = GeneratePreferredPosition(position),
            WorkRate = PickRandom(NameData.WorkRates),
            NationalKit = "",
            Weakfoot = _random.Next(1, 6),
            SkillMoves = _random.Next(1, 6)
        };

        ApplyStats(player, position, rating);
        return player;
    }

    private void ApplyStats(GeneratedPlayer player, string position, int rating)
    {
        int baseMin = Math.Max(30, rating - 25);
        int baseMax = Math.Min(99, rating + 10);

        switch (position)
        {
            case "GK":
                ApplyGKStats(player, rating, baseMin, baseMax);
                break;
            case "CB":
            case "LCB":
            case "RCB":
                ApplyDefenderStats(player, rating, baseMin, baseMax);
                break;
            case "LB":
            case "RB":
                ApplyFullBackStats(player, rating, baseMin, baseMax);
                break;
            case "CDM":
                ApplyCDMStats(player, rating, baseMin, baseMax);
                break;
            case "CM":
                ApplyCMStats(player, rating, baseMin, baseMax);
                break;
            case "CAM":
                ApplyCAMStats(player, rating, baseMin, baseMax);
                break;
            case "LM":
            case "RM":
            case "LW":
            case "RW":
                ApplyWingerStats(player, rating, baseMin, baseMax);
                break;
            case "ST":
            case "LS":
            case "LF":
                ApplyStrikerStats(player, rating, baseMin, baseMax);
                break;
            default:
                ApplyCMStats(player, rating, baseMin, baseMax);
                break;
        }
    }

    private void ApplyGKStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.GKPositioning = Stat(rating - 5, rating + 5);
        p.GKDiving = Stat(rating - 8, rating + 5);
        p.GKKicking = Stat(rating - 15, rating + 5);
        p.GKHandling = Stat(rating - 8, rating + 5);
        p.GKReflexes = Stat(rating - 5, rating + 5);

        p.BallControl = Stat(15, 40);
        p.Dribbling = Stat(10, 30);
        p.Marking = Stat(10, 25);
        p.SlidingTackle = Stat(8, 20);
        p.StandingTackle = Stat(8, 20);
        p.Aggression = Stat(25, 60);
        p.Reactions = Stat(rating - 15, rating);
        p.AttackingPosition = Stat(5, 20);
        p.Interceptions = Stat(15, 40);
        p.Vision = Stat(30, 60);
        p.Composure = Stat(rating - 20, rating);
        p.Crossing = Stat(5, 20);
        p.ShortPass = Stat(30, 60);
        p.LongPass = Stat(35, 70);
        p.Acceleration = Stat(30, 60);
        p.Speed = Stat(25, 55);
        p.Stamina = Stat(30, 55);
        p.Strength = Stat(40, 75);
        p.Balance = Stat(30, 55);
        p.Agility = Stat(35, 60);
        p.Jumping = Stat(50, 80);
        p.Heading = Stat(15, 40);
        p.ShotPower = Stat(20, 55);
        p.Finishing = Stat(5, 25);
        p.LongShots = Stat(10, 30);
        p.Curve = Stat(10, 30);
        p.FreekickAccuracy = Stat(10, 30);
        p.Penalties = Stat(15, 40);
        p.Volleys = Stat(10, 30);
    }

    private void ApplyDefenderStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.Marking = Stat(rating - 5, rating + 5);
        p.SlidingTackle = Stat(rating - 8, rating + 5);
        p.StandingTackle = Stat(rating - 5, rating + 5);
        p.Aggression = Stat(rating - 15, rating + 5);
        p.Interceptions = Stat(rating - 8, rating + 5);
        p.Heading = Stat(rating - 10, rating + 5);
        p.Strength = Stat(rating - 10, rating + 5);
        p.Jumping = Stat(rating - 10, rating + 5);
        p.Reactions = Stat(rating - 15, rating);
        p.Composure = Stat(rating - 15, rating);

        p.BallControl = Stat(baseMin - 10, baseMax - 15);
        p.Dribbling = Stat(baseMin - 15, baseMax - 20);
        p.AttackingPosition = Stat(25, 55);
        p.Vision = Stat(baseMin - 10, baseMax - 15);
        p.Crossing = Stat(25, 55);
        p.ShortPass = Stat(baseMin - 5, baseMax - 10);
        p.LongPass = Stat(baseMin - 5, baseMax - 5);
        p.Acceleration = Stat(baseMin - 10, baseMax - 10);
        p.Speed = Stat(baseMin - 10, baseMax - 10);
        p.Stamina = Stat(baseMin, baseMax - 5);
        p.Balance = Stat(baseMin - 10, baseMax - 5);
        p.Agility = Stat(baseMin - 15, baseMax - 15);
        p.ShotPower = Stat(baseMin - 10, baseMax - 10);
        p.Finishing = Stat(20, 50);
        p.LongShots = Stat(25, 55);
        p.Curve = Stat(20, 50);
        p.FreekickAccuracy = Stat(20, 50);
        p.Penalties = Stat(30, 60);
        p.Volleys = Stat(20, 50);

        ApplyLowGKStats(p);
    }

    private void ApplyFullBackStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.Marking = Stat(rating - 10, rating + 3);
        p.SlidingTackle = Stat(rating - 10, rating + 3);
        p.StandingTackle = Stat(rating - 8, rating + 3);
        p.Interceptions = Stat(rating - 10, rating + 3);
        p.Crossing = Stat(rating - 10, rating + 5);
        p.Acceleration = Stat(rating - 8, rating + 5);
        p.Speed = Stat(rating - 8, rating + 5);
        p.Stamina = Stat(rating - 5, rating + 5);
        p.Agility = Stat(rating - 10, rating + 5);

        p.Aggression = Stat(baseMin - 5, baseMax - 5);
        p.Reactions = Stat(baseMin - 5, baseMax - 5);
        p.BallControl = Stat(baseMin - 5, baseMax - 10);
        p.Dribbling = Stat(baseMin - 5, baseMax - 10);
        p.AttackingPosition = Stat(baseMin - 10, baseMax - 10);
        p.Vision = Stat(baseMin - 5, baseMax - 10);
        p.Composure = Stat(baseMin - 5, baseMax - 10);
        p.ShortPass = Stat(baseMin - 5, baseMax - 5);
        p.LongPass = Stat(baseMin - 5, baseMax - 10);
        p.Strength = Stat(baseMin - 5, baseMax - 5);
        p.Balance = Stat(baseMin - 5, baseMax);
        p.Jumping = Stat(baseMin - 5, baseMax - 5);
        p.Heading = Stat(baseMin - 10, baseMax - 10);
        p.ShotPower = Stat(baseMin - 15, baseMax - 15);
        p.Finishing = Stat(20, 50);
        p.LongShots = Stat(20, 50);
        p.Curve = Stat(baseMin - 10, baseMax - 10);
        p.FreekickAccuracy = Stat(20, 50);
        p.Penalties = Stat(25, 55);
        p.Volleys = Stat(20, 50);

        ApplyLowGKStats(p);
    }

    private void ApplyCDMStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.StandingTackle = Stat(rating - 5, rating + 5);
        p.SlidingTackle = Stat(rating - 8, rating + 3);
        p.Marking = Stat(rating - 8, rating + 5);
        p.Interceptions = Stat(rating - 5, rating + 5);
        p.Aggression = Stat(rating - 10, rating + 5);
        p.ShortPass = Stat(rating - 8, rating + 5);
        p.LongPass = Stat(rating - 8, rating + 5);
        p.Stamina = Stat(rating - 8, rating + 5);
        p.Strength = Stat(rating - 10, rating + 5);
        p.Reactions = Stat(rating - 10, rating);
        p.Composure = Stat(rating - 10, rating);

        p.BallControl = Stat(baseMin, baseMax - 5);
        p.Dribbling = Stat(baseMin - 5, baseMax - 10);
        p.AttackingPosition = Stat(baseMin - 10, baseMax - 10);
        p.Vision = Stat(baseMin - 5, baseMax);
        p.Crossing = Stat(baseMin - 10, baseMax - 10);
        p.Acceleration = Stat(baseMin - 5, baseMax - 5);
        p.Speed = Stat(baseMin - 5, baseMax - 5);
        p.Balance = Stat(baseMin - 5, baseMax - 5);
        p.Agility = Stat(baseMin - 5, baseMax - 5);
        p.Jumping = Stat(baseMin - 5, baseMax - 5);
        p.Heading = Stat(baseMin - 5, baseMax - 5);
        p.ShotPower = Stat(baseMin - 5, baseMax - 5);
        p.Finishing = Stat(baseMin - 15, baseMax - 15);
        p.LongShots = Stat(baseMin - 5, baseMax - 5);
        p.Curve = Stat(baseMin - 10, baseMax - 10);
        p.FreekickAccuracy = Stat(baseMin - 10, baseMax - 10);
        p.Penalties = Stat(baseMin - 5, baseMax - 5);
        p.Volleys = Stat(baseMin - 10, baseMax - 10);

        ApplyLowGKStats(p);
    }

    private void ApplyCMStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.ShortPass = Stat(rating - 5, rating + 5);
        p.LongPass = Stat(rating - 8, rating + 5);
        p.Vision = Stat(rating - 8, rating + 5);
        p.BallControl = Stat(rating - 8, rating + 5);
        p.Stamina = Stat(rating - 5, rating + 5);
        p.Reactions = Stat(rating - 10, rating);
        p.Composure = Stat(rating - 10, rating);

        p.Dribbling = Stat(baseMin, baseMax - 5);
        p.Marking = Stat(baseMin - 5, baseMax - 5);
        p.SlidingTackle = Stat(baseMin - 10, baseMax - 10);
        p.StandingTackle = Stat(baseMin - 5, baseMax - 5);
        p.Aggression = Stat(baseMin - 5, baseMax - 5);
        p.AttackingPosition = Stat(baseMin - 5, baseMax - 5);
        p.Interceptions = Stat(baseMin - 5, baseMax - 5);
        p.Crossing = Stat(baseMin - 5, baseMax - 5);
        p.Acceleration = Stat(baseMin - 5, baseMax - 5);
        p.Speed = Stat(baseMin - 5, baseMax - 5);
        p.Strength = Stat(baseMin - 5, baseMax - 5);
        p.Balance = Stat(baseMin - 5, baseMax);
        p.Agility = Stat(baseMin - 5, baseMax);
        p.Jumping = Stat(baseMin - 10, baseMax - 5);
        p.Heading = Stat(baseMin - 10, baseMax - 5);
        p.ShotPower = Stat(baseMin - 5, baseMax);
        p.Finishing = Stat(baseMin - 10, baseMax - 5);
        p.LongShots = Stat(baseMin - 5, baseMax);
        p.Curve = Stat(baseMin - 5, baseMax - 5);
        p.FreekickAccuracy = Stat(baseMin - 5, baseMax - 5);
        p.Penalties = Stat(baseMin - 5, baseMax);
        p.Volleys = Stat(baseMin - 10, baseMax - 5);

        ApplyLowGKStats(p);
    }

    private void ApplyCAMStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.BallControl = Stat(rating - 5, rating + 5);
        p.Dribbling = Stat(rating - 5, rating + 5);
        p.Vision = Stat(rating - 5, rating + 5);
        p.ShortPass = Stat(rating - 5, rating + 5);
        p.Composure = Stat(rating - 8, rating + 3);
        p.Reactions = Stat(rating - 8, rating + 3);
        p.AttackingPosition = Stat(rating - 8, rating + 5);

        p.Crossing = Stat(baseMin, baseMax);
        p.LongPass = Stat(baseMin - 5, baseMax);
        p.Acceleration = Stat(baseMin, baseMax);
        p.Speed = Stat(baseMin - 5, baseMax - 5);
        p.Stamina = Stat(baseMin - 5, baseMax - 5);
        p.Strength = Stat(baseMin - 10, baseMax - 10);
        p.Balance = Stat(baseMin, baseMax);
        p.Agility = Stat(baseMin, baseMax);
        p.Jumping = Stat(baseMin - 15, baseMax - 10);
        p.Heading = Stat(baseMin - 15, baseMax - 10);
        p.ShotPower = Stat(baseMin, baseMax);
        p.Finishing = Stat(baseMin, baseMax);
        p.LongShots = Stat(baseMin, baseMax);
        p.Curve = Stat(baseMin, baseMax);
        p.FreekickAccuracy = Stat(baseMin, baseMax);
        p.Penalties = Stat(baseMin, baseMax);
        p.Volleys = Stat(baseMin - 5, baseMax);
        p.Marking = Stat(20, 45);
        p.SlidingTackle = Stat(15, 40);
        p.StandingTackle = Stat(20, 45);
        p.Aggression = Stat(baseMin - 10, baseMax - 10);
        p.Interceptions = Stat(25, 55);
        p.SkillMoves = Math.Max(p.SkillMoves, _random.Next(3, 6));

        ApplyLowGKStats(p);
    }

    private void ApplyWingerStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.Acceleration = Stat(rating - 3, rating + 5);
        p.Speed = Stat(rating - 3, rating + 5);
        p.Dribbling = Stat(rating - 5, rating + 5);
        p.BallControl = Stat(rating - 5, rating + 5);
        p.Crossing = Stat(rating - 5, rating + 5);
        p.Agility = Stat(rating - 5, rating + 5);
        p.Balance = Stat(rating - 8, rating + 5);

        p.Reactions = Stat(baseMin - 5, baseMax);
        p.Composure = Stat(baseMin - 5, baseMax - 5);
        p.AttackingPosition = Stat(baseMin, baseMax);
        p.Vision = Stat(baseMin - 5, baseMax);
        p.ShortPass = Stat(baseMin - 5, baseMax - 5);
        p.LongPass = Stat(baseMin - 10, baseMax - 10);
        p.Stamina = Stat(baseMin - 5, baseMax);
        p.Strength = Stat(baseMin - 15, baseMax - 10);
        p.Jumping = Stat(baseMin - 10, baseMax - 10);
        p.Heading = Stat(baseMin - 15, baseMax - 15);
        p.ShotPower = Stat(baseMin - 5, baseMax);
        p.Finishing = Stat(baseMin - 5, baseMax);
        p.LongShots = Stat(baseMin - 5, baseMax - 5);
        p.Curve = Stat(baseMin, baseMax);
        p.FreekickAccuracy = Stat(baseMin - 5, baseMax - 5);
        p.Penalties = Stat(baseMin - 5, baseMax - 5);
        p.Volleys = Stat(baseMin - 10, baseMax - 5);
        p.Marking = Stat(15, 40);
        p.SlidingTackle = Stat(15, 40);
        p.StandingTackle = Stat(15, 45);
        p.Aggression = Stat(baseMin - 15, baseMax - 15);
        p.Interceptions = Stat(20, 45);
        p.SkillMoves = Math.Max(p.SkillMoves, _random.Next(2, 6));

        ApplyLowGKStats(p);
    }

    private void ApplyStrikerStats(GeneratedPlayer p, int rating, int baseMin, int baseMax)
    {
        p.Finishing = Stat(rating - 3, rating + 5);
        p.AttackingPosition = Stat(rating - 5, rating + 5);
        p.ShotPower = Stat(rating - 5, rating + 5);
        p.Heading = Stat(rating - 10, rating + 5);
        p.Composure = Stat(rating - 8, rating + 3);
        p.Reactions = Stat(rating - 8, rating + 3);
        p.Volleys = Stat(rating - 10, rating + 5);

        p.BallControl = Stat(baseMin, baseMax);
        p.Dribbling = Stat(baseMin - 5, baseMax);
        p.Vision = Stat(baseMin - 5, baseMax - 5);
        p.Crossing = Stat(baseMin - 10, baseMax - 10);
        p.ShortPass = Stat(baseMin - 5, baseMax - 5);
        p.LongPass = Stat(baseMin - 15, baseMax - 15);
        p.Acceleration = Stat(baseMin, baseMax);
        p.Speed = Stat(baseMin - 5, baseMax);
        p.Stamina = Stat(baseMin - 5, baseMax - 5);
        p.Strength = Stat(baseMin - 5, baseMax);
        p.Balance = Stat(baseMin - 5, baseMax);
        p.Agility = Stat(baseMin - 5, baseMax);
        p.Jumping = Stat(baseMin - 5, baseMax);
        p.LongShots = Stat(baseMin - 5, baseMax);
        p.Curve = Stat(baseMin - 5, baseMax - 5);
        p.FreekickAccuracy = Stat(baseMin - 5, baseMax - 5);
        p.Penalties = Stat(baseMin, baseMax);
        p.Marking = Stat(15, 35);
        p.SlidingTackle = Stat(10, 30);
        p.StandingTackle = Stat(15, 35);
        p.Aggression = Stat(baseMin - 10, baseMax - 5);
        p.Interceptions = Stat(15, 40);

        ApplyLowGKStats(p);
    }

    private void ApplyLowGKStats(GeneratedPlayer p)
    {
        p.GKPositioning = Stat(5, 18);
        p.GKDiving = Stat(3, 15);
        p.GKKicking = Stat(5, 18);
        p.GKHandling = Stat(3, 15);
        p.GKReflexes = Stat(3, 15);
    }

    private int Stat(int min, int max)
    {
        min = Math.Clamp(min, 1, 99);
        max = Math.Clamp(max, min, 99);
        return _random.Next(min, max + 1);
    }

    private int GenerateRating(int leagueRank = 1)
    {
        var roll = _random.Next(100);

        return leagueRank switch
        {
            1 => roll switch
            {
                < 3 => _random.Next(55, 62),
                < 10 => _random.Next(62, 70),
                < 30 => _random.Next(70, 77),
                < 60 => _random.Next(77, 84),
                < 88 => _random.Next(84, 90),
                _ => _random.Next(90, 95)
            },
            2 => roll switch
            {
                < 5 => _random.Next(50, 58),
                < 20 => _random.Next(58, 65),
                < 50 => _random.Next(65, 72),
                < 80 => _random.Next(72, 78),
                < 95 => _random.Next(78, 83),
                _ => _random.Next(83, 87)
            },
            3 => roll switch
            {
                < 8 => _random.Next(45, 53),
                < 25 => _random.Next(53, 60),
                < 55 => _random.Next(60, 67),
                < 82 => _random.Next(67, 73),
                < 96 => _random.Next(73, 78),
                _ => _random.Next(78, 82)
            },
            4 => roll switch
            {
                < 10 => _random.Next(40, 48),
                < 30 => _random.Next(48, 55),
                < 60 => _random.Next(55, 62),
                < 85 => _random.Next(62, 68),
                < 97 => _random.Next(68, 73),
                _ => _random.Next(73, 77)
            },
            5 => roll switch
            {
                < 12 => _random.Next(35, 43),
                < 35 => _random.Next(43, 50),
                < 65 => _random.Next(50, 56),
                < 88 => _random.Next(56, 62),
                < 98 => _random.Next(62, 67),
                _ => _random.Next(67, 71)
            },
            _ => roll switch
            {
                < 15 => _random.Next(30, 38),
                < 38 => _random.Next(38, 45),
                < 68 => _random.Next(45, 51),
                < 90 => _random.Next(51, 57),
                < 98 => _random.Next(57, 62),
                _ => _random.Next(62, 66)
            }
        };
    }

    private int GenerateAge()
    {
        var roll = _random.Next(100);
        return roll switch
        {
            < 10 => _random.Next(17, 20),
            < 35 => _random.Next(20, 24),
            < 65 => _random.Next(24, 28),
            < 85 => _random.Next(28, 32),
            _ => _random.Next(32, 38)
        };
    }

    // Picks a birth date that makes the player exactly `age` years old on the reference date.
    private string GenerateBirthDate(int age, DateOnly referenceDate)
    {
        var birthDate = referenceDate.AddYears(-age).AddDays(-_random.Next(0, 365));
        return birthDate.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
    }

    private (string Height, string Weight) GeneratePhysicals(string position)
    {
        int heightCm;
        int weightKg;

        switch (position)
        {
            case "GK":
                heightCm = _random.Next(183, 200);
                weightKg = _random.Next(78, 95);
                break;
            case "CB":
            case "LCB":
            case "RCB":
                heightCm = _random.Next(178, 196);
                weightKg = _random.Next(75, 92);
                break;
            case "ST":
            case "LS":
            case "LF":
                heightCm = _random.Next(175, 195);
                weightKg = _random.Next(72, 90);
                break;
            case "CDM":
            case "CM":
                heightCm = _random.Next(173, 190);
                weightKg = _random.Next(68, 85);
                break;
            default:
                heightCm = _random.Next(168, 185);
                weightKg = _random.Next(62, 80);
                break;
        }

        return ($"{heightCm} cm", $"{weightKg} kg");
    }

    private string GeneratePreferredPosition(string position)
    {
        if (_random.Next(100) < 30)
        {
            var secondary = position switch
            {
                "GK" => "GK",
                "CB" => PickRandom(["CDM", "LB", "RB"]),
                "LB" => PickRandom(["LM", "LW"]),
                "RB" => PickRandom(["RM", "RW"]),
                "CDM" => PickRandom(["CM", "CB"]),
                "CM" => PickRandom(["CDM", "CAM"]),
                "CAM" => PickRandom(["CM", "LW", "RW"]),
                "LM" => PickRandom(["LW", "LB"]),
                "RM" => PickRandom(["RW", "RB"]),
                "LW" => PickRandom(["ST", "LM"]),
                "RW" => PickRandom(["ST", "RM"]),
                "ST" => PickRandom(["LW", "RW", "CAM"]),
                _ => position
            };
            return secondary == position ? position : $"{position}/{secondary}";
        }
        return position;
    }

    private (string First, string Last) GenerateName(string nationality)
    {
        if (NameData.NationalityNames.TryGetValue(nationality, out var names))
        {
            return (PickRandom(names.FirstNames), PickRandom(names.LastNames));
        }

        var fallbackFirst = PickRandom(NameData.NationalityNames["England"].FirstNames);
        var fallbackLast = PickRandom(NameData.NationalityNames["England"].LastNames);
        return (fallbackFirst, fallbackLast);
    }

    private static readonly (string Nationality, int Weight)[] WeightedNationalities =
    [
        ("England", 160), ("Spain", 100), ("France", 100), ("Germany", 90), ("Italy", 80),
        ("Brazil", 90), ("Argentina", 80), ("Portugal", 60), ("Netherlands", 50), ("Belgium", 40),
        ("Scotland", 35), ("Republic of Ireland", 35), ("Wales", 25), ("Colombia", 40),
        ("Mexico", 30), ("United States", 30), ("Japan", 30), ("Korea Republic", 25),
        ("Denmark", 30), ("Sweden", 30), ("Norway", 30), ("Poland", 30), ("Turkey", 25),
        ("Russia", 25), ("Croatia", 20), ("Switzerland", 20), ("Austria", 20), ("Australia", 18),
        ("Nigeria", 18), ("Ghana", 15), ("Ivory Coast", 15), ("Senegal", 15), ("Cameroon", 12),
        ("Uruguay", 15), ("Chile", 15), ("Saudi Arabia", 15), ("Czech Republic", 12),
        ("Romania", 12), ("Serbia", 12), ("Greece", 12), ("Ukraine", 12), ("Morocco", 12),
        ("Algeria", 10), ("Tunisia", 10), ("China PR", 10), ("Paraguay", 10), ("Ecuador", 10),
        ("Peru", 10), ("Venezuela", 8), ("Bolivia", 5), ("Costa Rica", 8), ("Jamaica", 5),
        ("Honduras", 5), ("Northern Ireland", 8), ("Finland", 8), ("Iceland", 5), ("Hungary", 8),
        ("India", 5)
    ];

    private static readonly int TotalWeight = WeightedNationalities.Sum(n => n.Weight);

    private string PickWeightedNationality()
    {
        var roll = _random.Next(TotalWeight);
        int cumulative = 0;
        foreach (var (nationality, weight) in WeightedNationalities)
        {
            cumulative += weight;
            if (roll < cumulative)
                return nationality;
        }
        return "England";
    }

    private T PickRandom<T>(T[] items) => items[_random.Next(items.Length)];
}
