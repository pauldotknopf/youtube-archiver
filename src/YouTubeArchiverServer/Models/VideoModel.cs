using System.Collections.Generic;
using Common;
using Common.Models;

namespace YouTubeArchiverServer.Models
{
    public class VideoModel
    {
        public VideoModel()
        {
            MentionedTopics = new List<TopicModel>();
        }
        
        public ChannelModel Channel { get; set; }
        
        public Video Video { get; set; }
        
        public VideoPath VideoPath { get; set; }
        
        public List<TopicModel> MentionedTopics { get; set; }
    }
}