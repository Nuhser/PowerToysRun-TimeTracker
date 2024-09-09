using System;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class Utility
    {
        public class TrackerEntry(string name)
        {
            public string Name { get; set; } = name;
            public DateTime Start { get; set; } = DateTime.Now;
            public DateTime? End { get; set; } = null;
        }
        
        public static string GetDurationAsString(TimeSpan? duration)
        {
            if (duration == null)
                return "";

            return duration?.Hours + "h " + duration?.Minutes + "m " + duration?.Seconds + "s";
        }
    }
}