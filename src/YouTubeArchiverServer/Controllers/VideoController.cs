using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer.Controllers
{
    public class VideoController : Controller
    {
        private readonly List<ChannelModel> _channels;

        public VideoController(List<ChannelModel> channels)
        {
            _channels = channels;
        }
        
        public ActionResult Index([FromRouteData]string channelId, [FromRouteData]string videoId)
        {
            var channel = _channels.Single(x => x.Channel.Id == channelId);
            var video = channel.Videos.Single(x => x.Video.Id == videoId);
            return View("Index", video);
        }
    }
}