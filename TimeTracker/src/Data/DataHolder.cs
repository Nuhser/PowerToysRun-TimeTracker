using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Community.Powertoys.Run.Plugin.TimeTracker.Platform;
using Community.Powertoys.Run.Plugin.TimeTracker.Settings;
using Wox.Plugin.Logger;

namespace Community.Powertoys.Run.Plugin.TimeTracker.Data
{
    public class DataHolder : AbstractDataHolder
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum DataStructureVersion
        {
            v1,
            v2
        }

        public DataStructureVersion Version { get; set; } = DataStructureVersion.v2;
        public Dictionary<DateOnly, List<TrackerEntry>> TrackerEntries { get; set; } = [];
    }
}