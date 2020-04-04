using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace Common
{
    public class IndexWorkspace : IIndexWorkspace
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
        
        public void SaveCaptions(string videoId, List<Caption> captions)
        {
            var captionDirectory = Path.Combine(_directory, "captions");
            if (!Directory.Exists(captionDirectory))
            {
                Directory.CreateDirectory(captionDirectory);
            }

            var destination = Path.Combine(captionDirectory, $"{videoId}.json");

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
            
            File.WriteAllText(destination, JsonConvert.SerializeObject(captions, Formatting.Indented));
        }
        
        public List<Video> GetVideos()
        {
            var result = new List<Video>();
            var videosDirectory = Path.Combine(_directory, "videos");

            if (!Directory.Exists(videosDirectory))
            {
                return result;
            }

            foreach (var file in Directory.GetFiles(videosDirectory, "*.json"))
            {
                result.Add(JsonConvert.DeserializeObject<Video>(File.ReadAllText(file)));
            }

            return result;
        }

        public Dictionary<string, List<Caption>> GetCaptions()
        {
            var result = new Dictionary<string, List<Caption>>();
            var captionsDirectory = Path.Combine(_directory, "captions");

            if (!Directory.Exists(captionsDirectory))
            {
                return result;
            }

            foreach (var file in Directory.GetFiles(captionsDirectory, "*.json"))
            {
                result.Add(Path.GetFileNameWithoutExtension(file), JsonConvert.DeserializeObject<List<Caption>>(File.ReadAllText(file)));
            }

            return result;
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

        public Dictionary<string, TopicSearch> GetTopics()
        {
            var result = new Dictionary<string, TopicSearch>();
            var topicsDirectory = Path.Combine(_directory, "topics");

            if (!Directory.Exists(topicsDirectory))
            {
                return result;
            }

            foreach (var topicFile in Directory.GetFiles(topicsDirectory, "*.json"))
            {
                var topicKey = SanitizeTopic(Path.GetFileNameWithoutExtension(topicFile).ToLower());
                result.Add(topicKey, JsonConvert.DeserializeObject<TopicSearch>(File.ReadAllText(topicFile)));
            }

            return result;
        }

        public void SaveTopic(TopicSearch topicSearch)
        {
            var topicsDirectory = Path.Combine(_directory, "topics");
            if (!Directory.Exists(topicsDirectory))
            {
                Directory.CreateDirectory(topicsDirectory);
            }

            var topicKey = SanitizeTopic(topicSearch.Topic);
            var destination = Path.Combine(topicsDirectory, $"{topicKey}.json");
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
            File.WriteAllText(destination, JsonConvert.SerializeObject(topicSearch, Formatting.Indented));
        }

        private string SanitizeTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException(nameof(topic));
            }
            
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                topic = topic.Replace(c, '-');
            }

            return topic.Trim().Replace(' ', '-').ToLower();
        }
    }
}