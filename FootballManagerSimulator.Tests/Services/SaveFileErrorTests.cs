using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Tests.Services;

/// <summary>What saving, listing, deleting and loading do when the saves folder or a save file is not as expected.</summary>
[Collection(SaveFilesCollection.Name)]
public class SaveFileErrorTests : IDisposable
{
    private readonly GameHost _host = new(new SettingsModel());
    private GameEngine Engine => _host.Engine;
    private string SaveDirectory => _host.Storage.SaveDirectory;

    public SaveFileErrorTests()
    {
        _host.State.Date = new DateOnly(2026, 9, 1);
        _host.State.ManagerName = "Tester";
        _host.State.MyClubId = 1;
        _host.State.Clubs.Add(TestData.Club(1));
    }

    public void Dispose() => _host.Dispose();

    private string PathOf(string fileName) => Path.Combine(SaveDirectory, fileName);

    [Fact]
    public void SaveGame_ReportsAFailureWhenTheSavesFolderIsMissing()
    {
        Directory.Delete(SaveDirectory, recursive: true);

        Assert.Equal("Unable to save game.", Engine.SaveGame("slot"));
    }

    [Fact]
    public void ListingSaves_FindsNothingWhenTheSavesFolderIsMissing()
    {
        Directory.Delete(SaveDirectory, recursive: true);

        Assert.Empty(Engine.ListSaves());
        Assert.Null(Engine.MostRecentSave());
    }

    [Fact]
    public void DeleteSave_ReportsASaveThatIsInUse()
    {
        if (!OperatingSystem.IsWindows())
            return; // only Windows refuses to delete an open file

        Engine.SaveGame("open");
        using (File.Open(PathOf("open.fms"), FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.Equal("Unable to delete the save.", Engine.DeleteSave("open.fms"));

        Assert.True(File.Exists(PathOf("open.fms")));
    }

    [Theory]
    [InlineData("[1, 2, 3]")]
    [InlineData("")]
    public void LoadGame_RefusesAFileThatIsNotASaveAndKeepsTheCurrentGame(string contents)
    {
        File.WriteAllText(PathOf("bad.fms"), contents);

        Assert.Equal("Not a save file.", Engine.LoadGame("bad.fms"));
        Assert.Equal(("Tester", new DateOnly(2026, 9, 1)), (_host.State.ManagerName, _host.State.Date));
    }

    [Fact]
    public void ListSaves_SkipsAFileThatIsNotASave()
    {
        File.WriteAllText(PathOf("array.fms"), "[1, 2, 3]");
        Engine.SaveGame("good");

        Assert.Equal("good.fms", Assert.Single(Engine.ListSaves()).FileName);
    }

    [Fact]
    public void LoadGame_IgnoresFieldsTheGameNoLongerHas()
    {
        Engine.SaveGame("old");
        var json = JObject.Parse(File.ReadAllText(PathOf("old.fms")));
        json.AddFirst(new JProperty("RemovedFeature", new JObject { ["Level"] = 3 }));
        File.WriteAllText(PathOf("old.fms"), json.ToString());
        _host.State.ManagerName = "Someone Else";

        Assert.Null(Engine.LoadGame("old.fms"));
        Assert.Equal("Tester", _host.State.ManagerName);
    }

    [Fact]
    public void LoadGame_SkipsScheduledEventsItCannotRebuildAndKeepsTheRest()
    {
        var expansion = ActivatorUtilities.CreateInstance<StadiumExpansionEvent>(_host.Services, new DateOnly(2027, 9, 1), false);
        _host.State.Events.Add(expansion);
        Engine.SaveGame("events");

        var json = JObject.Parse(File.ReadAllText(PathOf("events.fms")));
        var events = (JArray)json["Events"]!;
        var good = (JObject)events[0];
        events.Add(new JObject { ["$type"] = "FootballManagerSimulator.Maui.Events.NoSuchEvent, Nowhere", ["TriggerDate"] = good["TriggerDate"] });
        events.Add(new JObject { ["$type"] = good["$type"], ["TriggerDate"] = "not a date" });
        events.Add(new JObject { ["TriggerDate"] = good["TriggerDate"] });
        File.WriteAllText(PathOf("events.fms"), json.ToString());

        Assert.Null(Engine.LoadGame("events.fms"));

        var loaded = Assert.Single(_host.State.Events);
        Assert.IsType<StadiumExpansionEvent>(loaded);
        Assert.Equal(new DateOnly(2027, 9, 1), loaded.TriggerDate);
    }
}
