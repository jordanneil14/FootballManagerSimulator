using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class TacticHelper(
    IState state,
    IPlayerHelper playerHelper) : ITacticHelper
{
    private readonly IState State = state;
    private readonly IPlayerHelper PlayerHelper = playerHelper;

    public static readonly string[] Formations = ["4-4-2", "4-3-3", "4-5-1", "4-1-2-1-2", "3-4-3", "3-4-1-2", "5-2-1-2", "5-4-1", "5-3-2"];

    public void ResetTacticForClub(ClubModel club)
    {
        club.TacticSlots = club.Formation switch
        {
            "4-3-3" =>
                [
                    new TacticSlotModel
                    {
                        Id = 1,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.GK
                    },
                    new TacticSlotModel
                    {
                        Id = 2,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.RB
                    },
                    new TacticSlotModel
                    {
                        Id = 3,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 4,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 5,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LB
                    },
                    new TacticSlotModel
                    {
                        Id = 6,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 7,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 8,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 9,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.RW
                    },
                    new TacticSlotModel
                    {
                        Id = 10,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.ST
                    },
                    new TacticSlotModel
                    {
                        Id = 11,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LW
                    },
                    new TacticSlotModel
                    {
                        Id = 12,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 13,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 14,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 15,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 16,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 17,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 18,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    }
                ],
            "4-5-1" =>
                [
                    new TacticSlotModel
                    {
                        Id = 1,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.GK
                    },
                    new TacticSlotModel
                    {
                        Id = 2,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.RB
                    },
                    new TacticSlotModel
                    {
                        Id = 3,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 4,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 5,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LB
                    },
                    new TacticSlotModel
                    {
                        Id = 6,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.RM
                    },
                    new TacticSlotModel
                    {
                        Id = 7,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 8,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 9,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 10,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LM
                    },
                    new TacticSlotModel
                    {
                        Id = 11,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.ST
                    },
                    new TacticSlotModel
                    {
                        Id = 12,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 13,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 14,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 15,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 16,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 17,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 18,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    }
                ],
            "4-1-2-1-2" =>
                [
                    new TacticSlotModel
                    {
                        Id = 1,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.GK
                    },
                    new TacticSlotModel
                    {
                        Id = 2,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.RB
                    },
                    new TacticSlotModel
                    {
                        Id = 3,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 4,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 5,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LB
                    },
                    new TacticSlotModel
                    {
                        Id = 6,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CDM
                    },
                    new TacticSlotModel
                    {
                        Id = 7,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 8,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 9,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CAM
                    },
                    new TacticSlotModel
                    {
                        Id = 10,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.ST
                    },
                    new TacticSlotModel
                    {
                        Id = 11,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.ST
                    },
                    new TacticSlotModel
                    {
                        Id = 12,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 13,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 14,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 15,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 16,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 17,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 18,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    }
                ],
            "3-4-3" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.RM },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.LM },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.RW },
                    new TacticSlotModel { Id = 10, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 11, PlayerId = null, TacticSlotType = TacticSlotType.LW },
                    new TacticSlotModel { Id = 12, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 13, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 14, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 15, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 16, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 17, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 18, PlayerId = null, TacticSlotType = TacticSlotType.SUB }
                ],
            "3-4-1-2" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.RM },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.LM },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.CAM },
                    new TacticSlotModel { Id = 10, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 11, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 12, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 13, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 14, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 15, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 16, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 17, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 18, PlayerId = null, TacticSlotType = TacticSlotType.SUB }
                ],
            "5-2-1-2" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.RB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.LB },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.CAM },
                    new TacticSlotModel { Id = 10, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 11, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 12, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 13, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 14, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 15, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 16, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 17, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 18, PlayerId = null, TacticSlotType = TacticSlotType.SUB }
                ],
            "5-4-1" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.RB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.LB },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.RM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 10, PlayerId = null, TacticSlotType = TacticSlotType.LM },
                    new TacticSlotModel { Id = 11, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 12, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 13, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 14, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 15, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 16, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 17, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 18, PlayerId = null, TacticSlotType = TacticSlotType.SUB }
                ],
            "5-3-2" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.RB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.LB },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 10, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 11, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 12, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 13, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 14, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 15, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 16, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 17, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 18, PlayerId = null, TacticSlotType = TacticSlotType.SUB }
                ],
            _ =>
                [
                    new TacticSlotModel
                    {
                        Id = 1,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.GK
                    },
                    new TacticSlotModel
                    {
                        Id = 2,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.RB
                    },
                    new TacticSlotModel
                    {
                        Id = 3,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 4,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CB
                    },
                    new TacticSlotModel
                    {
                        Id = 5,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LB
                    },
                    new TacticSlotModel
                    {
                        Id = 6,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.RM
                    },
                    new TacticSlotModel
                    {
                        Id = 7,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 8,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.CM
                    },
                    new TacticSlotModel
                    {
                        Id = 9,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.LM
                    },
                    new TacticSlotModel
                    {
                        Id = 10,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.ST
                    },
                    new TacticSlotModel
                    {
                        Id = 11,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.ST
                    },
                    new TacticSlotModel
                    {
                        Id = 12,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 13,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 14,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 15,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 16,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 17,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    },
                    new TacticSlotModel
                    {
                        Id = 18,
                        PlayerId = null,
                        TacticSlotType = TacticSlotType.SUB
                    }
                ],
        };

        for (var i = 19; i <= 99; i++)
        {
            club.TacticSlots.Add(new TacticSlotModel
            {
                Id = i,
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

    private HashSet<int> GetSuspendedPlayerIds(int clubId)
    {
        return State.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == clubId && p.SuspensionMatches > 0)
            .Select(p => p.Id)
            .ToHashSet();
    }

    private void FillEmptyFirstElevenTacticSlotsByClubId(int clubId)
    {
        var suspendedIds = GetSuspendedPlayerIds(clubId);
        var players = State.Players.Where(p => p.Contract != null && p.Contract.ClubId == clubId && !suspendedIds.Contains(p.Id));

        // Move players from the reserves into the starting 11
        var tacticSlots = State.Clubs
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

            var reserves = State.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null && !suspendedIds.Contains(p.PlayerId.Value));

            var selectedPlayer = reserves.Join(preferredPlayers, arg => arg.PlayerId, arg2 => arg2.Id,
                (first, second) => new
                {
                    first.PlayerId,
                    second.Rating,
                }).OrderByDescending(p => p.Rating).FirstOrDefault();

            if (selectedPlayer == null) continue;

            State.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == slot.Id)
                .PlayerId = selectedPlayer.PlayerId;

            State.Clubs.First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == slot.PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }

        var emptySlots = State.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB && p.PlayerId == null);

        foreach (var emptySlot in emptySlots)
        {
            var isGkSlot = emptySlot.TacticSlotType == TacticSlotType.GK;
            var selectedPlayer = State.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null && !suspendedIds.Contains(p.PlayerId.Value))
                .Select(p => new { Slot = p, Player = PlayerHelper.GetPlayerById(p.PlayerId!.Value) })
                .Where(p => p.Player != null && (isGkSlot ? p.Player.PreferredPosition == "GK" : p.Player.PreferredPosition != "GK"))
                .OrderByDescending(p => p.Player!.Rating)
                .FirstOrDefault();
            if (selectedPlayer == null) continue;

            State.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == emptySlot.Id)
                .PlayerId = selectedPlayer.Slot.PlayerId;

            State.Clubs.First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.PlayerId == emptySlot.PlayerId)
                .Skip(1)
                .First().PlayerId = null;
        }
    }

    private void FillEmptySubTacticSlotsByClubId(int clubId)
    {
        var suspendedIds = GetSuspendedPlayerIds(clubId);
        var club = State.Clubs.First(p => p.Id == clubId);

        var subSlots = club.TacticSlots
            .Where(p => p.TacticSlotType == TacticSlotType.SUB && p.PlayerId == null)
            .ToList();

        if (!subSlots.Any()) return;

        var assignedIds = new HashSet<int>();
        var positionGroups = new[] { "GK", "DEF", "MID", "ATT" };

        foreach (var group in positionGroups)
        {
            var emptySlot = subSlots.FirstOrDefault(s => !s.PlayerId.HasValue);
            if (emptySlot == null) break;

            var bestPlayer = AvailableReserves(club, suspendedIds, assignedIds)
                .Where(p => GetPositionGroup(p.Player.PreferredPosition) == group)
                .OrderByDescending(p => p.Player.Rating)
                .FirstOrDefault();

            if (bestPlayer == null) continue;

            emptySlot.PlayerId = bestPlayer.Player.Id;
            bestPlayer.Slot.PlayerId = null;
            assignedIds.Add(bestPlayer.Player.Id);
        }

        foreach (var emptySlot in subSlots.Where(s => !s.PlayerId.HasValue).ToList())
        {
            var bestRemaining = AvailableReserves(club, suspendedIds, assignedIds)
                .OrderByDescending(p => p.Player.Rating)
                .FirstOrDefault();

            if (bestRemaining == null) break;

            emptySlot.PlayerId = bestRemaining.Player.Id;
            bestRemaining.Slot.PlayerId = null;
            assignedIds.Add(bestRemaining.Player.Id);
        }
    }

    // Reserves who can be named as a substitute: not suspended and not already picked.
    private IEnumerable<ReserveCandidate> AvailableReserves(ClubModel club, HashSet<int> suspendedIds, HashSet<int> assignedIds)
    {
        foreach (var slot in club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES))
        {
            if (slot.PlayerId is not { } playerId || suspendedIds.Contains(playerId) || assignedIds.Contains(playerId))
                continue;

            if (PlayerHelper.GetPlayerById(playerId) is { } player)
                yield return new ReserveCandidate(slot, player);
        }
    }

    private sealed record ReserveCandidate(TacticSlotModel Slot, PlayerModel Player);

    private static string GetPositionGroup(string position)
    {
        return position switch
        {
            "GK" => "GK",
            "CB" or "RB" or "LB" => "DEF",
            "CM" or "CDM" or "CAM" or "RM" or "LM" => "MID",
            "ST" or "CF" or "RW" or "LW" or "RF" or "LF" => "ATT",
            _ => "MID"
        };
    }

    private void FillEmptyReserveSlotsByClubId(int clubId)
    {
        var reservePlayers = State.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null);

        for (var i = 0; i < reservePlayers.Count(); i++)
        {
            var lowerSlotId = State.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId == null)
                .OrderBy(p => p.Id)
                .FirstOrDefault()?.Id;

            if (lowerSlotId == null)
                continue;

            State.Clubs
                .First(p => p.Id == clubId)
                .TacticSlots
                .First(p => p.Id == lowerSlotId)
                .PlayerId = reservePlayers.ElementAt(i).PlayerId;

            State.Clubs
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
