using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer.Controllers
{
    public class VideosController : Controller
    {
        private readonly List<ChannelModel> _channels;

        public VideosController(List<ChannelModel> channels)
        {
            _channels = channels;
        }
        
        public ActionResult All()
        {
            var videosModel = new VideosModel();
            
            foreach (var channel in _channels)
            {
                foreach (var video in channel.Videos)
                {
                    videosModel.Videos.Add(video);
                }
            }
            
            return View("All", videosModel);
        }

        public ActionResult ForChannel(string channelId)
        {
            return View("ForChannel", _channels.Single(x => x.Channel.Id == channelId));
        }
    }
}