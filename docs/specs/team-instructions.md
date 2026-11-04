# Spec: Team instructions (mentality, pressing and more)

## Problem

The only tactical choices today are the formation and who plays where. The formation is just a
set of slots. It doesn't change how a match plays out, apart from the out-of-position penalty.
Every club plays the same way, and the user has nothing to change mid-match except personnel.

## Goal

Add a small set of **team instructions**. Every setting other than the neutral one must have:

- an **advantage**: in some situations it raises your chance of winning (or of not losing), and
- a **disadvantage**: in other situations, or against certain opponents, it lowers it.

No setting should be best in every situation. The right choice should depend on:

1. **Who you're playing** (stronger or weaker, and what tactics they use),
2. **your players' attributes** (stamina, pace, passing, crossing...), and
3. **the state of the match** (the score and the minute).

## How the engine decides goals today

Everything lives in `MatchSimulatorHelper`.

- The match is walked through in steps of 1–10 minutes. At each step there is a fixed chance of a
  goal (`OpenPlayGoalChance`, about 1/7), which gives roughly 2.4 goals a match.
- When a goal happens, `HomeWinsChance` decides who scored, in proportion to each side's strength:
  `XI rating sum ^ 3`, ×1.06³ for the user's club, ×0.8 for the away side, ×0.8 per missing player.
- Cards: an event roll every 5–15 minutes, with each side equally likely to be carded.
- Injuries: the same pattern, every 8–20 minutes.
- When the user changes the line-up mid-match, `ReGenerateFromMinute` re-rolls everything after the
  current minute. Changing instructions mid-match can use the same mechanism.

So there are two levers: **how many goals** there are, and **who scores them**. Both matter:

- The share of goals each side scores is the obvious lever.
- The **number** of goals is a lever too. Fewer goals means more draws and more upsets, which helps
  the weaker side. More goals lets the stronger side's edge show, which helps the favourite. That
  gives Defensive and Attacking set-ups a natural trade-off, without making either one simply better.

## The model: Threat and Exposure

Each side gets two numbers, recalculated at each minute from its own instructions, the opponent's
instructions, both XIs' attributes and the minute:

- **Threat (T)**: how dangerous the side is going forward. 1.0 is neutral.
- **Exposure (E)**: how open the side is at the back. 1.0 is neutral.

Using today's strength shares `sH` and `sA` (which add up to 1):

```
wH = sH × T_home × E_away     // home side's weight to score
wA = sA × T_away × E_home     // away side's weight to score

goal chance this step = OpenPlayGoalChance × (wH + wA)      (capped at 0.35)
P(home scored | goal) = wH / (wH + wA)
```

With every setting neutral, `wH + wA = 1` and the engine behaves exactly as it does today. That is
the backwards-compatibility guarantee, and a test should pin it.

Reading the formula:

- **T = E** for a side changes the *number* of goals, but not its share. This is a pure risk dial.
- **T > E** improves the side's share of goals: a real edge, which must be paid for elsewhere
  (fatigue, cards, a counter-tactic).
- Each side's T and E are clamped to **[0.5, 1.8]** so combinations can't run away.

Cards and injuries use the same idea, with per-side rate multipliers `C` (cards) and `I` (injuries):

```
event chance = base × (C_home + C_away) / 2
P(home side's player) = C_home / (C_home + C_away)
```

## The instructions

Five settings. The middle option of each is neutral, and it is the default for every club and
every old save.

### 1. Mentality

| Option | T | E | Advantage | Disadvantage |
|---|---|---|---|---|
| Defensive | 0.82 | 0.80 | Fewer goals; you concede noticeably less. | You score less too; a favourite turns wins into draws. |
| **Balanced** | 1.00 | 1.00 | — | — |
| Attacking | 1.18 | 1.22 | More goals, so a stronger side's quality shows and it wins more. Good for chasing a game. | You're more open: an underdog playing like this usually loses more heavily. |

Three levels (decided). Mentality is mostly a risk dial (T ≈ E).

### 2. Pressing

| Option | Effect | Advantage | Disadvantage |
|---|---|---|---|
*(As built, after tuning with `TacticsBalanceSimulationTests`.)*

| Option | Effect | Advantage | Disadvantage |
|---|---|---|---|
| Low | T ×0.94, E ×0.96. Cards ×0.80. After the 60th minute, T ×1.10 against a side pressing high (they're tiring and you're fresh). | No fatigue, fewer cards, sharper late on against a press. | You give the opponent the ball, so you create less. |
| **Medium** | — | — | — |
| High | T ×1.08, E ×0.97 (winning the ball high up the pitch). Cards ×1.35, injuries ×1.20. **Fatigue:** from half-time, T and E drift towards T 0.88 / E 1.18, reaching it by the 75th minute and staying there (including extra time). | The strongest setting for the first half. | Bookings, injuries, and a second-half fade. A side with poor stamina is worse off than if it hadn't pressed at all. |

Fatigue is scaled by the XI's average outfield `Stamina` (players on the pitch at that minute):
`fade = clamp((85 − avgStamina) / 30, 0.3, 1.0)`. A very fit side gets 30% of the full drift;
a side averaging 55 or less gets all of it. Fitness is never a complete fix.

Simulated results (20,000 matches each, evenly matched sides, points per game against Medium):

| Our press | Our stamina | Opponent | PPG change |
|---|---|---|---|
| High | 82 | Medium | +0.06 |
| High | 65 | Medium | ±0.00 |
| High | 45 | Medium | −0.04 |
| Low | 65 | Medium | −0.03 |
| Low (vs Medium against the same press) | 65 | High | +0.02 |

### 3. Passing style

`passQuality` is the XI's outfield average of `ShortPass`, `Vision` and `BallControl`.
`directQuality` is the forwards' average of `Speed`, `Strength` and `Heading`.

| Option | Effect | Advantage | Disadvantage |
|---|---|---|---|
| Possession | E ×0.90, T ×0.95. The E bonus is scaled by `passQuality`: full at 75+, none at 55 or below. | You keep the ball, so the opponent gets fewer chances. | Slower to create. **Countered by a high press**: against High pressing, E ×1.15 instead, unless `passQuality` ≥ 80 (good enough to play through it). |
| **Mixed** | — | — | — |
| Direct | T ×1.08, scaled by `directQuality` (full at 75+, none at 55 or below). E ×1.05 (the ball comes straight back). | **Beats a high press**: it cancels the opponent's pressing T bonus. Suits big, quick forwards. | Gives the ball away. **Countered by a deep line**: against a Deep line, the T bonus is lost (there's no space to run into). |

### 4. Defensive line

`paceGap` = clamp((opponent forwards' average `Speed`/`Acceleration` − your CBs' average) / 20, 0, 1).

| Option | Effect | Advantage | Disadvantage |
|---|---|---|---|
| Deep | T ×0.93. E ×0.92 against a Direct opponent. | Shuts out counter-attacks and long balls. | You're far from goal, so you create less. |
| **Standard** | — | — | — |
| High | E ×0.95, T ×1.04 (compact side). | Pins the opponent back and supports a press. | **Vulnerable to pace**: against a Direct opponent, E ×(1 + 0.25 × `paceGap`). Slow centre-backs get punished. |

### 5. Width

`crossThreat` = average of the wide players' `Crossing` and the strikers' `Heading`.

| Option | Effect | Advantage | Disadvantage |
|---|---|---|---|
| Narrow | T ×0.97, E ×0.95. | Crowds the middle, so you're harder to play through. | Less threat. The E bonus is lost against a Wide opponent. |
| **Balanced** | — | — | — |
| Wide | T × (0.97 + 0.10 × clamp((`crossThreat` − 60) / 20, 0, 1)). E ×1.03. | Up to +7% threat with good crossers and a target man. | Full-backs push up, which leaves gaps. Without the players for it, this is a net loss (T 0.97). |

### Combinations

Some combinations reinforce or undermine each other:

| Combination | Effect | Why |
|---|---|---|
| High press + High line | Press E bonus ×1.5 (E 0.925) | A compact side; the press works as intended. |
| High press + Deep line | Press E becomes ×1.05 instead of ×0.95 | Gaps between the lines get exploited. |
| Low press + Deep line ("low block") | Extra E ×0.95 | Hard to break down. |
| Possession + Low press | Possession E bonus halved | You can't keep the ball if you never win it back. |

The UI should warn about the two bad combinations (High press + Deep line, Possession + Low press).

### What beats what

| If the opponent plays… | …it is countered by | …and it beats |
|---|---|---|
| High press | Direct passing; Low press (late on) | Possession (unless their passing is 80+) |
| Possession | High press | Low press |
| Direct | Deep line | High press; High line with slow CBs |
| High line | Direct with quick forwards | — |
| Deep line | Wide with good crossers (still works against it) | Direct |

## AI clubs

AI clubs pick instructions in `PrepareClub` before each match. They base the choice on their share
of strength `s` (from today's formula, before any tactics), their own attributes, and whether they're
home or away.

| Situation | Mentality | Pressing | Passing | Line | Width |
|---|---|---|---|---|---|
| Strong favourite (s ≥ 0.62) | Attacking | High if avg stamina ≥ 70, else Medium | Possession if `passQuality` ≥ 72, else Mixed | High if CB pace ≥ 70, else Standard | Wide if `crossThreat` ≥ 70 |
| Even (0.42–0.62) | Balanced (Attacking at home if s ≥ 0.55) | Medium | Mixed | Standard | Balanced |
| Underdog (s < 0.42) | Defensive | Low | Direct if `directQuality` ≥ 68, else Mixed | Deep | Narrow |

Add some randomness, e.g. a 20% chance of moving one setting one step towards Balanced, so the
AI doesn't become fully predictable.

**In-match reactions (phase 3):** at the 70th minute, an AI side losing by one goal switches to
Attacking, and one losing by two or more switches to All-out attack; a side winning by one goal
switches to Defensive. To do this, generation is split at the 70th minute in both `PreGenerateMatch`
and the `SimulateSecondHalf` path: generate up to minute 70, check the score, apply the switch, then
generate from 70 to 90. The user's live match already regenerates after changes, so this fits the
same pattern.

## User experience

### Tactics page

A new **Team instructions** panel under Formation.

- Each of the five settings is a segmented control.
- Under the selected option, show one line starting **+** (the advantage) and one starting
  **−** (the disadvantage), using the plain-language text from the tables above.
- Combination warnings appear inline, e.g. *"⚠ A high press with a deep line leaves gaps between your lines."*
- Hints based on the squad's attributes, e.g. *"Your squad's stamina (61) means a high press will fade badly after the hour."*

### Match screen

- The same panel, editable when the match is paused (`CanEditTactics`). A change triggers
  `ReGenerateFromMinute`, exactly like a substitution. Instruction changes don't count towards the
  substitution limit.
- From the 60th minute, a "tiring" indicator appears when the user's side is pressing High.

### Pre-match screen

Show the opponent's instructions ("Expected approach: Defensive, low block, direct"), so the user
can set up to counter them. This is what makes the counter-tactics a real decision rather than a
hidden modifier.

## Implementation

| File | Change |
|---|---|
| `Models/TeamInstructionsModel.cs` (new) | `Mentality`, `Pressing`, `PassingStyle`, `DefensiveLine` and `Width` enums, each defaulting to the neutral option. |
| `Models/ClubModel.cs` | `public TeamInstructionsModel Instructions { get; set; } = new();` Newtonsoft leaves the default in place for old saves without the property. |
| `Models/TacticalModifierRules.cs` (new) | A pure static function: `Modifiers(own, opponent, ownXi, opponentXi, minute)` → `(Threat, Exposure, CardRate, InjuryRate)`. All the constants from the tables above live here. |
| `Helpers/MatchSimulatorHelper.cs` | Replace `GetRatingsAtMinute` + `HomeWinsChance` with `GetGoalWeightsAtMinute(fixture, minute)` → `(wH, wA)`. `SimulateGoals` scales the step chance by `wH + wA`, and `AddGoal` and `AwardPenalties` pick the side from the weights. `SimulateCards` and `SimulateInjuries` use the C/I rates. |
| `Helpers/MatchSimulatorHelper.cs` | **The penalty shoot-out stays neutral**: `SimulatePenalties` and `GeneratePenaltyEvents` keep using strength only. |
| `Helpers/MatchSimulatorHelper.cs` → `PrepareClub` | AI instruction choice (see the table above). |
| `Components/Pages/Tactics.razor`, `Match.razor`, `PreMatch.razor` | The UI described above. |

Notes:

- Attribute averages (stamina, pass quality and so on) are computed once per side per
  regeneration, not at every step.
- `FixtureModel` should record the instructions each side used (`HomeInstructions` and
  `AwayInstructions`), so results screens and future reports can show them.
- Missing players (red cards, injuries) still reduce strength by ×0.8 each, as today. Their
  attributes drop out of the averages.

## Balance and testing

### Unit tests (`TacticalModifierRulesTests`)

- All settings neutral → T = E = C = I = 1, and `wH + wA = 1`.
- Each option produces the T/E values in the tables above.
- Fatigue: High press at minutes 59, 75 and 90, for avg stamina 50 and 85.
- Counters: Direct against High press cancels the press T bonus; High press against Possession
  (passQuality 70 and 82).
- The pace-gap scaling of the High line.
- The clamp at [0.5, 1.8].

### Engine tests (`MatchSimulatorHelperTests`)

- With neutral instructions, a seeded run produces exactly the same events as today (regression guard).
- Card share follows the C rates (High press side gets about 57% of cards against Medium).

### Simulation (`[SimulationFact]`, `TacticsBalanceSimulationTests`)

Run 20,000 matches for each scenario, measuring win/draw/loss and points per game.

1. **No dominant option:** for equal sides, each single option against all-Balanced keeps points per
   game within ±0.08 of the Balanced mirror, **except** the deliberate asymmetries (High press with
   good stamina should come out ahead; with poor stamina, behind).
2. **The trade-offs show up:**
   - an underdog (s = 0.35) gains points per game with Defensive and loses them with Attacking,
   - a favourite (s = 0.65) does the reverse.
3. **Counters work:** each "beats" pairing in the matchup table gives the winner a points-per-game edge.
4. **Goals per match** across a full AI season stay within 2.3–2.8.

Then rerun `DifficultySimulationTests` and the 10-season run. AI favourites now play Attacking,
which should widen the gap between good and bad sides slightly. If the leagues spread out too
much, tone down the Attacking T/E.

## Phasing

1. **Phase 1 (done):** the Threat/Exposure engine change, Mentality and Pressing, the Team
   Instructions panel on the Tactics page and the Match screen (unlimited changes, each
   re-evaluating the rest of the match), and AI pre-match choice.
   - Code: `Models/TeamInstructionsModel.cs`, `Models/TacticalModifierRules.cs`,
     `MatchSimulatorHelper` (`GetGoalWeightsAtMinute`, `SideEventRoll`, `ChooseAIInstructions`),
     `Components/TeamInstructionsPanel.razor`.
   - AI choice in phase 1: favourite (share ≥ 0.62) plays Attacking, and presses High if its
     average stamina is 70+; underdog (< 0.38) plays Defensive with a Low press; anyone else is
     Balanced, pressing High only with stamina 78+. 20% of the time the mentality is softened to Balanced.
   - Not done yet from the wider spec: recording each side's instructions on `FixtureModel`, and
     the attribute-scaled averages beyond stamina (they belong to phase 2 settings).
   - Mentality results (20,000 matches each, points per game against Balanced): underdog
     Defensive +0.05 / Attacking −0.07; favourite Attacking +0.05 / Defensive −0.03; even sides
     within ±0.03. Goals per match: about 1.9 Defensive, 2.4 Balanced, 2.8 Attacking.
2. **Phase 2:** Passing style, Defensive line, combinations, opponent's approach on the pre-match screen.
3. **Phase 3:** Width, AI in-match reactions at the 70th minute, the tiring indicator.

## Out of scope

- A condition/fitness system that carries over between matches (pressing would then cost next week's legs too).
- Per-player instructions (e.g. a man-marking job).
- Set-piece routines.

## Decisions

1. Mentality has **three levels**: Defensive, Balanced, Attacking. The AI's 70th-minute reaction
   (phase 3) therefore uses Attacking when losing, with no "all-out attack" level.
2. Showing the opponent's approach before a match will come from a new **analyst** staff role,
   to be added separately. It is not part of these phases.
3. Instruction changes mid-match are **unlimited**. Each one re-evaluates the rest of the match
   (`ReGenerateFromMinute`), exactly like a substitution.
