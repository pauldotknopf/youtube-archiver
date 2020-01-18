using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net;
using Serilog;

namespace YouTubeArchiver
{
    public partial class Program
    {
        class VideoCommand
        {
            public static Command Create()
            {
                var command = new Command("download-video")
                {
                    new Argument<string>("video-id"),
                    new Argument<string>("output")
                };

                command.Handler = CommandHandler.Create(typeof(VideoCommand).GetMethod(nameof(Run)));
                
                return command;
            }

            public static void Run(string videoId, string output)
            {
                try
                {
                    Log.Logger.Information("Download {videoId} to {output}...", videoId, output);
                    var player = Helpers.GetVideoPlayerInfoForYouTubeVideo(videoId);
                    
                    var stream = player.StreamingData.Formats.FirstOrDefault(x =>
                        x.Quality == "medium" && x.MimeType.Contains("video/mp4"));

                    if (stream == null)
                    {
                        stream = player.StreamingData.Formats.FirstOrDefault(x =>
                            x.MimeType.Contains("video/mp4"));
                    }

                    if (stream == null)
                    {
                        Log.Logger.Error("Couldn't find stream for {videoId}...", videoId);
                        return;
                    }

                    return;

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(stream.Url, output);
                    }
                            
                    Log.Logger.Information("Downloaded!");
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error downloading video. " + ex.Message);
                    Environment.Exit(1);
                }
            }
        }
    }
}