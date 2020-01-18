using System.Collections.Generic;
using Common.Models;

namespace YouTubeArchiverServer.Models
{
    public class ChannelModel
    {
        public Channel Channel { get; set; }
        
        public List<Video> Videos { get; set; }
    }
}