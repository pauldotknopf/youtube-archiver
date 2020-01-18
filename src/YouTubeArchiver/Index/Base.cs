using System.CommandLine;

namespace YouTubeArchiver.Index
{
    public class Base
    {
        public static Command Create()
        {
            return new Command("index")
            {
                UpdateDb.Create(),
                UpdateCaptions.Create(),
                DownloadVideos.Create(),
                SearchCaptions.Create(),
                IndexTopic.Create()
            };
        }
    }
}