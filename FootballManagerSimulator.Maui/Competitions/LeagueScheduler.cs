using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions;

// Builds league schedules in which no club plays more than MaxRun home or away games in a row.
// Schedules are in terms of slots 0..n-1; callers shuffle clubs into slots for variety.
public static class LeagueScheduler
{
    public const int MaxRun = 2;

    // Clubs are shuffled into slots so each season's order differs. `fixtureDates` receives the
    // number of matchdays and returns that many dates; matchday n is played on the nth earliest.
    public static List<FixtureModel> CreateFixtures(
        IEnumerable<ClubModel> clubs, int meetings, Func<int, IEnumerable<DateOnly>> fixtureDates)
    {
        var shuffled = clubs.OrderBy(_ => RandomNumberHelper.Next()).ToList();
        var rounds = Season(shuffled.Count, meetings, RandomNumberHelper.Next);
        var dates = fixtureDates(rounds.Count).OrderBy(d => d).ToList();

        return rounds
            .SelectMany((round, index) => round.Select(m => new FixtureModel
            {
                HomeClub = shuffled[m.Home],
                AwayClub = shuffled[m.Away],
                Round = index + 1,
                Date = dates[index],
                KickOffTime = new TimeOnly(15, 00)
            }))
            .OrderBy(p => p.Round)
            .ThenBy(p => p.HomeClub.Name)
            .ToList();
    }

    // Circle method with alternating venues: every slot alternates home and away apart from
    // a single pair of consecutive games at the same venue. Needs an even number of slots.
    public static List<List<(int Home, int Away)>> SingleRoundRobin(int slots)
    {
        if (slots < 2 || slots % 2 != 0)
            throw new ArgumentException("A round robin needs an even number of clubs.", nameof(slots));

        var rotating = slots - 1;
        var rounds = new List<List<(int Home, int Away)>>();

        for (var round = 0; round < rotating; round++)
        {
            var matches = new List<(int Home, int Away)>
            {
                round % 2 == 0 ? (rotating, round) : (round, rotating)
            };

            for (var k = 1; k < slots / 2; k++)
            {
                var a = (round + k) % rotating;
                var b = (round - k + rotating) % rotating;
                matches.Add(k % 2 == 1 ? (a, b) : (b, a));
            }

            rounds.Add(matches);
        }

        return rounds;
    }

    // Each pairing meets `meetings` times (2 or 3): the first round robin, its mirror, and for a
    // third meeting the first round robin again. Each later block starts from a rotated round,
    // picked at random among those that keep every slot within MaxRun.
    public static List<List<(int Home, int Away)>> Season(int slots, int meetings, Func<int, int> randomIndex)
    {
        if (meetings is < 2 or > 3)
            throw new ArgumentOutOfRangeException(nameof(meetings));

        var first = SingleRoundRobin(slots);
        var mirrored = first.Select(r => r.Select(m => (m.Away, m.Home)).ToList()).ToList();
        var count = first.Count;
        var valid = new List<List<List<(int Home, int Away)>>>();

        for (var secondShift = 0; secondShift < count; secondShift++)
        {
            var thirdShifts = meetings == 3 ? Enumerable.Range(0, count) : [-1];
            foreach (var thirdShift in thirdShifts)
            {
                var season = first.Concat(Rotate(mirrored, secondShift)).ToList();
                if (thirdShift >= 0)
                    season.AddRange(Rotate(first, thirdShift));

                if (LongestRun(season, slots) <= MaxRun)
                    valid.Add(season);
            }
        }

        if (valid.Count == 0)
            throw new InvalidOperationException($"No schedule for {slots} clubs keeps runs to {MaxRun}.");

        return valid[randomIndex(valid.Count)];
    }

    private static IEnumerable<List<(int Home, int Away)>> Rotate(List<List<(int Home, int Away)>> rounds, int shift) =>
        rounds.Skip(shift).Concat(rounds.Take(shift));

    public static int LongestRun(IEnumerable<List<(int Home, int Away)>> rounds, int slots)
    {
        var last = new bool?[slots];
        var run = new int[slots];
        var longest = 0;

        foreach (var round in rounds)
        foreach (var (home, away) in round)
        {
            foreach (var (slot, isHome) in new[] { (home, true), (away, false) })
            {
                run[slot] = last[slot] == isHome ? run[slot] + 1 : 1;
                last[slot] = isHome;
                longest = Math.Max(longest, run[slot]);
            }
        }

        return longest;
    }

    // One more round robin between `history.Count` slots that already have games behind them.
    // Keeps every slot within MaxRun (counting the games in `history`, oldest first, true = home)
    // and, within that, levels as many pairings as possible: `hosted(a, b)` is how often a has
    // already hosted b. Returns null if no order and choice of venues keeps within MaxRun.
    public static List<List<(int Home, int Away)>>? Continue(
        IReadOnlyList<IReadOnlyList<bool>> history,
        Func<int, int, int> hosted,
        Func<int, int> randomIndex,
        int attempts = 25)
    {
        var slots = history.Count;
        if (slots < 2) return [];

        List<List<(int Home, int Away)>>? best = null;
        var bestCost = int.MaxValue;

        for (var attempt = 0; attempt < attempts && bestCost > 0; attempt++)
        {
            // A different labelling gives a different set of pairings per round.
            var labels = Enumerable.Range(0, slots).OrderBy(_ => randomIndex(int.MaxValue)).ToArray();
            var pairings = SingleRoundRobin(slots)
                .Select(r => r.Select(m => (labels[m.Home], labels[m.Away])).ToList())
                .ToList();

            var search = new ContinueSearch(history, hosted, pairings, bestCost);
            if (search.Run() is { } result && search.BestCost < bestCost)
            {
                best = result;
                bestCost = search.BestCost;
            }
        }

        return best;
    }

    private sealed class ContinueSearch
    {
        private readonly List<List<(int A, int B)>> _pairings;
        private readonly Func<int, int, int> _hosted;
        private readonly bool?[] _last;
        private readonly int[] _run;
        private readonly bool[] _used;
        private readonly List<List<(int Home, int Away)>> _current = [];
        private List<List<(int Home, int Away)>>? _best;

        public int BestCost { get; private set; }

        public ContinueSearch(IReadOnlyList<IReadOnlyList<bool>> history, Func<int, int, int> hosted,
            List<List<(int A, int B)>> pairings, int costToBeat)
        {
            _pairings = pairings;
            _hosted = hosted;
            _used = new bool[pairings.Count];
            _last = new bool?[history.Count];
            _run = new int[history.Count];
            BestCost = costToBeat;

            for (var slot = 0; slot < history.Count; slot++)
            {
                foreach (var isHome in history[slot])
                {
                    _run[slot] = _last[slot] == isHome ? _run[slot] + 1 : 1;
                    _last[slot] = isHome;
                }
            }
        }

        public List<List<(int Home, int Away)>>? Run()
        {
            Search(0);
            return _best;
        }

        private void Search(int cost)
        {
            if (cost >= BestCost) return;

            if (_current.Count == _pairings.Count)
            {
                BestCost = cost;
                _best = _current.Select(r => r.ToList()).ToList();
                return;
            }

            for (var i = 0; i < _pairings.Count; i++)
            {
                if (_used[i]) continue;
                _used[i] = true;
                _current.Add([]);
                Orient(_pairings[i], 0, cost);
                _current.RemoveAt(_current.Count - 1);
                _used[i] = false;
                if (BestCost == 0) return;
            }
        }

        private void Orient(List<(int A, int B)> round, int index, int cost)
        {
            if (index == round.Count)
            {
                Search(cost);
                return;
            }

            var (a, b) = round[index];
            // Try the levelling venue first so good schedules are found early.
            var options = _hosted(a, b) <= _hosted(b, a) ? new[] { (a, b), (b, a) } : new[] { (b, a), (a, b) };

            foreach (var (home, away) in options)
            {
                if (!CanPlay(home, true) || !CanPlay(away, false)) continue;

                var unlevelled = _hosted(home, away) + 1 != _hosted(away, home) ? 1 : 0;
                var saved = (_last[home], _run[home], _last[away], _run[away]);
                Play(home, true);
                Play(away, false);
                _current[^1].Add((home, away));

                Orient(round, index + 1, cost + unlevelled);

                _current[^1].RemoveAt(_current[^1].Count - 1);
                (_last[home], _run[home], _last[away], _run[away]) = saved;
                if (BestCost == 0) return;
            }
        }

        private bool CanPlay(int slot, bool isHome) => _last[slot] != isHome || _run[slot] < MaxRun;

        private void Play(int slot, bool isHome)
        {
            _run[slot] = _last[slot] == isHome ? _run[slot] + 1 : 1;
            _last[slot] = isHome;
        }
    }
}
