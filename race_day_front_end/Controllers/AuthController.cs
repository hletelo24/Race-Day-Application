using Microsoft.AspNetCore.Mvc;

namespace race_day_front_end.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
