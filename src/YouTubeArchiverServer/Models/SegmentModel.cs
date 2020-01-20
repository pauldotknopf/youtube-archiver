using System;
using Humanizer;

namespace YouTubeArchiverServer.Models
{
    public class SegmentModel
    {
        public TopicVideoModel Video { get; set; }
        
        public string Text { get; set; }
        
        public int Location { get; set; }

        public string FriendlyLocation
        {
            get
            {
                var timespan = TimeSpan.FromSeconds(Location);
                return timespan.ToString();
            }
        }
    }
}