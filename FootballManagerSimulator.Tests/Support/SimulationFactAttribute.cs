namespace FootballManagerSimulator.Tests.Support;

// Long-running simulations (an hour or more) only run when RUN_SIMULATIONS=1 is set, so normal test runs skip them.
public sealed class SimulationFactAttribute : FactAttribute
{
    public SimulationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_SIMULATIONS") != "1")
            Skip = "Long-running simulation. Set RUN_SIMULATIONS=1 to run it.";
    }
}
