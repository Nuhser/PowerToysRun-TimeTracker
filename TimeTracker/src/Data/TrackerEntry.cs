using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Community.Powertoys.Run.Plugin.TimeTracker.Data;

public class TrackerEntry(string name)
{
    public string Name { get; set; } = name;
    public List<TrackerSubEntry> SubEntries { get; set; } = [new TrackerSubEntry()];

    [JsonIgnore]
    public bool Running => SubEntries.Any(subEntry => subEntry.Running);

    public TimeSpan? GetDuration(bool includeRunning = false)
    {
        return SubEntries
        .Select(subEntry => subEntry.GetDuration(includeRunning))
        .Aggregate((a, b) => a?.Add(b ?? TimeSpan.Zero) ?? b?.Add(a ?? TimeSpan.Zero));
    }

    public bool HasSubEntries()
    {
        return SubEntries.Count > 1;
    }

    public DateTime? GetStart()
    {
        if (HasSubEntries())
        {
            return null;
        }

        return SubEntries.First().Start;
    }

    public DateTime? GetEnd()
    {
        if (HasSubEntries())
        {
            return null;
        }

        return SubEntries.First().End;
    }
}

public class TrackerSubEntry
{
    public DateTime Start { get; set; } = DateTime.Now;
    public DateTime? End { get; set; } = null;

    [JsonIgnore]
    public bool Running => End == null;

    public TimeSpan? GetDuration(bool includeRunning = false)
    {
        return End?.Subtract(Start) ?? (includeRunning ? DateTime.Now.Subtract(Start) : null);
    }
}