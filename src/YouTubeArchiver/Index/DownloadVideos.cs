using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Common;
using Serilog;

namespace YouTubeArchiver.Index
{
    public class DownloadVideos
    {
        public static Command Create()
        {
            var command = new Command("download-videos")
            {
                Handler = CommandHandler.Create(typeof(DownloadVideos).GetMethod(nameof(Run)))
            };
            
            return command;
        }
        
        public static void Run(string indexDirectory)
        {
            var workspace = Helpers.GetWorkspace(indexDirectory);
            
            Log.Logger.Information("Finding videos that have yet to be downloaded...");
            var videos = workspace.GetVideos()
                .Where(x => workspace.GetVideoPath(x.Id).Type == VideoPathType.None)
                .ToList();
            
            Log.Logger.Information("Downloading {total} videos...", videos.Count);

            int index = 0;
            foreach (var video in videos)
            {
                index++;
                Log.Logger.Information("Downloading video for {videoId} ({current} of {total})...", video.Id, index, videos.Count);

                try
                {
                    workspace.DownloadVideo(video.Id, $"https://invidio.us/latest_version?id={video.Id}&itag=18");
                    
                    Log.Information("Downloaded!");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Couldn't get video for {videoId}. " + ex.Message, video.Id);
                }
            }
        }
    }
}