using Newtonsoft.Json;

namespace Common.Models
{
    public class Caption
    {
        [JsonProperty("start")]
        public double Start { get; set; }
            
        [JsonProperty("duraction")]
        public double Duration { get; set; }
            
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}