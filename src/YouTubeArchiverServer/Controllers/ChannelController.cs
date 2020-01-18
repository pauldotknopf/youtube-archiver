using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;
using YouTubeArchiverServer.Services;

namespace YouTubeArchiverServer.Controllers
{
    public class ChannelController : Controller
    {
        private readonly ChannelService _channelService;

        public ChannelController(ChannelService channelService)
        {
            _channelService = channelService;
        }
        
        public ActionResult Index([FromRouteData]string channelId)
        {
            var model = new ChannelModel();
            model.Channel = _channelService.GetChannelById(channelId);
            model.Videos = _channelService.GetChannelVideos(channelId);
            return View("Index", model);
        }
    }
}