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
        
        public string Topic { get; set; }
        
        public List<VideoResult> Results { get; set; }
        
        public class VideoResult
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            
            [JsonProperty("title")]
            public string Title { get; set; }
            
            [JsonProperty("url")]
            public string Url { get; set; }
            
            [JsonProperty("uploadedOn")]
            public string UploadedOn { get; set; }

            [JsonProperty("segments")]
            public List<Segment> Segments { get; set; }
            
            public class Segment
            {
                [JsonProperty("text")]
                public string Text { get; set; }
                
                [JsonProperty("location")]
                public int Location { get; set; }
                
                [JsonProperty("directUrl")]
                public string DirectUrl { get; set; }
            }
        }
    }
}