using Microsoft.AspNetCore.Mvc;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer.Controllers
{
    public class VideosController : Controller
    {
        public ActionResult Index([FromRouteData]VideosModel data)
        {
            return View("Index", data);
        }
    }
}