using System.Collections.Generic;
using Common.Models;

namespace YouTubeArchiverServer.Models
{
    public class TopicVideoModel
    {
        public TopicVideoModel()
        {
            Segments = new List<SegmentModel>();
        }
        
        public TopicModel Topic { get; set; }
        
        public VideoModel Video { get; set; }
        
        public List<SegmentModel> Segments { get; set; }
    }
}