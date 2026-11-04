# Spec: Out-of-position rating penalty

## Problem

A player's `Rating` counts in full wherever he is picked. A striker in goal or a centre-back on
the left wing adds as much to the team's strength as he would in his own position. The only thing
that cares about position today is the AI's line-up picker (`TacticHelper.FillEmptyFirstElevenTacticSlots`),
and it falls back to "best-rated non-GK" when no natural fit exists.

## Goal

When a player starts in a slot that is not one of his preferred positions, his rating for that
match is reduced. The further the slot is from what he knows, the bigger the cut. This should:

- make team selection a real decision for the user,
- make AI clubs pick sensibly,
- show the user the cost before kick-off.

## How things work today

| Piece | Where | What it does |
|---|---|---|
| Player positions | `PlayerModel.PreferredPosition` | `"CB"` or `"CB/LB"` (30% of players get a second position from `PlayerGenerator.GeneratePreferredPosition`) |
| Slot positions | `TacticSlotModel.TacticSlotType` | Formations use GK, CB, LB, RB, CDM, CM, CAM, LM, RM, LW, RW, ST; plus SUB/RES |
| Team strength | `ClubHelper.GetStartingElevenSumRatingForClub` | Sum of raw `Rating` for the XI |
| Match odds | `MatchSimulatorHelper.GetAdjustedRatings` | `sum ^ 3`, ×1.06³ for the user, ×0.8 away; ×0.8 per missing player |
| In-match changes | `ReGenerateFromMinute` | Re-reads the current slots, so a sub or reshuffle already changes the odds from that minute |

Because everything goes through `GetStartingElevenSumRatingForClub`, applying the penalty there
covers open play, in-match penalties, and the shoot-out in one place.

## Rules

### 1. Familiarity levels

A player's familiarity with a slot is the **best** match across all positions in his
`PreferredPosition` list.

| Level | Meaning | Rating multiplier |
|---|---|---|
| Natural | Slot is one of his preferred positions | 1.00 |
| Adjacent | Neighbouring role, very similar job | 0.95 |
| Related | Same line or same flank, different job | 0.85 |
| Unfamiliar | Any other outfield slot | 0.70 |
| Goalkeeper swap | Outfielder in GK slot, or GK in an outfield slot | 0.40 |

Effective rating = `round(Rating × multiplier)`.

### 2. Position map (symmetric)

`LS` and `LF` (generator-only labels) count as `ST`.

**Adjacent (0.95)**

- CB – CDM
- CDM – CM
- CM – CAM
- CAM – ST
- LB – LM, RB – RM
- LM – LW, RM – RW
- LW – ST, RW – ST

**Related (0.85)**

- CB – LB, CB – RB, LB – RB
- CDM – CAM
- CM – LM, CM – RM, LM – RM
- LB – LW, RB – RW
- LW – RW
- CAM – LW, CAM – RW

Everything else outfield is **Unfamiliar (0.70)**. GK ↔ any outfield slot is **Goalkeeper swap (0.40)**.

These pairs line up with the secondary positions the generator already hands out, so a
"CB" whose secondary is "CDM" is natural in both, and adjacent-or-better in nearby slots.

### 3. Edge cases

- **SUB / RES slots**: no penalty (they aren't on the pitch).
- **Empty or unrecognised `PreferredPosition`** (old saves, test fixtures): treat as Natural. No penalty.
- **Second listed position** (e.g. the "LB" in "CB/LB") counts as Natural, same as the first.
- The player's displayed `Rating` (squad, scout, transfers, player page) **does not change**. Only
  the match-day contribution does.

### 4. Size of the effect

With a typical XI summing to ~825 and the cube in `GetAdjustedRatings`:

| Example (75-rated player) | Lost points | Team strength change |
|---|---|---|
| Adjacent | −4 | ≈ −1.4% |
| Related | −11 | ≈ −4% |
| Unfamiliar | −22 | ≈ −8% |
| Outfielder in goal | −45 | ≈ −15% |
| *(for comparison) a red card* | — | −20% |

Noticeable, never as bad as playing a man down. All five multipliers are constants so they can be tuned.

## Implementation

### New: `Models/PositionFamiliarityRules.cs`

Static class, in the style of the other `*Rules` classes.

```csharp
public enum PositionFamiliarity { Natural, Adjacent, Related, Unfamiliar, GoalkeeperSwap }

public static class PositionFamiliarityRules
{
    public const double AdjacentMultiplier = 0.95;
    public const double RelatedMultiplier = 0.85;
    public const double UnfamiliarMultiplier = 0.70;
    public const double GoalkeeperSwapMultiplier = 0.40;

    public static PositionFamiliarity GetFamiliarity(string preferredPositions, TacticSlotType slot);
    public static double Multiplier(PositionFamiliarity familiarity);
    public static int EffectiveRating(PlayerModel player, TacticSlotType slot);
}
```

- Parse `PreferredPosition` with `Split('/', TrimEntries | RemoveEmptyEntries)`, map LS/LF → ST.
- Hold the adjacent and related pairs as two `HashSet<(string, string)>` stored in both orders.
- SUB/RES → Natural.

### Change: `ClubHelper.GetStartingElevenSumRatingForClub`

Sum `PositionFamiliarityRules.EffectiveRating(player, slot.TacticSlotType)` instead of `player.Rating`.
Keep the `int` return type. No change needed in `MatchSimulatorHelper`.

### Change: AI selection (`TacticHelper.FillEmptyFirstElevenTacticSlots`)

*(As built.)* The two passes are replaced by one greedy loop: at each step, take the reserve and
empty slot with the highest **effective rating**, until the XI is full or nobody is left. This
replaces `GetFallbackPosition` (RM↔RW, LM↔LW are now just "Adjacent").

- A keeper is **never** put in an outfield slot (he's kept for the bench), as before.
- An outfielder goes in goal only when no keeper is free. Previously the GK slot was left empty.
- With no natural RW, a spare ST can now be picked over an RM, since both are Adjacent and the
  higher effective rating wins.
- Result: an 80-rated CB (Related at RB: 80 × 0.85 = 68) loses out to a 70-rated natural RB,
  and an 80-rated CM (Unfamiliar at RB: 56) is nowhere near.

### Change: UI

*(As built.)* For starters, the player's position and rating columns are coloured by familiarity,
and the rating column shows the effective rating. A tooltip explains the cut, e.g.
*"Out of position (CB at RB): 80 → 68"*.

| Familiarity | Colour | CSS class |
|---|---|---|
| Natural | normal | — |
| Adjacent | yellow `#f2d544` | `fam-adjacent` |
| Related | amber `#f5a524` | `fam-related` |
| Unfamiliar | orange `#f2762e` | `fam-unfamiliar` |
| Goalkeeper swap | red (`--danger`) | `fam-gkswap` |

- **Tactics.razor `SlotRow`**: position and rating columns.
- **Match.razor squad tab**: a position column has been added to the starters table, and the
  rating column now shows the effective rating, so a mid-match reshuffle shows its cost.
- SUB/RES rows are never coloured and show the raw `Rating`.
- Helpers: `Ui.FamiliarityCssClass` and `Ui.FamiliarityTitle`.

## Tests

`PositionFamiliarityRulesTests` (new, in `Tests/Models`):

- Natural for first and second listed positions.
- Each adjacent and related pair, both directions.
- An unlisted outfield pair is Unfamiliar.
- GK in ST slot and ST in GK slot are GoalkeeperSwap.
- LS/LF behave as ST.
- Empty `PreferredPosition` → Natural.
- SUB/RES → Natural.
- `EffectiveRating` rounds as expected (e.g. 75 × 0.85 = 64).

Existing tests to update/extend:

- `SmallHelperTests.GetStartingElevenSumRatingForClub_SumsOnlyFilledStarterSlots`: still passes
  if fixture players have matching/empty positions; add a case with a player out of position.
- `TacticHelperTests`: AI prefers a natural lower-rated player over a much higher-rated
  unfamiliar one; never puts a GK outfield when an outfielder is free.
- *(Not added.)* A `MatchSimulatorHelperTests` check that a club with players out of position
  wins less often. The team-strength sum test covers the input to the odds directly.

Balance check: rerun `DifficultySimulationTests`. AI clubs should barely change (they mostly
pick naturally), but the user's side now pays for bad picks, so watch the difficulty numbers.

## Out of scope (possible follow-ups)

- Players learning new positions by playing there.
- Applying familiarity to individual attributes (e.g. `PositionScoringWeight` for goalscorers).
- Separate penalty for playing on the "wrong" foot (LB with a right-footed player).

## Decisions

1. A second listed position is 100% (Natural), same as the first.
2. The penalty is a percentage, not a flat number of points.
3. No pop-up warning: out-of-position players are shown by colouring their position
   (yellow → amber → orange → red).
