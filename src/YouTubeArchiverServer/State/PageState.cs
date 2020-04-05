namespace YouTubeArchiverServer.State
{
    public class PageState : ITitleHint
    {
        public PageState(string title)
        {
            Title = title;
        }
        
        public string Title { get; }
    }
}