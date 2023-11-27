using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using System.Numerics;

namespace FootballManagerSimulator.Helpers;

public class TacticHelper : ITacticHelper
{
    private readonly IState State;

    public TacticHelper(IState state)
    {
        State = state;
    }

    public void ResetTacticForClub(Club club)
    {
        club.TacticSlots = new List<TacticSlot>()
        {
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.GK
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.RB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.LB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.LM
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.RM
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerID = null,
                TacticSlotType = TacticSlotType.SUB
            }
        };


        var myPlayers = State.Players.Where(p => p.Contract != null && p.Contract.Club.ID == club.ID);

        var reserveSlots = myPlayers.Select(p => new TacticSlot
        {
            PlayerID = p.ID,
            TacticSlotType = TacticSlotType.RES
        });

        club.TacticSlots.AddRange(reserveSlots);
    }

    public void FillEmptyTacticSlotsByClub(Club club)
    {
        var players = State.Players.Where(p => p.Contract != null && p.Contract.Club.ID == club.ID);

        // Move players from the reserves into the starting 11
        var playingTacticSlots = club.TacticSlots.Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB);

        foreach(var slot in playingTacticSlots)
        {
            var position = ResolvePosition(slot.TacticSlotType);

            var preferredPlayers = players.Where(p => p.Position.Split("/").Contains(position.ToString()));
            var reserves = club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerID != null);
            var availablePreferredPlayers = reserves.Where(p => preferredPlayers.Any(q => q.ID == p.PlayerID));


            slot.PlayerID = availablePreferredPlayers?
                .FirstOrDefault()?
                .PlayerID;

            club.TacticSlots.Where(p => p.PlayerID == slot.PlayerID).Skip(1).First().PlayerID = null;
        }

        // Move the remaining players from the reserves into the SUB positions
        foreach (var slot in club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.SUB))
        {
            var reserves = club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerID != null);
            slot.PlayerID = reserves
                .FirstOrDefault()?.PlayerID;
            club.TacticSlots.Where(p => p.PlayerID == slot.PlayerID).Skip(1).First().PlayerID = null;
        }
    }

    private static Position ResolvePosition(TacticSlotType tacticSlotType)
    {
        Enum.TryParse(tacticSlotType.ToString(), out Position position);
        return position;
    }
}
