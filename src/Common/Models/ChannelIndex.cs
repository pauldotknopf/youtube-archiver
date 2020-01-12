using System.Collections.Generic;
using Newtonsoft.Json;

namespace Common.Models
{
    public class ChannelIndex
    {
        [JsonProperty("channel")]
        public Channel Channel { get; set; }
        
        [JsonProperty("videos")]
        public List<Video> Videos { get; set; }
    }
}