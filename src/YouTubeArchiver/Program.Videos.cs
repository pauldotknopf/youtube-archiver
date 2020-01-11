using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using CommandLine;
using Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver
{
    partial class Program
    {
        [Verb("get-videos")]
        class GetVideosOptions : BaseIndexOptions
        {
            
        }

        private static int GetVideos(GetVideosOptions options)
        {
            options.Init();

            var workspace = options.GetWorkspace();

            var videosDirectory = Path.Combine(options.IndexDirectory, "videos");
            if (!Directory.Exists(videosDirectory))
            {
                Directory.CreateDirectory(videosDirectory);
            }
            
            foreach (var video in workspace.Index.Videos)
            {
                Log.Logger.Information("Downloading video for {videoId}...", video.Id);

                try
                {
                    var videoFile = Path.Combine(videosDirectory, $"{video.Id}.mp4");

                    if (File.Exists(videoFile))
                    {
                        Log.Logger.Information("Already downloaded, skipping...");
                        continue;
                    }
                    
                    var getVideoResponse =
                        GetRequestBody($"https://www.youtube.com/get_video_info?html5=1&video_id={video.Id}");

                    var keys = getVideoResponse.Split("&").Select(x =>
                    {
                        var split = x.Split("=");
                        return new Tuple<string, string>(split[0], HttpUtility.UrlDecode(split[1]));
                    }).ToDictionary(x => x.Item1, x => x.Item2);

                    var playerResponse = JsonConvert.DeserializeObject<GetVideoPlayerObject>(keys["player_response"]);

                    var stream = playerResponse.StreamingData.Formats.FirstOrDefault(x =>
                        x.Quality == "medium" && x.MimeType.Contains("video/mp4"));

                    if (stream == null)
                    {
                        stream = playerResponse.StreamingData.Formats.FirstOrDefault(x =>
                            x.MimeType.Contains("video/mp4"));
                    }

                    if (stream == null)
                    {
                        Log.Logger.Error("Couldn't find stream for {videoId}...", video.Id);
                        continue;
                    }

                    using (var client = new WebClient())
                    {
                        var tmpFile = $"{Path.GetDirectoryName(videoFile)}/tmp.mp4";
                        if (File.Exists(tmpFile))
                        {
                            File.Delete(tmpFile);
                        }
                        client.DownloadFile(stream.Url, tmpFile);
                        File.Move(tmpFile, videoFile);
                    }
                    
                    Log.Logger.Information("Downloaded!");
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Couldn't get video for {videoId}. " + ex.Message, video.Id);
                }
            }

            return 0;
        }
    }
}