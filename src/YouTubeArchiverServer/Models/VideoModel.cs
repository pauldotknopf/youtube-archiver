using Common.Models;

namespace YouTubeArchiverServer.Models
{
    public class VideoModel
    {
        public Channel Channel { get; set; }
        
        public Video Video { get; set; }
    }
}