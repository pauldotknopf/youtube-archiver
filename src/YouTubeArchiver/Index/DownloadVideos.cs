using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Serilog;

namespace YouTubeArchiver.Index
{
    public class DownloadVideos
    {
        public static Command Create()
        {
            var command = new Command("download-videos")
            {
                Helpers.BuildIndexOption()
            };

            command.Handler = CommandHandler.Create(typeof(DownloadVideos).GetMethod(nameof(Run)));

            return command;
        }

        public static Task Run(string indexDirectory)
        {
            var workspace = Helpers.GetWorkspace(indexDirectory);
            
            Log.Logger.Information("Discovering already downloaded videos...");
            workspace.DiscoverLocalVideos();

            var videos = workspace.Index.Videos.Where(x => !workspace.VideoFiles.ContainsKey(x.Id)).ToList();
            
            Log.Logger.Information("Downloading {total} videos...", videos.Count);

            int index = 0;
            foreach (var video in videos)
            {
                index++;
                Log.Logger.Information("Downloading video for {videoId} ({current} of {total})...", video.Id, index, videos.Count);

                workspace.DownloadVideo(video, path =>
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        
                        var player = Helpers.GetVideoPlayerInfoForYouTubeVideo(video.Id);

                        var stream = player.StreamingData.Formats.FirstOrDefault(x =>
                            x.Quality == "medium" && x.MimeType.Contains("video/mp4"));

                        if (stream == null)
                        {
                            stream = player.StreamingData.Formats.FirstOrDefault(x =>
                                x.MimeType.Contains("video/mp4"));
                        }

                        if (stream == null)
                        {
                            Log.Logger.Error("Couldn't find stream for {videoId}...", video.Id);
                            return;
                        }

                        using (var client = new WebClient())
                        {
                            var tmpFile = $"{Path.GetDirectoryName(path)}/tmp.mp4";
                            if (File.Exists(tmpFile))
                            {
                                File.Delete(tmpFile);
                            }
                            client.DownloadFile(stream.Url, tmpFile);
                            File.Move(tmpFile, path);
                        }
                        
                        Log.Logger.Information("Downloaded!");
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Couldn't get video for {videoId}. " + ex.Message, video.Id);
                    }
                });
            }
            
            return Task.CompletedTask;
        }
    }
}