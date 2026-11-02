using Microsoft.AspNetCore.Mvc;

namespace front_end_web.Controllers
{
    public class RoutesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
