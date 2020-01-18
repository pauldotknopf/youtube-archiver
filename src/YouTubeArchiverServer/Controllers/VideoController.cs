using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;
using YouTubeArchiverServer.Services;

namespace YouTubeArchiverServer.Controllers
{
    public class VideoController : Controller
    {
        private readonly ChannelService _channelService;

        public VideoController(ChannelService channelService)
        {
            _channelService = channelService;
        }
        
        public ActionResult Index([FromRouteData]string channelId, [FromRouteData]string videoId)
        {
            var model = new VideoModel();
            model.Channel = _channelService.GetChannelById(channelId);
            model.Video = _channelService.GetVideoById(videoId);

            return View("Index", model);
        }
    }
}