namespace YouTubeArchiverServer
{
    public class Config
    {
        public string SingleChannelId { get; set; }

        public bool IsForSingleChannel => !string.IsNullOrEmpty(SingleChannelId);
    }
}