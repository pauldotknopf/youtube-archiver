using System.Collections.Generic;
using Newtonsoft.Json;

namespace Common.Models
{
    public class TopicSearch
    {
        public TopicSearch()
        {
            Results = new List<VideoResult>();
        }
        
        [JsonProperty("topic")]
        public string Topic { get; set; }
        
        [JsonProperty("aliases")]
        public List<string> Aliases { get; set; }
        
        [JsonProperty("hash")]
        public string Hash { get; set; }
        
        [JsonProperty("results")]
        public List<VideoResult> Results { get; set; }
        
        public class VideoResult
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            
            [JsonProperty("segments")]
            public List<Segment> Segments { get; set; }
            
            public class Segment
            {
                [JsonProperty("text")]
                public string Text { get; set; }
                
                [JsonProperty("location")]
                public int Location { get; set; }
            }
        }
    }
}