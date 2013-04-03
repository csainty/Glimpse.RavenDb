using System;
using Glimpse.Core.Message;

namespace Glimpse.RavenDb.Message
{
    public class RavenDbTimelineMessage : MessageBase, ITimelineMessage
    {
        internal static TimelineCategoryItem RavenDbTimelineCategory = new TimelineCategoryItem("RavenDb", "#ff000", "#00ff00");

        public TimelineCategoryItem EventCategory { get; set; }

        public string EventName { get; set; }

        public string EventSubText { get; set; }

        public TimeSpan Duration { get; set; }

        public TimeSpan Offset { get; set; }

        public DateTime StartTime { get; set; }
    }
}