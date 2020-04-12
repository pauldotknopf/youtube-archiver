using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        public VideoPath GetVideoPath(string id)
        {
            var mirrorDirectory = Path.Combine(_directory, "mirror");
            if (!Directory.Exists(mirrorDirectory))
            {
                return VideoPath.None;
            }

            var localFile = Path.Combine(mirrorDirectory, $"{id}.mp4");

            if (File.Exists(localFile))
            {
                return VideoPath.Local(localFile);
            }

            var mirrorFile = Path.Combine(mirrorDirectory, $"{id}.txt");

            if (File.Exists(mirrorFile))
            {
                return VideoPath.External(File.ReadAllText(mirrorFile));
            }
            
            return VideoPath.None;
        }

        public void UpdateVideoPath(string id, VideoPathExternal path)
        {
            var mirrorDirectory = Path.Combine(_directory, "mirror");
            if (!Directory.Exists(mirrorDirectory))
            {
                Directory.CreateDirectory(mirrorDirectory);
            }
            
            var localFile = Path.Combine(mirrorDirectory, $"{id}.mp4");

            if (File.Exists(localFile))
            {
                File.Delete(localFile);
            }

            var mirrorFile = Path.Combine(mirrorDirectory, $"{id}.txt");

            if (File.Exists(mirrorFile))
            {
                File.Delete(mirrorFile);
            }
            
            File.WriteAllText(mirrorFile, path.Url);
        }

        public Dictionary<string, List<Caption>> GetCaptions()
        {
            var result = new Dictionary<string, List<Caption>>();
            var captionsDirectory = Path.Combine(_directory, "captions");

            if (!Directory.Exists(captionsDirectory))
            {
                return result;
            }

            foreach (var file in Directory.GetFiles(captionsDirectory, "*.json").OrderBy(x => x))
            {
                result.Add(Path.GetFileNameWithoutExtension(file), JsonConvert.DeserializeObject<List<Caption>>(File.ReadAllText(file)));
            }

            return result;
        }
        
        public void DownloadVideo(string id, string url)
        {
            if (GetVideoPath(id).Type != VideoPathType.None)
            {
                throw new Exception("The video is already downloaded.");
            }

            var mirrorDirectory = Path.Combine(_directory, "mirror");
            if (!Directory.Exists(mirrorDirectory))
            {
                Directory.CreateDirectory(mirrorDirectory);
            }
            using (var client = new WebClient())
            {
                var tmpFile = Path.Combine(mirrorDirectory, $"{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 5)}-tmp.mp4");
                if (File.Exists(tmpFile))
                {
                    File.Delete(tmpFile);
                }
                
                client.DownloadFile(url, tmpFile);
                File.Move(tmpFile, Path.Combine(mirrorDirectory, $"{id}.mp4"));
            }
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

        public TopicSearch FindTopic(string topic)
        {
            var topicKey = SanitizeTopic(topic);
            var potential = Path.Combine(_directory, "topics", $"{topicKey}.json");
            if (File.Exists(potential))
            {
                return JsonConvert.DeserializeObject<TopicSearch>(File.ReadAllText(potential));
            }

            return null;
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