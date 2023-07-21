using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Player : IPlayer
{
    public static Player FromPlayerItem(SerialisablePlayerModel item, int? index, Team? team)
    {
        var player = new Player
        {
            Age = new Random().Next(16, 33),
            ID = item.ID,
            Name = item.Name,
            Position = AssignPosition(),
            Rating = item.Rating,
            ShirtNumber = index.GetValueOrDefault(),
            Contract = team == null ? null : new PlayerContract
            {
                Team = team,
            }
        };
        team?.AddPlayer(player);
        return player;
    }

    public void AddToTeam(Team team)
    {
        team.AddPlayer(this);
    }

    private static PlayerPosition AssignPosition()
    {
        var rand = new Random().Next(0, 6);
        if (rand ==  0) { return PlayerPosition.GK; }
        if (rand == 1 || rand == 2) { return PlayerPosition.DEF;  }
        if (rand == 3 || rand == 4) { return PlayerPosition.MID; }
        return PlayerPosition.FWD;
    }

    public int ID { get; set; }
    public string Name { get; set; } = "";
    public int Rating { get; set; }
    public int ShirtNumber { get; set; }
    public int Age { get; set; }
    public PlayerPosition Position { get; set; }

    // Only players which are signed to a team will have a contract
    public PlayerContract? Contract { get; set; }
    public override string ToString()
    {
        return $"{Name}";
    }

    public SerialisablePlayerModel SerialisablePlayer()
    {
        return new SerialisablePlayerModel
        {
            Contract = Contract?.SerialisablePlayerContract(),
            Age = Age,
            ID = ID, 
            Name = Name,
            Position = Position,
            Rating = Rating,
            ShirtNumber = ShirtNumber
        };
    }

    public class SerialisablePlayerModel
    {
        public PlayerContract.SerialisablePlayerContractModel? Contract { get; set; }
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public int Rating { get; set; }
        public int ShirtNumber { get; set; }
        public int Age { get; set; }
        public PlayerPosition Position { get; set; }

    }
}
