using Newtonsoft.Json;

namespace Common.Models
{
    public class Channel
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("upload-playlist-id")]
        public string UploadPlaylistId { get; set; }
    }
}