using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Community.Powertoys.Run.Plugin.TimeTracker.Platform;
using Community.Powertoys.Run.Plugin.TimeTracker.Settings;

namespace Community.Powertoys.Run.Plugin.TimeTracker.Data;

public class DataManager(SettingsManager settingsManager) : AbstractDataManager<DataHolder>
{
    private readonly SettingsManager _settingsManager = settingsManager;

    public override string GetDataFilePath()
    {
        return Path.Combine(DATA_BASE_DIRECTORY_PATH, "data.json");
    }

    public void AddTrackerEntry(string name)
    {
        DateOnly key = DateOnly.FromDateTime(DateTime.Now);

        if (!Data.TrackerEntries.TryGetValue(key, out List<TrackerEntry>? value))
        {
            Data.TrackerEntries.Add(key, [new TrackerEntry(name)]);
        }
        else
        {
            if (value.Any(entry => entry.Name == name))
            {
                value.Where(entry => entry.Name == name)
                    .First()
                    .SubEntries
                    .Add(new TrackerSubEntry());
            }
            else
            {
                Data.TrackerEntries[key].Add(new TrackerEntry(name));
            }
        }

        ToJson();
    }

    public bool IsTaskRunning()
    {
        return Data.TrackerEntries.Any(day => day.Value.Any(entry => entry.Running));
    }

    public int GetNumberOfRunningTasks()
    {
        return Data.TrackerEntries
            .Select(day => day.Value)
            .Aggregate(new List<TrackerEntry>(), (a, b) => [.. a, .. b])
            .Where(entry => entry.Running)
            .Count();
    }

    public List<string> GetNamesOfRunningTask()
    {
        return Data.TrackerEntries
            .Select(day => day.Value)
            .Aggregate(new List<TrackerEntry>(), (a, b) => [.. a, .. b])
            .Where(entry => entry.Running)
            .Select(entry => entry.Name)
            .ToList();
    }

    public string GetNamesOfRunningTasksAsString()
    {
        return string.Join(", ", GetNamesOfRunningTask().Select(name => "'" + name + "'").ToList());
    }

    public bool IsTaskRunningForDate(DateOnly date)
    {
        return Data.TrackerEntries[date].Any(entry => entry.Running);
    }

    public TimeSpan? GetTotalDurationForDay(DateOnly date, bool includeRunning = false)
        {
            return Data.TrackerEntries[date]
                .Select(entry => entry.GetDuration(includeRunning))
                .Aggregate((a, b) => a?.Add(b ?? TimeSpan.Zero) ?? b?.Add(a ?? TimeSpan.Zero));
        }
}