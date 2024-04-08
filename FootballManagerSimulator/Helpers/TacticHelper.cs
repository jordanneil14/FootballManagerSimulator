using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class TacticHelper : ITacticHelper
{
    private readonly IState State;

    public TacticHelper(
        IState state)
    {
        State = state;
    }

    public void ResetTacticForClub(Club club)
    {
        club.TacticSlots = new List<TacticSlot>()
        {
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.GK
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.RB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.LB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.RM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.LM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            }
        };

        for (var i = 0; i < 81; i++)
        {
            club.TacticSlots.Add(new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.RES
            });
        }

        var players = State.Players.Where(p => p.Contract != null && p.Contract.ClubId == club.Id).ToList();
        foreach (var player in players)
        {
            var tacticSlot = club.TacticSlots.FirstOrDefault(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId == null);
            if (tacticSlot == null) break;
            tacticSlot.PlayerId = player.Id;
        }
    }

    public void FillEmptyTacticSlotsByClub(Club club)
    {
        var players = State.Players.Where(p => p.Contract != null && p.Contract.ClubId == club.Id);

        // Move players from the reserves into the starting 11
        var playingTacticSlots = club.TacticSlots.Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB);

        foreach(var slot in playingTacticSlots)
        {
            var position = ResolvePosition(slot.TacticSlotType);

            var preferredPlayers = players.Where(p => p.PreferredPosition.Split("/").Contains(position.ToString()));
            var reserves = club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);
            var availablePreferredPlayers = reserves.Where(p => preferredPlayers.Any(q => q.Id == p.PlayerId));

            slot.PlayerId = availablePreferredPlayers?
                .FirstOrDefault()?
                .PlayerId;

            club.TacticSlots
                .Where(p => p.PlayerId == slot.PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }

        // Move the remaining players from the reserves into the SUB positions
        foreach (var slot in club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.SUB))
        {
            var reserves = club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);
            slot.PlayerId = reserves
                .FirstOrDefault()?.PlayerId;

            club.TacticSlots
                .Where(p => p.PlayerId == slot.PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }
    }

    private static Position ResolvePosition(TacticSlotType tacticSlotType)
    {
        Enum.TryParse(tacticSlotType.ToString(), out Position position);
        return position;
    }
}
