using System.CommandLine;

namespace YouTubeArchiver.Index
{
    public class Base
    {
        public static Command Create()
        {
            return new Command("index")
            {
                new Option(new []{"-i", "--index-directory"}, "The directory where the index exists.")
                {
                    Name = "index-directory",
                    Argument = new Argument<string>()
                },
                UpdateDb.Create(),
                UpdateCaptions.Create(),
                DownloadVideos.Create(),
                SearchCaptions.Create(),
                IndexTopic.Create(),
                UploadVideos.Create()
            };
        }
    }
}