using System.Collections.Generic;
using Common.Models;

namespace YouTubeArchiverServer.Models
{
    public class TopicsModel
    {
        public TopicsModel()
        {
            Topics = new List<TopicModel>();
        }
        public List<TopicModel> Topics { get; }
    }
}