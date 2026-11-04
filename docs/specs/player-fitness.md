# Spec: Player fitness

*(As built.)* Code: `Models/FitnessRules.cs`, `MatchSimulatorHelper` (`FitnessAt`, `StrengthAt`,
`ApplyFitness`, injury weighting), `ProcessHelper.RecoverFitness`, `Components/FitnessBar.razor`.

## Rules

Every player has **fitness from 0 to 100** (`PlayerModel.Fitness`). New players and old saves start at 100.

### Drain during a match

- Every minute on the pitch costs `(37 / 90) × DrainFactor(Stamina)`, about 0.41.
  `DrainFactor = clamp(1 + (65 − Stamina) / 60, 0.6, 1.6)`, so lower Stamina tires quicker.
- Minutes while the team is **pressing high** cost **×1.25**.
- A substitute only tires from the minute he comes on. A player taken off or sent off stops tiring.
- At an average Stamina of 65: **37** lost over 90 minutes (about 46 when pressing high all game).
- **Goalkeepers** (GK among their listed positions) tire at **a third of the rate**: about 12 over 90 minutes.
  This goes by the player, so a keeper is still treated as a keeper if he's played outfield.
- **Centre-backs** (CB among their listed positions, and not a keeper) tire **a third slower**:
  about 25 over 90 minutes. Like keepers, this goes by the player, not the slot he's in.
- Fitness is charged at full time (`ApplyFitness`). During the match, `FitnessAt(fixture, player, minute)`
  works out the live value, which the match screen shows.

### Recovery

- Every day: `8 + 0.1 × (Stamina − 65)` (at least 4), up to 100.
- The user's physios add the same share they take off injury lay-offs (+15% / +25% / +30%).
- At average Stamina, a full 90 minutes takes about four and a half days to recover (8 a day), so a Saturday–Tuesday–Saturday run leaves players short of full fitness.

### Effect on the team

- A player's contribution to team strength is his rating (after any out-of-position cut) ×
  `StrengthFactor(fitness)`: full at 90 and above, then −0.6% per point below 90, never below 0.6.
  For example, 80 → ×0.94 and 40 → ×0.70.
- This applies minute by minute during the match, so players fade late on and fresh substitutes are worth more.

### Injuries

- `InjuryWeight = 1 + 1.5 × (100 − fitness) / 100`. A side's injury rate is scaled by its players' average
  weight, and the injured player is picked in proportion to their weight. Tired players get injured more,
  and injuries overall rise slightly late in matches.

### Team selection

- AI clubs (and the user's Auto Pick) rank candidates by rating × out-of-position factor × fitness factor,
  so a tired player is rested when a fresher option is close enough in quality.

### Relationship to team-instruction fatigue

The pressing **fade / squeeze / counter** rules in the team instructions spec still use their own team
workload model (minutes pressed). Player fitness sits on top of that. If player fitness drove those
rules, every side would count as tired late on and pressing late would become a free win.

## Screens

- **Tactics:** a fitness bar with the number next to each player.
- **Live match, Tactics tab:** the live fitness number (coloured) for starters and substitutes.
- **Club squad list** and **Player page:** fitness bar.
- Colours: green 85+, yellow 70–84, orange 55–69, red below 55.

## Not done

- AI sides make no substitutions, so their players fade late on while the user can bring on fresh legs.
- Fitness doesn't yet show on the pitch view.
