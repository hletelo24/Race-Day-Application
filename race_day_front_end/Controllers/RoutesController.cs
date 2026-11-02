using Microsoft.AspNetCore.Mvc;

namespace race_day_front_end.Controllers
{
    public class RoutesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
