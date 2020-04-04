using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer.Controllers
{
    public class TopicController : Controller
    {
        public ActionResult Index([FromRouteData]TopicModel data)
        {
            return View("Index", data);
        }
        
        public ActionResult List([FromRouteData]TopicsModel data)
        {
            return View("List", data);
        }
    }
}