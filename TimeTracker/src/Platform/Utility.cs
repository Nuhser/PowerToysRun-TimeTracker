using System;

namespace Community.Powertoys.Run.Plugin.TimeTracker.Platform
{
    public static class Utility
    {
        public static string GetDurationAsString(TimeSpan? duration)
        {
            if (duration == null)
                return "";

            return duration?.Hours + "h " + duration?.Minutes + "m " + duration?.Seconds + "s";
        }
    }
}