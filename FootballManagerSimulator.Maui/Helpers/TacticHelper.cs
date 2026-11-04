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

    public static readonly string[] Formations = ["4-4-2", "4-3-3", "4-5-1", "4-1-2-1-2", "4-2-3-1", "4-2-2-2", "4-4-1-1", "3-4-3", "3-4-1-2", "3-5-2", "5-2-1-2", "5-4-1", "5-3-2"];

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
                        TacticSlotType = TacticSlotType.ST
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
            "4-2-3-1" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.RB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.LB },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.RW },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.CAM },
                    new TacticSlotModel { Id = 10, PlayerId = null, TacticSlotType = TacticSlotType.LW },
                    new TacticSlotModel { Id = 11, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 12, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 13, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 14, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 15, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 16, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 17, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 18, PlayerId = null, TacticSlotType = TacticSlotType.SUB }
                ],
            "4-2-2-2" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.RB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.LB },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.CDM },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CDM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.RW },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.LW },
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
            "4-4-1-1" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.RB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.LB },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.RM },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.LM },
                    new TacticSlotModel { Id = 10, PlayerId = null, TacticSlotType = TacticSlotType.CAM },
                    new TacticSlotModel { Id = 11, PlayerId = null, TacticSlotType = TacticSlotType.ST },
                    new TacticSlotModel { Id = 12, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 13, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 14, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 15, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 16, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 17, PlayerId = null, TacticSlotType = TacticSlotType.SUB },
                    new TacticSlotModel { Id = 18, PlayerId = null, TacticSlotType = TacticSlotType.SUB }
                ],
            "3-5-2" =>
                [
                    new TacticSlotModel { Id = 1, PlayerId = null, TacticSlotType = TacticSlotType.GK },
                    new TacticSlotModel { Id = 2, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 3, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 4, PlayerId = null, TacticSlotType = TacticSlotType.CB },
                    new TacticSlotModel { Id = 5, PlayerId = null, TacticSlotType = TacticSlotType.RM },
                    new TacticSlotModel { Id = 6, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 7, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 8, PlayerId = null, TacticSlotType = TacticSlotType.CM },
                    new TacticSlotModel { Id = 9, PlayerId = null, TacticSlotType = TacticSlotType.LM },
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

    // The club's contracted players, gathered in one pass over State.Players so team selection
    // doesn't rescan every player in the game for each slot it fills.
    private sealed class Squad
    {
        public required ClubModel Club { get; init; }
        public required List<PlayerModel> Players { get; init; }
        public required Dictionary<int, PlayerModel> PlayersById { get; init; }
        public required HashSet<int> UnavailableIds { get; init; }
    }

    private Squad LoadSquad(int clubId)
    {
        var players = State.Players.Where(p => p.Contract != null && p.Contract.ClubId == clubId).ToList();
        // TryAdd keeps the first player on a duplicate id, matching GetPlayerById.
        var playersById = new Dictionary<int, PlayerModel>();
        foreach (var player in players)
            playersById.TryAdd(player.Id, player);

        return new Squad
        {
            Club = State.Clubs.First(p => p.Id == clubId),
            Players = players,
            PlayersById = playersById,
            UnavailableIds = players
                .Where(p => p.SuspensionMatches > 0 || p.IsInjured(State.Date))
                .Select(p => p.Id)
                .ToHashSet()
        };
    }

    // Slots can hold a player who has since left the club, so fall back to a full lookup.
    private PlayerModel? FindPlayer(Squad squad, int playerId) =>
        squad.PlayersById.TryGetValue(playerId, out var player) ? player : PlayerHelper.GetPlayerById(playerId);

    // Fills the empty starting slots from the reserves. Each step makes the single best remaining
    // pick: the reserve and slot with the highest effective rating (rating after any out-of-position
    // cut), so a natural full-back beats a much better centre-midfielder at right-back. A keeper is
    // never put outfield (he's kept for the bench); an outfielder only goes in goal if no keeper is free.
    private void FillEmptyFirstElevenTacticSlots(Squad squad)
    {
        var unavailableIds = squad.UnavailableIds;
        var club = squad.Club;

        var emptySlots = club.TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB && p.PlayerId == null)
            .ToList();

        var candidates = new List<ReserveCandidate>();
        foreach (var slot in club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId != null && !unavailableIds.Contains(p.PlayerId.Value)))
        {
            if (FindPlayer(squad, slot.PlayerId!.Value) is { } player)
                candidates.Add(new ReserveCandidate(slot, player));
        }

        while (emptySlots.Count > 0 && candidates.Count > 0)
        {
            var best = emptySlots
                .SelectMany(slot => candidates
                    .Where(candidate => slot.TacticSlotType == TacticSlotType.GK || !IsGoalkeeper(candidate.Player))
                    .Select(candidate =>
                {
                    var familiarity = PositionFamiliarityRules.GetFamiliarity(candidate.Player, slot.TacticSlotType);
                    return new
                    {
                        Slot = slot,
                        Candidate = candidate,
                        Familiarity = familiarity,
                        Effective = PositionFamiliarityRules.EffectiveRating(candidate.Player, slot.TacticSlotType)
                    };
                }))
                .OrderBy(p => p.Familiarity == PositionFamiliarity.GoalkeeperSwap)
                .ThenByDescending(p => p.Effective)
                .ThenBy(p => p.Familiarity)
                .FirstOrDefault();

            if (best == null) break;

            best.Slot.PlayerId = best.Candidate.Player.Id;
            best.Candidate.Slot.PlayerId = null;
            emptySlots.Remove(best.Slot);
            candidates.Remove(best.Candidate);
        }
    }

    private static bool IsGoalkeeper(PlayerModel player) =>
        player.PreferredPosition.Split('/', StringSplitOptions.TrimEntries).Contains("GK");

    private void FillEmptySubTacticSlots(Squad squad)
    {
        var unavailableIds = squad.UnavailableIds;
        var club = squad.Club;

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

            var bestPlayer = AvailableReserves(squad, assignedIds)
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
            var bestRemaining = AvailableReserves(squad, assignedIds)
                .OrderByDescending(p => p.Player.Rating)
                .FirstOrDefault();

            if (bestRemaining == null) break;

            emptySlot.PlayerId = bestRemaining.Player.Id;
            bestRemaining.Slot.PlayerId = null;
            assignedIds.Add(bestRemaining.Player.Id);
        }
    }

    // Reserves who can be named as a substitute: not suspended and not already picked.
    private IEnumerable<ReserveCandidate> AvailableReserves(Squad squad, HashSet<int> assignedIds)
    {
        foreach (var slot in squad.Club.TacticSlots.Where(p => p.TacticSlotType == TacticSlotType.RES))
        {
            if (slot.PlayerId is not { } playerId || squad.UnavailableIds.Contains(playerId) || assignedIds.Contains(playerId))
                continue;

            if (FindPlayer(squad, playerId) is { } player)
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
        var squad = LoadSquad(clubId);
        FillEmptyFirstElevenTacticSlots(squad);
        FillEmptySubTacticSlots(squad);
        FillEmptyReserveSlotsByClubId(clubId);
    }
}
