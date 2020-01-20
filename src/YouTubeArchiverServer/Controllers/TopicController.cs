using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer.Controllers
{
    public class TopicController : Controller
    {
        public ActionResult Index([FromRouteData]TopicModel topic)
        {
            return View("Index", topic);
        }
        
        public ActionResult List([FromRouteData]ChannelModel channel)
        {
            return View("List", channel);
        }
    }
}