using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer.Controllers
{
    public class TopicController : Controller
    {
        private readonly List<ChannelModel> _channels;

        public TopicController(List<ChannelModel> channels)
        {
            _channels = channels;
        }

        // /topics
        public ActionResult List()
        {
            var topicsModel = new TopicsModel();

            foreach (var topic in _channels.SelectMany(x => x.Topics))
            {
                var current = topicsModel.Topics.SingleOrDefault(x => x.Id == topic.Id);
                if (current == null)
                {
                    current = new TopicModel();
                    topicsModel.Topics.Add(current);
                }

                current.Id = topic.Id;
                current.Topic = topic.Topic;
                current.Videos.AddRange(topic.Videos);
            }

            return View("List", topicsModel);
        }

        // /topics/{channelId}
        public ActionResult ListByChannel(string channelId)
        {
            return View("ListByChannel", _channels.Single(x => x.Channel.Id == channelId));
        }

        // /topic/{topicId}
        public ActionResult TopicVideos([FromRouteData]string topicId)
        {
            TopicModel topicModel = null;
            
            foreach (var topic in _channels.SelectMany(x => x.Topics))
            {
                if (topic.Id != topicId)
                {
                    continue;
                }

                if (topicModel == null)
                {
                    topicModel = new TopicModel();
                    topicModel.Id = topicId;
                    topicModel.Topic = topic.Topic;
                }
                
                topicModel.Videos.AddRange(topic.Videos);
            }
            
            return View("TopicVideos", topicModel);
        }
        
        // /topic/{topicId}/channel/{channelId}
        public ActionResult TopicVideosForChannel([FromRouteData]string topicId, [FromRouteData]string channelId)
        {
            return View("TopicVideosForChannel", _channels.Single(x => x.Channel.Id == channelId).Topics
                .SingleOrDefault(x => x.Id == topicId));
        }
    }
}