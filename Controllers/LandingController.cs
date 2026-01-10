using Microsoft.AspNetCore.Mvc;

namespace ClashArt.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
