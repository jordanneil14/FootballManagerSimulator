using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class TacticHelper(
    IState state,
    IPlayerHelper playerHelper) : ITacticHelper
{
    public void ResetTacticForClub(Club club)
    {
        club.TacticSlots = club.Formation switch
        {
            "4-3-3" => new List<TacticSlot>()
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
                        TacticSlotType = TacticSlotType.CM
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
                        TacticSlotType = TacticSlotType.RW
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
                        TacticSlotType = TacticSlotType.LW
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
                },
            "4-5-1" => new List<TacticSlot>()
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
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlot
                    {
                        Id = 10,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LM
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
                },
            "4-1-2-1-2" => new List<TacticSlot>()
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
                        TacticSlotType = TacticSlotType.CDM
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
                        TacticSlotType = TacticSlotType.CAM
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
                },
            _ => new List<TacticSlot>()
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
                },
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

    private void FillEmptyFirstElevenTacticSlotsByClubId(int clubId)
    {
        var players = state.Players.Where(p => p.Contract != null && p.Contract.ClubId == clubId);

        // Move players from the reserves into the starting 11
        var tacticSlots = state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB && p.PlayerId == null);

        foreach (var slot in tacticSlots)
        {
            var position = ResolvePosition(slot.TacticSlotType);

            var preferredPlayers = players.Where(p => p.PreferredPosition.Split("/").Contains(position.ToString())).ToList();

            var fallbackPosition = GetFallbackPosition(position);
            var secondaryPlayers = players.Where(p => p.PreferredPosition.Split("/").Contains(fallbackPosition.ToString()));
            preferredPlayers.AddRange(secondaryPlayers);
            if (!preferredPlayers.Any()) continue;

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

        var emptySlots = state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB && p.PlayerId == null);

        foreach (var emptySlot in emptySlots)
        {
            var selectedPlayer = state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .FirstOrDefault(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);
            if (selectedPlayer == null) continue;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == emptySlot.Id)
                .PlayerId = selectedPlayer.PlayerId;

            state.Clubs.First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == emptySlot.PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }
    }

    private void FillEmptySubTacticSlotsByClubId(int clubId)
    {
        // Move the remaining players from the reserves into the SUB positions
        var tacticSlots = state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType == TacticSlotType.SUB && p.PlayerId == null)
            .ToList();

        for (var i = 0; i < tacticSlots.Count; i++)
        {
            var reserves = state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null)
                .Select(p => new
                {
                    TacticSlot = p,
                    Player = playerHelper.GetPlayerById(p.PlayerId.Value)
                });

            if (!reserves.Any())
                return;

            int? playerId = null;
            if (i == 0)
                playerId = reserves?.FirstOrDefault(p => p.Player.PreferredPosition == "GK").Player?.Id;

            if (playerId == null)
                playerId = reserves!.First().Player?.Id;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == tacticSlots.ElementAt(i).Id)
                .PlayerId = playerId;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == playerId)
                .Skip(1)
                .First().PlayerId = null;
        }
    }

    private void FillEmptyReserveSlotsByClubId(int clubId)
    {
        var reservePlayers = state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);

        for (var i = 0; i < reservePlayers.Count(); i++)
        {
            var lowerSlotId = state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId == null)
                .OrderBy(p => p.Id)
                .FirstOrDefault()?.Id;

            if (lowerSlotId == null)
                continue;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == lowerSlotId)
                .PlayerId = reservePlayers.ElementAt(i).PlayerId;

            state.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == reservePlayers.ElementAt(i).PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }
    }

    public void FillEmptyTacticSlotsByClubId(int clubId)
    {
        FillEmptyFirstElevenTacticSlotsByClubId(clubId);
        FillEmptySubTacticSlotsByClubId(clubId);
        FillEmptyReserveSlotsByClubId(clubId);
    }

    private static Position GetFallbackPosition(Position position)
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
