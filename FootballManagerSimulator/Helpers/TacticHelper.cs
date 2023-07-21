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

    public void ResetTacticForTeam(Team team)
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
                TacticSlotType = TacticSlotType.DEF
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.DEF
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.DEF
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.DEF
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.MID
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.MID
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.MID
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.MID
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.FWD
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.FWD
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

        var players = State.Teams.Where(p => p == team).First().Players;

        foreach(var player in players)
        {
            team.TacticSlots.Add(new TacticSlot
            {
                Player = player,
                TacticSlotType = TacticSlotType.RES
            });
        }
    }

    public void PickTacticSlots(Team team)
    {
        var players = State.Teams.Where(p => p == team).First().Players;

        // Move players from the reserves into the starting 11
        foreach(var slot in team.TacticSlots
            .Where(p => new[] { TacticSlotType.GK, TacticSlotType.DEF, TacticSlotType.MID, TacticSlotType.FWD }.Contains(p.TacticSlotType)))
        {
            var position = ResolvePosition(slot.TacticSlotType);

            var preferredPlayers = players.Where(p => p.Position == position);
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

    private PlayerPosition ResolvePosition(TacticSlotType tacticSlotType)
    {
        if (tacticSlotType == TacticSlotType.GK) return PlayerPosition.GK;
        if (tacticSlotType == TacticSlotType.DEF) return PlayerPosition.DEF;
        if (tacticSlotType == TacticSlotType.MID) return PlayerPosition.MID;
        return PlayerPosition.FWD;
    }
}
