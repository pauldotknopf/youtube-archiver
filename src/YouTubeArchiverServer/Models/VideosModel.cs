using System.Collections.Generic;

namespace YouTubeArchiverServer.Models
{
    public class VideosModel
    {
        public VideosModel()
        {
            Videos = new List<VideoModel>();
        }
        
        public List<VideoModel> Videos { get; }
    }
}