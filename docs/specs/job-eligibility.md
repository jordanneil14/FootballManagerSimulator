# Spec: Job eligibility and manager reputation

*(As built.)* Code: `Models/JobRules.cs` (every number below), `Helpers/ReputationHelper.cs` (standing,
reputation, end-of-season review), `Helpers/JobOfferHelper.cs` (chances, applications, offers, out of work),
`Models/JobApplicationModel.cs`, `Components/Pages/Jobs.razor`, `ManagerProfile.razor`, `Main.razor`.

## Why

Any vacant club could be joined with one click, so a manager just sacked by a Division 6 club could walk into a
Spanish Division 1 job. Unsolicited offers had a separate rule (any club in your country, your league or
higher). Now both depend on your **reputation** against the club's **standing**, joining a club is an
**application** it can turn down, and results build your reputation so you can work your way up.

## 1. Club standing (0–100)

Leagues with the same `Rank` in different countries aren't equal, so standing comes from the squad.

- Squad strength = the average rating of the club's best 20 players (`ReputationHelper.SquadStrengths`, also
  used by `GameEngine.MySquadAverageRating`).
- Standing = the club's **percentile** among all clubs by squad strength: the weakest is 0, the strongest 100,
  equal squads share a value.
- Worked out when needed and never saved. Screens work it out once per render and pass it around.
- Shown as 1–5 stars, rounded up: 0–20 ★, 20–40 ★★ … 80–100 ★★★★★.

## 2. Manager reputation (0–100)

Saved as `State.ManagerReputation`. It's null until first needed, so old saves start from their club.

### Starting value

- New game: the starting club's standing − 10, at least 5.
- Old saves: the same with your current club. If you're unemployed, it uses your most recent club, or 20 if
  there is none.

### Changes

| Event | Change |
|---|---|
| End of season: league finish against expected | `clamp((expected − actual) × 1.5, −10, +10)` × league weight × share of league games managed |
| End of season: drift toward your club's standing | `+ (standing − reputation) × 0.10`, only when positive |
| Won the league | +8 × league weight |
| Won a cup (any trophy this season other than a league title or play-off final) | +5 × that cup's weight |
| Promoted | +5 |
| Relegated | −6 |
| Sacked | −8 |
| Resigned | −3 |
| Out of work: every 30 days after the first 90 | −1, never below 5 |
| Moving to a new job | no change. It isn't a resignation. |

- **Expected finish** is where the squad's strength ranks within its league. It's stored on `ManagerJobModel`
  when you start a job, at the start of a new game, and on 30 June for the season ahead.
- **League weight** is the average standing of the league's clubs ÷ 50, between 0.5 and 1.5. A cup's weight uses
  the clubs in its fixtures.
- The season is reviewed on 30 June using the final table, before promotion and relegation. Promotion and
  relegation are counted once the clubs have moved. You get one "Your Reputation" notification that lists each
  reason, and `State.LastSeasonReputationChange` is set. If you're out of work on 30 June, there's no review.
- Reputation always stays within 0–100.

## 3. Will the club have you?

`gap = reputation − standing`, adjusted:

- **Employed and Under pressure / At risk** (board confidence below 30): −5.
- **Employed and Secure** (75+): +3.
- **The club that sacked you:** out of reach until the end of the season after the one you were sacked in.

| Band | Gap | Chance | Shown as |
|---|---|---|---|
| Interested | ≥ −5 | 90% (60% if the gap is above +30, since big names may not stay) | green |
| Possible | −5 to −15 | 50% | yellow |
| Long shot | −15 to −25 | 15% | red |
| Out of reach | < −25 | can't apply | grey, with the reason |

After **90 days** out of work, Interested and Possible clubs both accept at 90%. Only the labels are shown,
never the percentages.

## 4. Applying

- The Jobs screen's **Apply** button opens a confirmation that shows the band. It saves a
  `JobApplicationModel { ClubId, AppliedOn, DecideOn }`.
- `DecideOn` = 2–4 days later, capped at the day before the club's `VacantUntil`, and never earlier than tomorrow.
- **Limits:** 3 pending applications, one per club. You can apply at any time, even just after starting a job,
  and it doesn't affect board confidence.
- On `DecideOn` (checked daily, before vacancies are filled) the chance is rolled against the gap at that moment:
  - **Accepted:** "{Club} want you as manager" with Accept / Decline (the job-offer actions). Like any action
    notification it must be answered before the next day. The vacancy is held open a day if it would otherwise close.
  - **Rejected:** the chairman gives a reason. You can't apply to that club again for **90 days**
    (`JobRejectionModel { ClubId, Until }`), even if it has a new vacancy. The row shows
    "Rejected — reapply from {date}".
- If the club appoints someone first, the application ends with a "position filled" notification.
- Leaving a club cancels its pending action notifications, but job offers and applications stand.

## 5. Unsolicited offers

- Still only while Secure, with a 15% chance each Monday, and one at a time.
- Candidates are any club in any country whose adjusted gap is between **−10 and +5**. The pick is weighted by
  `101 − JobSecurity`, so clubs with a struggling manager call most often.
- An offer has already been agreed by the club: accepting it moves you straight away.

## 6. No dead ends out of work

After **28 days** out of work with no Interested or Possible vacancy (that hasn't rejected you), one is opened.
It's the weakest-standing reachable club whose manager is Under pressure or At risk. If there's none, it's the
reachable club with the lowest job security. You always get a "sack their manager" notification.

## 7. Screens

- **Jobs:**
  - The header shows your reputation.
  - Vacancies: Club · League · Standing · Your chance · Appointing by · Apply / Applied / reason, best chance
    first, then by standing.
  - "Your Applications" panel, with decision dates.
  - Managers Under Threat also shows Standing and Your chance.
- **Manager Profile:** a Reputation panel with stars, the number, and the change last season.
- **Main (out of work):** your reputation and how many vacancies are within reach.

## Decisions

1. Applying while employed doesn't affect board confidence (maybe later).
2. Reputation is one global number.
3. Chances are shown as labels only.
4. Leaving for a new job isn't a resignation.

## Not done

- `JoinClub` is still public on `IJobOfferHelper`, and some tests use it. The UI only reaches it through an
  accepted offer or application.
- Applications and unemployment aren't processed on 30 June (end-of-season day). They catch up on 1 July.
- There's no 10-season career test yet that checks the manager isn't stuck out of work. The dead-end safeguard is
  unit-tested, and `FullGameTests` checks a reachable job exists after two months out of work.
