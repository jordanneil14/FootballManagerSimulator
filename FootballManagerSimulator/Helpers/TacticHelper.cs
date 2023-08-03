using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class TacticHelper : ITacticHelper
{
    private readonly IState State;

    public TacticHelper(IState state)
    {
        State = state;
    }

    public void ResetTacticForTeam(Club team)
    {
        team.TacticSlots = new List<TacticSlot>()
        {
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.GK
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.RB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.LB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.LM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.RM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            }
        };

        var players = State.Clubs.Where(p => p == team).First().Players;

        foreach(var player in players)
        {
            team.TacticSlots.Add(new TacticSlot
            {
                Player = player,
                TacticSlotType = TacticSlotType.RES
            });
        }
    }

    public void PickTacticSlots(Club team)
    {
        var players = State.Clubs.Where(p => p == team).First().Players;

        // Move players from the reserves into the starting 11
        var playingTacticSlots = team.TacticSlots.Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB);

        foreach(var slot in playingTacticSlots)
        {
            var position = ResolvePosition(slot.TacticSlotType);


            var preferredPlayers = players.Where(p => p.Position.Split("/").Contains(position.ToString()));
            var reserves = team.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES && p.Player != null);
            var availablePreferredPlayers = reserves.Where(p => preferredPlayers.Any(q => q == p.Player));

            slot.Player = availablePreferredPlayers?.OrderByDescending(p => p.Player.Rating).FirstOrDefault()?.Player;
            team.TacticSlots.Where(p => p.Player == slot.Player).Skip(1).First().Player = null;
        }

        // Move the remaining players from the reserves into the SUB positions
        foreach(var slot in team.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.SUB))
        {
            var reserves = team.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES && p.Player != null);
            slot.Player = reserves.OrderByDescending(p => p.Player.Rating).FirstOrDefault()?.Player;
            team.TacticSlots.Where(p => p.Player == slot.Player).Skip(1).First().Player = null;
        }
    }

    private static Position ResolvePosition(TacticSlotType tacticSlotType)
    {
        Enum.TryParse(tacticSlotType.ToString(), out Position position);
        return position;
    }
}
