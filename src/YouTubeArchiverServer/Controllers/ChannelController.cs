using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer.Controllers
{
    public class ChannelController : Controller
    {
        private readonly List<ChannelModel> _channels;

        public ChannelController(List<ChannelModel> channels)
        {
            _channels = channels;
        }

        public ActionResult List()
        {
            return View("List", _channels);
        }
        
        public ActionResult Index([FromRouteData]string channelId)
        {
            return View("Index", _channels.Single(x => x.Channel.Id == channelId));
        }
    }
}