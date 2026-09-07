using System;
using System.IO;
using ControlBee.Services;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(LocalizationManager))]
public class LocalizationManagerTest : IDisposable
{
    private readonly string _folder = Directory
        .CreateTempSubdirectory(nameof(LocalizationManagerTest))
        .FullName;

    [Fact]
    public void LoadMergesLaterFileOverEarlier()
    {
        var shared = Write(
            "shared.json",
            """{"LogView": {"WorkLog": "shared", "Start": "start"}}"""
        );
        var machine = Write(
            "machine.json",
            """{"LogView": {"WorkLog": "machine"}, "Own": "own"}"""
        );
        var manager = new LocalizationManager();

        manager.Load(shared, machine);

        Assert.Equal("machine", manager["LogView.WorkLog"]);
        Assert.Equal("start", manager["LogView.Start"]);
        Assert.Equal("own", manager["Own"]);
    }

    [Fact]
    public void LoadSkipsAFileItCannotRead()
    {
        var machine = Write("machine.json", """{"Own": "own"}""");
        var manager = new LocalizationManager();

        manager.Load(Path.Combine(_folder, "missing.json"), machine);

        Assert.Equal("own", manager["Own"]);
    }

    [Fact]
    public void LoadKeepsTranslationsWhenNoFileCouldBeRead()
    {
        var machine = Write("machine.json", """{"Own": "own"}""");
        var manager = new LocalizationManager();
        manager.Load(machine);

        manager.Load(Path.Combine(_folder, "missing.json"));

        Assert.Equal("own", manager["Own"]);
    }

    [Fact]
    public void LoadReplacesEverythingLoadedBefore()
    {
        var ko = Write("ko.json", """{"Own": "own", "OnlyKo": "ko"}""");
        var en = Write("en.json", """{"Own": "own-en"}""");
        var manager = new LocalizationManager();
        manager.Load(ko);

        manager.Load(en);

        Assert.Equal("own-en", manager["Own"]);
        Assert.Equal("[MISSING:OnlyKo]", manager["OnlyKo"]);
    }

    private string Write(string name, string json)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllText(path, json);
        return path;
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
        GC.SuppressFinalize(this);
    }
}
