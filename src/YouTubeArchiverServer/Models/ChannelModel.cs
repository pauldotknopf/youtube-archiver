using System.Collections.Generic;
using Common.Models;

namespace YouTubeArchiverServer.Models
{
    public class ChannelModel
    {
        public ChannelModel()
        {
            Videos = new List<VideoModel>();
            Topics = new List<TopicModel>();
        }
        
        public Channel Channel { get; set; }
        
        public List<VideoModel> Videos { get; set; }
        
        public List<TopicModel> Topics { get; set; }
    }
}