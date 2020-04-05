using System.Collections.Generic;

namespace YouTubeArchiverServer.Models
{
    public class TopicModel
    {
        public TopicModel()
        {
            Videos = new List<TopicVideoModel>();
        }
        
        public string Id { get; set; }
        
        public string Topic { get; set; }

        public List<TopicVideoModel> Videos { get; set; }
    }
}