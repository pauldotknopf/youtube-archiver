using System.Collections.Generic;
using Common.Models;

namespace Common
{
    public interface IIndexWorkspace
    {
        ChannelIndex Index { get; }

        List<Video> GetVideos();

        VideoPath GetVideoPath(string id);

        void UpdateVideoPath(string id, VideoPathExternal path);

        Dictionary<string, List<Caption>> GetCaptions();

        Dictionary<string, TopicSearch> GetTopics();

        TopicSearch FindTopic(string topic);
    }
}