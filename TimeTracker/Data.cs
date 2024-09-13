using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wox.Plugin.Logger;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class Data
    {
        private readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() { WriteIndented = true };

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum PluginVersion
        {
            v1_0_0,
            v1_0_1
        }

        public class TrackerEntry(string name)
        {
            public string Name { get; set; } = name;
            public List<TrackerSubEntries> SubEntries { get; set; } = [new TrackerSubEntries()];

            [JsonIgnore]
            public TimeSpan? Duration => SubEntries
                .Select(subEntry => subEntry.Duration)
                .Aggregate((a, b) => a?.Add(b ?? TimeSpan.Zero) ?? b?.Add(a ?? TimeSpan.Zero));

            [JsonIgnore]
            public bool Running => SubEntries.Any(subEntry => subEntry.Running);

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

        public class TrackerSubEntries()
        {
            public DateTime Start { get; set; } = DateTime.Now;
            public DateTime? End { get; set; } = null;

            [JsonIgnore]
            public TimeSpan? Duration => (End != null) ? End - Start : null;
            [JsonIgnore]
            public bool Running => End == null;
        }

        // properties
        public PluginVersion Version { get; set; } = PluginVersion.v1_0_1;
        public Dictionary<DateOnly, List<TrackerEntry>> TrackerEntries { get; set; } = [];

        // utility methods
        public void AddTrackerEntry(string name)
        {
            DateOnly key = DateOnly.FromDateTime(DateTime.Now);

            if (!TrackerEntries.TryGetValue(key, out List<TrackerEntry>? value))
            {
                TrackerEntries.Add(key, [new TrackerEntry(name)]);
            }
            else
            {
                if (value.Any(entry => entry.Name == name))
                {
                    value.Where(entry => entry.Name == name)
                        .First()
                        .SubEntries
                        .Add(new TrackerSubEntries());
                }
                else
                {
                    TrackerEntries[key].Add(new TrackerEntry(name));
                }
            }

            ToJson();
        }

        public bool IsTaskRunning()
        {
            return TrackerEntries.Any(day => day.Value.Any(entry => entry.Running));
        }

        public int GetNumberOfRunningTasks()
        {
            return TrackerEntries
                .Select(day => day.Value)
                .Aggregate(new List<TrackerEntry>(), (a, b) => [.. a, .. b])
                .Where(entry => entry.Running)
                .Count();
        }

        public List<string> GetNamesOfRunningTask()
        {
            return TrackerEntries
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

        public static bool FromJson(out Data? data)
        {
            if (!File.Exists(SettingsManager.DATA_PATH))
            {
                data = new Data();
                data.ToJson();
                return true;
            }

            try
            {
                string jsonString = File.ReadAllText(SettingsManager.DATA_PATH);
                data = JsonSerializer.Deserialize<Data>(jsonString);

                if (data == null)
                {
                    Log.Error(SettingsManager.DATA_PATH + " was read but didn't contain any data content.", typeof(Data));
                    return false;
                }

                return true;
            }
            catch (JsonException)
            {
                Log.Error(SettingsManager.DATA_PATH + " couldn't be read or didn't contain a valid JSON.", typeof(Data));
                data = null;
                return false;
            }
        }

        public void ToJson()
        {
            string jsonString = JsonSerializer.Serialize(this, JSON_SERIALIZER_OPTIONS);
            File.WriteAllText(SettingsManager.DATA_PATH, jsonString);
        }
    }
}