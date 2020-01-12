using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace Common
{
    public class IndexWorkspace
    {
        private readonly string _directory;

        private IndexWorkspace(ChannelIndex index, string directory)
        {
            _directory = directory;
            Index = index;
        }

        public static IndexWorkspace Create(string directory)
        {
            var index = Path.Combine(directory, "index.json");
            if (!File.Exists(index))
            {
                return null;
            }
            
            return new IndexWorkspace(JsonConvert.DeserializeObject<ChannelIndex>(File.ReadAllText(index)), directory);
        }
        
        public ChannelIndex Index { get; }
        
        public Dictionary<string, string> CaptionsFiles { get; } = new Dictionary<string, string>();

        public Dictionary<string, string> VideoFiles { get; } = new Dictionary<string, string>();
        
        public void DiscoverCaptions()
        {
            var captionsDirectory = Path.Combine(_directory, "captions");

            CaptionsFiles.Clear();
            
            if (!Directory.Exists(captionsDirectory))
            {
                return;
            }

            foreach (var captionFile in Directory.GetFiles(captionsDirectory, "*.json"))
            {
                var videoId = Path.GetFileNameWithoutExtension(captionFile);
                if (Index.Videos.All(x => x.Id != videoId))
                {
                    Log.Logger.Warning($"The caption file captions/{Path.GetFileName(captionFile)} isn't referencing an indexed video.");
                    continue;
                }

                CaptionsFiles[videoId] = captionFile;
            }
        }

        public void DownloadCaption(Video video, Action<string> action)
        {
            var captionDirectory = Path.Combine(_directory, "captions");
            if (!Directory.Exists(captionDirectory))
            {
                Directory.CreateDirectory(captionDirectory);
            }

            action(Path.Combine(captionDirectory, $"{video.Id}.json"));
        }

        public void DiscoverLocalVideos()
        {
            var videosDirectory = Path.Combine(_directory, "videos");

            VideoFiles.Clear();
            
            if (!Directory.Exists(videosDirectory))
            {
                return;
            }

            foreach (var videoFile in Directory.GetFiles(videosDirectory, "*.mp4"))
            {
                var videoId = Path.GetFileNameWithoutExtension(videoFile);
                if (Index.Videos.All(x => x.Id != videoId))
                {
                    Log.Logger.Warning($"The video file videos/{Path.GetFileName(videoFile)} isn't referencing an indexed video.");
                    continue;
                }

                VideoFiles[videoId] = videoFile;
            }
        }
        
        public void DownloadVideo(Video video, Action<string> action)
        {
            var captionDirectory = Path.Combine(_directory, "videos");
            if (!Directory.Exists(captionDirectory))
            {
                Directory.CreateDirectory(captionDirectory);
            }

            action(Path.Combine(captionDirectory, $"{video.Id}.mp4"));
        }
    }
}