using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class TacticHelper(
    IState state) : ITacticHelper
{
    public void ResetTacticForClub(Club club)
    {
        club.TacticSlots = new List<TacticSlot>()
        {
            new TacticSlot
            {
                Id = 1,
                PlayerId = null,
                TacticSlotType = TacticSlotType.GK
            },
            new TacticSlot
            {   
                Id = 2,
                PlayerId = null,
                TacticSlotType = TacticSlotType.RB
            },
            new TacticSlot
            {
                Id = 3,
                PlayerId = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                Id = 4,
                PlayerId = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                Id = 5,
                PlayerId = null,
                TacticSlotType = TacticSlotType.LB
            },
            new TacticSlot
            {
                Id = 6,
                PlayerId = null,
                TacticSlotType = TacticSlotType.RM
            },
            new TacticSlot
            {
                Id = 7,
                PlayerId = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Id = 8,
                PlayerId = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Id = 9,
                PlayerId = null,
                TacticSlotType = TacticSlotType.LM
            },
            new TacticSlot
            {
                Id = 10,
                PlayerId = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                Id = 11,
                PlayerId = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                Id = 12,
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Id = 13,
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Id = 14,
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Id = 15,
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Id = 16,
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Id = 17,
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Id = 18,
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            }
        };

        for (var i = 19; i <= 99; i++)
        {
            club.TacticSlots.Add(new TacticSlot
            {
                Id = i,
                PlayerId = null,
                TacticSlotType = TacticSlotType.RES
            });
        }

        var players = state.Players.Where(p => p.Contract != null && p.Contract.ClubId == club.Id).ToList();
        foreach (var player in players)
        {
            var tacticSlot = club.TacticSlots.FirstOrDefault(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId == null);
            if (tacticSlot == null) break;
            tacticSlot.PlayerId = player.Id;
        }
    }

    public void FillEmptyTacticSlotsByClubId(int clubId)
    {
        var players = state.Players.Where(p => p.Contract != null && p.Contract.ClubId == clubId);

        if (clubId == 16)
        {

        }

        // Move players from the reserves into the starting 11
        var playingTacticSlots = state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB);

        foreach(var slot in playingTacticSlots)
        {
            if (slot.PlayerId != null) continue;

            var position = ResolvePosition(slot.TacticSlotType);
 
            var preferredPlayers = players.Where(p => p.PreferredPosition.Split("/").Contains(position.ToString())).ToList();
            if (!preferredPlayers.Any())
            {
                var fallbackPosition = GetFallbackPosition(position);
                var secondaryPlayers = players.Where(p => p.PreferredPosition.Split("/").Contains(fallbackPosition.ToString()));
                if (!secondaryPlayers.Any()) continue;
                preferredPlayers.AddRange(secondaryPlayers);
            }

            var reserves = state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);

            var selectedPlayer = reserves.Join(preferredPlayers, arg => arg.PlayerId, arg2 => arg2.Id,
                (first, second) => new
                {
                    first.PlayerId,
                    second.Rating,
                }).OrderByDescending(p => p.Rating).FirstOrDefault();

            if (selectedPlayer == null) continue;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == slot.Id)
                .PlayerId = selectedPlayer.PlayerId;

            state.Clubs.First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == slot.PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }

        // Move the remaining players from the reserves into the SUB positions
        var subSlots = state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType == TacticSlotType.SUB);

        foreach (var subSlot in subSlots)
        {
            var reserves = state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == subSlot.Id)
                .PlayerId = reserves.First().PlayerId;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == reserves.First().PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }

        // Fill empty playing slots which couldnt be resolved above
        var emptyPlayingSlots = state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB && p.PlayerId == null);

        foreach(var emptyPlayingSlot in emptyPlayingSlots)
        {
            var reserves = state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == emptyPlayingSlot.Id)
                .PlayerId = reserves.First().PlayerId;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == reserves.First().PlayerId)
                .First().PlayerId = null;
        }
    }

    private Position GetFallbackPosition(Position position)
    {
        return position switch
        {
            Position.RM => Position.RW,
            Position.RW => Position.RM,
            Position.LM => Position.LW,
            Position.LW => Position.LM,
            _ => position,
        };
    }

    private static Position ResolvePosition(TacticSlotType tacticSlotType)
    {
        Enum.TryParse(tacticSlotType.ToString(), out Position position);
        return position;
    }
}
